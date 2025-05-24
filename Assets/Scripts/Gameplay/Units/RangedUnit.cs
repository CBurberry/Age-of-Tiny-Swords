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
    private float projectileSpeed = 200f;

    private PrefabsPool<AProjectile> prefabsPool;

    protected override void Awake()
    {
        base.Awake();
        prefabsPool = new PrefabsPool<AProjectile>(projectilePrefab, transform.parent, 10);
        prefabsPool.SetActiveOnGet = false;
    }

    protected override void ResolveDamagableInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        if (target is IDamageable)
        {
            MoveTo((target as MonoBehaviour).transform, StartAttacking);
            interactionTarget = target;
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
    private void StartAttacking()
    {
        StartCoroutine(Attacking());
    }

    //TODO: Time the hit with the animation connecting the hit
    private IEnumerator Attacking()
    {
        IDamageable damageTarget = interactionTarget as IDamageable;
        Func<bool> condition = () => damageTarget != null && damageTarget.HpAlpha > 0f;
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            if (!IsTargetWithinDistance(damageTarget, out _))
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
                MoveTo((interactionTarget as MonoBehaviour).transform, StartAttacking, false);
                yield break;
            }
            else
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, true);
                damageTarget.ApplyDamage(data.BaseAttackDamage);
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }
        }

        animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
        OnUnitKill();
    }

    //Trigger on animation event
    [Button("RangedAttack (PlayMode)", EButtonEnableMode.Playmode)]
    private void RangedAttack()
    {
        //FOR TESTING
        if (interactionTarget == null) 
        {
            interactionTarget = moveToTargetTransform.GetComponent<IUnitInteractable>();
        }

        if (interactionTarget is not IDamageable) 
        {
            return;
        }

        Transform targetTransform = (interactionTarget as MonoBehaviour).transform;
        float distance = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 direction = (targetTransform.position - transform.position).normalized;

        //Starts inactive
        AProjectile projectile = prefabsPool.Get();
        TNT dynamite = projectile as TNT;
        Arrow arrow = projectile as Arrow;
        projectile.transform.position = transform.position;

        //NOT ROTATING CORRECTLY!
        //projectile.transform.LookAt(targetTransform.position);

        //Calculate the time to live and speed based on the distance (T = D/S)
        //Handle both arrows and TNT for now
        if (dynamite != null)
        {
            dynamite.Damage = data.BaseAttackDamage;
            dynamite.Direction = direction;
            dynamite.Ttl = distance / projectileSpeed;
            dynamite.Speed = projectileSpeed;
            dynamite.OnComplete = () => prefabsPool.Release(projectile);
        }
        else if (arrow != null) 
        {
            //arrow
            arrow.Direction = direction;
            arrow.Ttl = distance / projectileSpeed;
            arrow.Speed = projectileSpeed;
            arrow.OnComplete = () => 
            {
                (interactionTarget as IDamageable).ApplyDamage(data.BaseAttackDamage);
                prefabsPool.Release(projectile);
            };
        }

        projectile.gameObject.SetActive(true);
    }

    private void OnUnitKill()
    {
        Debug.Log("OnEnemyUnitKill");

        /* Was the target that was killed an enemy unit?
           - If so, look for another unit within an aggression range to engage combat in
           - Otherwise do nothing
         */

        //POLISH/TODO: Add a setting that prevents units from following an enemy unit too far or to return to a guarding / patrol position
    }
}
