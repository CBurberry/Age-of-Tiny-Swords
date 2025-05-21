using NaughtyAttributes;
using RuntimeStatics;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class governing behaviours that all units share e.g. Movement & Death.
/// 
/// This class should coordinate between the prefab's Animator, setting triggers, and processing Movement control 
/// (TODO: Incorporate Pathfinding rather than direct movement).
/// </summary>
public class SimpleUnit : MonoBehaviour, IDamageable
{
    protected const string ANIMATION_BOOL_MOVING = "IsMoving";

    public float HpAlpha => (float)currentHp / maxHp;

    [SerializeField]
    [Expandable]
    protected UnitData data;

    [SerializeField]
    [Tooltip("What interactable contexts does this unit support?")]
    [EnumFlags]
    private UnitInteractContexts interactableContexts;

    [SerializeField]
    private UnitHealthBar healthBar;

    [SerializeField]
    private DeadUnit deadPrefab;

    protected float moveSpeed => data.MovementSpeed;
    protected int maxHp => data.MaxHp;
    [ShowNonSerializedField]
    protected int currentHp;
    protected bool isMoving => moveToTargetPosition != null;    

    protected ABaseUnitInteractable interactionTarget;

    //These components should be on the prefab root, along with this script
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    [Tooltip("FOR TESTING PURPOSES, BUTTON BELOW")]
    private Transform moveToTargetTransform;    //FOR TESTING PURPOSES
    private Action onMoveToComplete;
    private Vector3? moveToStartingPosition;
    private Vector3? moveToTargetPosition;      //Reference to the target position
    private float startTime;                    //Time when the movement started.
    private float journeyLength;                //Total distance between the markers.

    protected virtual void Awake()
    {
        moveToStartingPosition = null;
        moveToTargetPosition = null;
        currentHp = maxHp;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        UpdateMoveTo();
    }

    public virtual bool MoveTo(Transform target, Action onComplete = null, bool clearTarget = true)
    {
        //Get the pivot of the sprite renderer of the object, we need to filter by name as there may be others for VFX animations
        var spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);

        //Smelly code I know. We assume bottom center pivot (or similarily defined custom pivot)
        var spriteRenderer = spriteRenderers.FirstOrDefault(x => x.gameObject.name == "Visuals");
        if (spriteRenderer == null) 
        {
            //Backup check for non-conforming objects
            spriteRenderer = spriteRenderers.FirstOrDefault();
        }

        Bounds bounds = spriteRenderer.bounds;

        //Set to bottom of bounds for simplicity
        bounds.center = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y, spriteRenderer.transform.position.z);
        bounds.extents = new Vector3(bounds.extents.x, 0f, 0f);
        return MoveTo(bounds.ClosestPoint(transform.position), onComplete, clearTarget);
    }

    //TODO: Use A* Pathfinding, return false when can't path
    public virtual bool MoveTo(Vector3 worldPosition, Action onComplete = null, bool clearTarget = true)
    {
        if (clearTarget) 
        {
            //Clear the interaction target as we may be cancelling another action
            interactionTarget = null;
        }

        if (worldPosition == transform.position) 
        {
            return true;
        }

        onMoveToComplete = onComplete;
        startTime = Time.time;
        journeyLength = Vector3.Distance(transform.position, worldPosition);
        moveToStartingPosition = transform.position;
        moveToTargetPosition = worldPosition;

        //Calculate direction vector between target and this to check if we need to flip the X-axis
        Vector3 directionUnitVector = ((Vector3)(moveToTargetPosition - transform.position)).normalized;

        //Set/Unset the required boolean flags for animator (also sprite renderer flip X-axis)
        spriteRenderer.flipX = directionUnitVector.x < 0f;
        animator.SetBool(ANIMATION_BOOL_MOVING, true);
        return true;
    }

    /// <summary>
    /// Get this unit to interact with a given target.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="context">Singular context</param>
    /// <returns>Success/Failure</returns>
    public virtual void Interact(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        if (!target.CanInteract(this, context, out UnitInteractContexts availableContexts)) 
        {
            Debug.Log("Could not interact");
            return;
        }

        if (BitwiseHelpers.GetSetBitCount((long)availableContexts) != 1) 
        {
            throw new ArgumentException($"[{nameof(SimpleUnit)}.{nameof(Interact)}]: Cannot resolve more than 1 interaction context at once!");
        }

        if (target is IBuilding)
        {
            ResolveBuildingInteraction(target, availableContexts);
        }
        else if (target is IResourceSource)
        {
            ResolveResourceInteraction(target, availableContexts);
        }
        else if (target is IDamageable /*&& Not Allied Faction*/) 
        {
            //TODO: Check target is not an allied faction unit or building!
            ResolveDamagableInteraction(target, availableContexts);
        }
        else
        {
            throw new NotImplementedException("TODO: define interaction behaviour with non-building classes");
        }
    }

    public virtual Vector3 GetClosestPosition(Vector3 position)
        => spriteRenderer.bounds.ClosestPoint(position);

    public virtual void ApplyDamage(int value)
    {
        if (value == 0f)
        {
            return;
        }

        //NOTE: Should there be some kind of 'took damage' animation or effect? If so, play here.

        currentHp = Math.Clamp(currentHp - value, 0, maxHp);

        //Update health bar
        bool shouldShow = currentHp < maxHp && currentHp > 0f;
        healthBar.SetValue(HpAlpha);
        healthBar.gameObject.SetActive(shouldShow);

        if (currentHp <= 0f)
        {
            TriggerDeath();
        }
    }

    protected virtual void ResolveBuildingInteraction(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(ResolveBuildingInteraction)}]: Context resolution not implemented for {nameof(SimpleUnit)}!");
    }

    protected virtual void ResolveDamagableInteraction(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(ResolveDamagableInteraction)}]: Context resolution not implemented for {nameof(SimpleUnit)}!");
    }

    protected virtual void ResolveResourceInteraction(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(ResolveResourceInteraction)}]: Context resolution not implemented for {nameof(SimpleUnit)}!");
    }

    protected virtual void UpdateMoveTo()
    {
        if (moveToTargetPosition != null)
        {
            //Distance moved equals elapsed time times speed..
            float distCovered = (Time.time - startTime) * moveSpeed;

            //Fraction of journey completed equals current distance divided by total distance.
            float fractionOfJourney = distCovered / journeyLength;

            //Set our position as a fraction of the distance between the markers.
            transform.position = Vector3.Lerp((Vector3)moveToStartingPosition, (Vector3)moveToTargetPosition, fractionOfJourney);
            if (transform.position == moveToTargetPosition)
            {
                EndMoveTo();
            }
        }
    }

    protected virtual void EndMoveTo()
    {
        moveToStartingPosition = null;
        moveToTargetPosition = null;
        animator.SetBool(ANIMATION_BOOL_MOVING, false);
        onMoveToComplete?.Invoke();
    }

    [Button("Interact with target (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void InteractWith()
    {
        Interact(moveToTargetTransform.gameObject.GetComponent<ABaseUnitInteractable>(), interactableContexts);
    }

    [Button("MoveToTransform (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void MoveToTransform()
    {
        if (moveToTargetTransform != null)
        {
            MoveTo(moveToTargetTransform);
        }
        else 
        {
            Debug.LogWarning("Transform reference is not set!");
        }
    }

    [Button("TriggerDeath (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void TriggerDeath()
    {
        //Hide self
        spriteRenderer.enabled = false;

        //Replace this prefab with a spawned instance of the death prefab
        DeadUnit deadUnit = Instantiate(deadPrefab, transform.position, Quaternion.identity, transform.parent);
        deadUnit.name = deadUnit.UnitName = name;
        Destroy(gameObject);
    }
}
