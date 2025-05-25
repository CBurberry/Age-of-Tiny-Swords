using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedUnit : AUnitInteractableUnit
{
    private const string ANIMATION_BOOL_ATTACKING = "IsAttacking";

    [SerializeField]
    private AProjectile projectilePrefab;

    [SerializeField]
    private float projectileSpeed;

    protected PrefabsPool<AProjectile> prefabsPool;

    protected override void Awake()
    {
        base.Awake();
        prefabsPool = new PrefabsPool<AProjectile>(projectilePrefab, transform.parent, 10);
    }

    protected override void ResolveResourceInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        //Only attackable if a goblin unit and the mine has pawns in it / is active
        if (Faction != Player.Faction.Goblins && target is GoldMine)
        {
            return;
        }

        GoldMine goldMine = target as GoldMine;
        if (goldMine.State == GoldMine.Status.Active)
        {
            interactionTarget = target;
            MoveTo(goldMine.transform, StartAttackMine, false, stopAtAttackDistance: true);
        }
    }

    protected override void ResolveBuildingInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        IDamageable damagableTarget = target as IDamageable;
        if (damagableTarget != null && damagableTarget.HpAlpha > 0f && damagableTarget.Faction != Faction)
        {
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
        }
        else
        {
            throw new NotImplementedException($"[{nameof(RangedUnit)}.{nameof(ResolveBuildingInteraction)}]: Context resolution not implemented for {nameof(RangedUnit)} & {context}!");
        }
    }

    protected override void ResolveDamagableInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        if (target is IDamageable)
        {
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
        }
        else
        {
            throw new NotImplementedException($"[{nameof(RangedUnit)}.{nameof(ResolveDamagableInteraction)}]: Context resolution not implemented for {nameof(RangedUnit)} & {context}!");
        }
    }

    //Other units can only attack this unit or do nothing
    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
        => unit.Faction != Faction ? UnitInteractContexts.Attack : UnitInteractContexts.None;

    //Start a cycle of MoveTo unit until close and attack
    protected virtual void StartAttacking()
    {
        StartCoroutine(Attacking());
    }

    //TODO: Time the hit with the animation connecting the hit
    protected virtual IEnumerator Attacking()
    {
        IDamageable damageTarget = interactionTarget as IDamageable;
        Func<bool> condition = () => damageTarget != null && damageTarget.HpAlpha > 0f && (interactionTarget as MonoBehaviour);
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            if (!IsTargetWithinDistance(damageTarget, out _))
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
                MoveTo((interactionTarget as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
                yield break;
            }
            else
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, true);
                RangedAttack();
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }
        }

        animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
    }

    private void StartAttackMine()
    {
        StartCoroutine(AttackMine());
    }

    private IEnumerator AttackMine()
    {
        GoldMine mine = interactionTarget as GoldMine;
        Vector3 closestPosition;
        Func<bool> condition = () => mine != null && mine.State == GoldMine.Status.Active && (interactionTarget as MonoBehaviour);
        while (condition.Invoke())
        {
            closestPosition = mine.SpriteRenderer.bounds.ClosestPoint(transform.position);
            float magnitude = (closestPosition - transform.position).magnitude;

            //Check we are at the target (proximity check? bounds?)
            if (magnitude > data.AttackDistance)
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
                MoveTo((interactionTarget as MonoBehaviour).transform, StartAttackMine, false, stopAtAttackDistance: true);
                yield break;
            }
            else
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, true);
                RangedAttack();
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }
        }

        animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
    }

    //Trigger on animation event
    [Button("RangedAttack (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void RangedAttack()
    {
        //FOR TESTING
        if (interactionTarget == null && moveToTargetTransform != null) 
        {
            interactionTarget = moveToTargetTransform.GetComponent<IUnitInteractable>();
        }

        Transform targetTransform = null;
        try 
        {
            targetTransform = (interactionTarget as MonoBehaviour)?.transform;
        }
        catch 
        {
            interactionTarget = null;
            Debug.LogWarning("Target invalid!");
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 travelVector = targetTransform.position - transform.position;
        Vector3 direction = travelVector.normalized;

        //NOTE: asuumes starts with component inactive
        AProjectile projectile = prefabsPool.Get();
        projectile.TravelVector = travelVector;
        projectile.Speed = projectileSpeed;
        SetProjectileSpawnPoint(projectile);
        SetProjectileRotation(projectile, direction);
        SetOtherProjectileProperties(projectile);
        projectile.enabled = true;
    }

    protected virtual void OnUnitKill()
    {
        Debug.Log("OnEnemyUnitKill");

        /* Was the target that was killed an enemy unit?
           - If so, look for another unit within an aggression range to engage combat in
           - Otherwise do nothing
         */

        //POLISH/TODO: Add a setting that prevents units from following an enemy unit too far or to return to a guarding / patrol position
    }

    protected virtual void SetProjectileSpawnPoint(AProjectile projectile)
        => projectile.transform.position = transform.position;

    protected virtual void SetProjectileRotation(AProjectile projectile, Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    //Set properties like damage radius etc which aren't common to base projectile
    protected virtual void SetOtherProjectileProperties(AProjectile projectile)
    {
    }
}
