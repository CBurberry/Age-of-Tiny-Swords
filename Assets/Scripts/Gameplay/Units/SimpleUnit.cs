using NaughtyAttributes;
using System;
using UnityEngine;

/// <summary>
/// Base class governing behaviours that all units share e.g. Movement & Death.
/// 
/// This class should coordinate between the prefab's Animator, setting triggers, and processing Movement control 
/// (TODO: Incorporate Pathfinding rather than direct movement).
/// </summary>
public class SimpleUnit : MonoBehaviour
{
    [SerializeField]
    [Expandable]
    protected UnitData data;

    protected float moveSpeed => data.MovementSpeed;
    protected float maxHp => data.MaxHp;

    [ShowNonSerializedField]
    protected float currentHp;

    //These components should be on the prefab root, along with this script
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    [Tooltip("FOR TESTING PURPOSES, BUTTON BELOW")]
    private Transform moveToTargetTransform;    //FOR TESTING PURPOSES

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
        //MoveTo update
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

    //TODO: Use A* Pathfinding, return false when can't path
    public bool MoveTo(Transform target) => MoveTo(target.position);
    public bool MoveTo(Vector3 worldPosition)
    {
        if (worldPosition == transform.position) 
        {
            return true;
        }

        startTime = Time.time;
        journeyLength = Vector3.Distance(transform.position, worldPosition);
        moveToStartingPosition = transform.position;
        moveToTargetPosition = worldPosition;

        //Calculate direction vector between target and this to check if we need to flip the X-axis
        Vector3 directionUnitVector = ((Vector3)(moveToTargetPosition - transform.position)).normalized;

        //Set/Unset the required boolean flags for animator (also sprite renderer flip X-axis)
        spriteRenderer.flipX = directionUnitVector.x < 0f;
        animator.SetBool("IsMoving", true);
        return true;
    }

    public void TakeDamage(float value)
    {
        if (value == 0f) 
        {
            return;
        }

        //NOTE: Should there be some kind of 'took damage' animation or effect? If so, play here.

        currentHp = Mathf.Clamp(currentHp - value, 0f, maxHp);
        if (currentHp <= 0f) 
        {
            TriggerDeath();
        }
    }

    protected virtual void EndMoveTo()
    {
        moveToStartingPosition = null;
        moveToTargetPosition = null;
        animator.SetBool("IsMoving", false);
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
        //TODO: Replace this prefab with a spawned instance of the death prefab
        throw new NotImplementedException();
    }
}
