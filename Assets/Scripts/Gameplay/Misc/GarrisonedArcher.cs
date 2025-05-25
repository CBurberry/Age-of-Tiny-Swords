using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarrisonedArcher : GarrisonedRangedUnit
{
    private const string ANIMATION_INT_FACING = "FacingDirection";

    public int ArrowDamage;

    [SerializeField]
    private AProjectile projectilePrefab;

    [SerializeField]
    private float projectileSpeed;

    [SerializeField]
    private float attackSpeed;

    [SerializeField]
    private float attackRange;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private SimpleUnit testAttackUnit;

    private PrefabsPool<AProjectile> prefabsPool;
    private IDamageable attackTarget;
    private FacingDirection facing;

    //Represents a 1-1 match to the int value in the animator (use flipX for left facing)
    private enum FacingDirection
    {
        Front = 0,
        Down = 1,
        Up = 2,
        DiagonalDown = 3,
        DiagonalUp = 4
    }

    private void Awake()
    {
        prefabsPool = new PrefabsPool<AProjectile>(projectilePrefab, transform, 10);
    }

    private void Update()
    {
        //TODO: Periodically check for enemies in range
    }

    public void StartAttacking()
    {
        StartCoroutine(Attacking());
    }

    //TODO: Time the hit with the animation connecting the hit
    private IEnumerator Attacking()
    {
        Func<bool> condition = () => attackTarget != null && attackTarget.HpAlpha > 0f && (attackTarget as MonoBehaviour);
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            if (!IsTargetWithinRange(attackTarget, out _))
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, false);

                //Do nothing, wait for a new target
                yield break;
            }
            else
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, true);
                RangedAttack();
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / attackSpeed, () => !condition.Invoke());
            }
        }

        animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
    }

    private void FaceTarget(float angle)
    {
        if (angle > -22.5f && angle <= 22.5f)
        {
            //Forward
            spriteRenderer.flipX = false;
            facing = FacingDirection.Front;
        }
        else if (angle > 22.5f && angle <= 67.5f)
        {
            //Forward diagonal up
            spriteRenderer.flipX = false;
            facing = FacingDirection.DiagonalUp;
        }
        else if (angle > 67.5f && angle <= 112.5f)
        {
            //Up
            spriteRenderer.flipX = false;
            facing = FacingDirection.Up;
        }
        else if (angle > 112.5f && angle <= 157.5f)
        {
            //Flipped Forward diagonal up
            spriteRenderer.flipX = true;
            facing = FacingDirection.DiagonalUp;
        }
        else if (angle > 157.5f && angle <= -157.5f)
        {
            //Flipped Forward
            spriteRenderer.flipX = true;
            facing = FacingDirection.Front;
        }
        else if (angle > -157.5f && angle <= -112.5f)
        {
            //Flipped Forward diagonal down
            spriteRenderer.flipX = true;
            facing = FacingDirection.DiagonalDown;
        }
        else if (angle > -112.5f && angle <= -67.5f)
        {
            //Down
            spriteRenderer.flipX = false;
            facing = FacingDirection.Down;
        }
        else if (angle > -67.5f && angle <= -22.5f)
        {
            //Forward diagonal down
            spriteRenderer.flipX = false;
            facing = FacingDirection.DiagonalDown;
        }
        else 
        {
            Debug.LogWarning("FACING NOT SET");
        }

        Debug.Log($"SetFacing: input angle {angle}, flipX {spriteRenderer.flipX}, facing {facing}");
        animator.SetInteger(ANIMATION_INT_FACING, (int)facing);
    }

    [Button("AttackTestTarget (PlayMode)", EButtonEnableMode.Playmode)]
    private void AttackTestTarget()
    {
        attackTarget = testAttackUnit;
        StartAttacking();
    }

    [Button("RangedAttack (PlayMode)", EButtonEnableMode.Playmode)]
    private void RangedAttack()
    {
        Transform targetTransform = null;
        try
        {
            targetTransform = (attackTarget as MonoBehaviour)?.transform;
        }
        catch
        {
            attackTarget = null;
            Debug.LogWarning("Target invalid!");
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 travelVector = targetTransform.position - transform.position;
        Vector3 direction = travelVector.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //Update the facing direction
        FaceTarget(angle);

        //NOTE: asuumes starts with component inactive
        AProjectile projectile = prefabsPool.Get();
        projectile.TravelVector = travelVector;
        projectile.Speed = projectileSpeed;
        SetProjectileSpawnPoint(projectile);
        SetProjectileRotation(projectile, angle);
        SetOtherProjectileProperties(projectile);
        projectile.enabled = true;
    }

    //Get the facing direction of the unit (based on animation state)
    //return the center boundary of the side representing the facing direction
    private void SetProjectileSpawnPoint(AProjectile projectile)
    {
        Vector3 offset = Vector3.zero;
        Bounds localBounds = spriteRenderer.localBounds;
        bool isRightFacing = !spriteRenderer.flipX;
        FacingDirection animFacing = (FacingDirection)animator.GetInteger(ANIMATION_INT_FACING);
        if (isRightFacing)
        {
            switch (animFacing)
            {
                case FacingDirection.Down:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Up:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Front:
                    offset = new Vector3(localBounds.center.x + localBounds.extents.x, localBounds.center.y, 0f);
                    break;
                case FacingDirection.DiagonalDown:
                    offset = new Vector3(localBounds.center.x + localBounds.extents.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.DiagonalUp:
                    offset = new Vector3(localBounds.center.x + localBounds.extents.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
            }
        }
        else
        {
            switch (animFacing)
            {
                case FacingDirection.Down:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Up:
                    offset = new Vector3(localBounds.center.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
                case FacingDirection.Front:
                    offset = new Vector3(localBounds.center.x - localBounds.extents.x, localBounds.center.y, 0f);
                    break;
                case FacingDirection.DiagonalDown:
                    offset = new Vector3(localBounds.center.x - localBounds.extents.x, localBounds.center.y - localBounds.extents.y, 0f);
                    break;
                case FacingDirection.DiagonalUp:
                    offset = new Vector3(localBounds.center.x - localBounds.extents.x, localBounds.center.y + localBounds.extents.y, 0f);
                    break;
            }
        }

        projectile.transform.position = transform.position + offset;
        Debug.Log("Arrow spawn position: " + projectile.transform.position);
    }

    private void SetProjectileRotation(AProjectile projectile, float angle)
    {
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetOtherProjectileProperties(AProjectile projectile)
    {
        projectile.OnComplete = () =>
        {
            attackTarget.ApplyDamage(ArrowDamage);
            projectile.enabled = false;
            prefabsPool.Release(projectile);
        };
    }

    private bool IsTargetWithinRange(IDamageable target, out Vector3 closestPosition)
    {
        closestPosition = target.GetClosestPosition(transform.position);
        float magnitude = (closestPosition - transform.position).magnitude;
        return magnitude <= attackRange;
    }
}
