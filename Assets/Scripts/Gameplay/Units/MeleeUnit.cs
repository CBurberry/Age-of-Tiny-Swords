using System;
using System.Collections;
using UnityEngine;

public class MeleeUnit : AUnitInteractableUnit
{
    private const string ANIMATION_BOOL_ATTACKING = "IsAttacking";

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
            throw new NotImplementedException($"[{nameof(MeleeUnit)}.{nameof(ResolveBuildingInteraction)}]: Context resolution not implemented for {nameof(MeleeUnit)} & {context}!");
        }
    }

    protected override void ResolveDamagableInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        if (target is IDamageable)
        {
            MoveTo((target as MonoBehaviour).transform, StartAttacking, stopAtAttackDistance: true);
            interactionTarget = target;
        }
        else
        {
            throw new NotImplementedException($"[{nameof(MeleeUnit)}.{nameof(ResolveDamagableInteraction)}]: Context resolution not implemented for {nameof(MeleeUnit)} & {context}!");
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
                MoveTo((interactionTarget as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
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
