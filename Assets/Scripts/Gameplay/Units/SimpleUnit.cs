using NaughtyAttributes;
using Pathfinding;
using RuntimeStatics;
using System;
using System.Linq;
using UniRx;
using UnityEngine;
using static Player;


[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AILerp))]
/// <summary>
/// Base class governing behaviours that all units share e.g. Movement & Death.
/// 
/// This class should coordinate between the prefab's Animator, setting triggers, and processing Movement control 
/// </summary>
public class SimpleUnit : MonoBehaviour, IDamageable
{
    protected const string ANIMATION_BOOL_MOVING = "IsMoving";

    BehaviorSubject<Vector3?> _targetPos = new(null);

    public Faction Faction => faction;
    public float HpAlpha => (float)currentHp / maxHp;

    [SerializeField]
    [Expandable]
    protected UnitData data;

    [SerializeField]
    private UnitHealthBar healthBar;

    [SerializeField]
    private DeadUnit deadPrefab;

    private Faction faction;
    protected float moveSpeed => data.MovementSpeed;
    protected int maxHp => data.MaxHp;
    [ShowNonSerializedField]
    protected int currentHp;
    protected bool isMoving { get; private set; }
    protected IUnitInteractable interactionTarget;

    //These components should be on the prefab root, along with this script
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    [Tooltip("FOR TESTING PURPOSES, BUTTON BELOW")]
    protected Transform moveToTargetTransform;    //FOR TESTING PURPOSES

    private Action onMoveToComplete;
    private CompositeDisposable _disposables = new();
    private AILerp _pathfinder;

    public IObservable<Vector3?> ObserveTargetPos() => _targetPos;

    protected virtual void Awake()
    {
        currentHp = maxHp;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        //To allow for later overriding if needed
        faction = data.Faction;
        SetupPathfinder();
    }

    protected virtual void OnDestroy()
    {
        _disposables.Clear();
    }

    protected virtual void Update()
    {
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

    public virtual bool MoveTo(Vector3 worldPosition, Action onComplete = null, bool clearTarget = true)
    {
        if (clearTarget) 
        {
            //Clear the interaction target as we may be cancelling another action
            interactionTarget = null;
        }

        if (worldPosition == transform.position) 
        {
            onComplete?.Invoke();
            return true;
        }

        onMoveToComplete = onComplete;
        _pathfinder.destination = worldPosition;
        _targetPos.OnNext(worldPosition);
        _pathfinder.SearchPath();
        return true;
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

    private void SetupPathfinder()
    {
        _pathfinder = GetComponent<AILerp>();
        _pathfinder.speed = data.MovementSpeed;
        _pathfinder.ObserveMovementDirection().DistinctUntilChanged().Subscribe(direction =>
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                animator.SetBool(ANIMATION_BOOL_MOVING, false);
                if (isMoving)
                {
                    isMoving = false;
                    if (onMoveToComplete != null)
                    {
                        var tempAction = onMoveToComplete;
                        onMoveToComplete = null;
                        tempAction?.Invoke();
                    }
                }
                _targetPos.OnNext(null);
            }
            else
            {
                spriteRenderer.flipX = direction.x < 0f;
                animator.SetBool(ANIMATION_BOOL_MOVING, true);
                isMoving = true;
            }
        }).AddTo(_disposables);
    }
}
