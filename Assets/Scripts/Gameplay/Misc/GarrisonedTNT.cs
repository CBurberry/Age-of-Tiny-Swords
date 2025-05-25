using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarrisonedTNT: GarrisonedRangedUnit
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private SimpleUnit testAttackUnit;

    //TODO: Time the hit with the animation connecting the hit
    protected override IEnumerator Attacking()
    {
        Func<bool> condition = () => attackTarget != null && attackTarget.HpAlpha > 0f && (attackTarget as MonoBehaviour);
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            if (!IsTargetWithinRange(attackTarget, out _))
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
                attackTarget.OnDeath -= OnTargetKilled;
                attackTarget = null;
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
        spriteRenderer.flipX = (angle <= 180f && angle > 90f) || (angle > -180f && angle <= -90f);
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
        SetProjectileRotation(projectile);
        SetOtherProjectileProperties(projectile);
        projectile.enabled = true;
    }

    //Get the facing direction of the unit (based on animation state)
    //return the center boundary of the side representing the facing direction
    private void SetProjectileSpawnPoint(AProjectile projectile)
    {
        //Get the facing direction of the unit (based on flipX)
        //return the center boundary of the side representing the facing direction
        float xPos =
            spriteRenderer.flipX ? transform.position.x - spriteRenderer.bounds.extents.x
            : transform.position.x + spriteRenderer.bounds.extents.x;

        projectile.transform.position = new Vector3(xPos, spriteRenderer.bounds.center.y, spriteRenderer.transform.position.z);
    }

    //Set rotation either through facing enum switch or flip
    private void SetProjectileRotation(AProjectile projectile)
    {
        //NOTE: We assume that our unit has already turned to face the target
        //Check our sprite renderer, apply the same FlipX value.
        projectile.SpriteRenderer.flipX = spriteRenderer.flipX;
    }

    //Set properties like damage radius etc which aren't common to base projectile
    private void SetOtherProjectileProperties(AProjectile projectile)
    {
        TNT dynamite = projectile as TNT;
        dynamite.Owner = GetComponentInParent<SimpleBuilding>();
        dynamite.Damage = Damage;
        dynamite.OnComplete = () =>
        {
            dynamite.Explode();
            dynamite.enabled = false;
            prefabsPool.Release(projectile);
        };
    }
}
