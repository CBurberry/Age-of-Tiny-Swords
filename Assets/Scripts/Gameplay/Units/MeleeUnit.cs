using System;
using System.Collections;
using UnityEngine;

public class MeleeUnit : AUnitInteractableUnit
{
    private const string ANIMATION_BOOL_ATTACKING = "IsAttacking";

    protected override void ResolveDamagableInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        if (target is Sheep || target is MeleeUnit)
        {
            MoveTo((target as MonoBehaviour).transform, StartAttacking);
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
        Vector3 closestPosition;
        IDamageable damageTarget = interactionTarget as IDamageable;
        Func<bool> condition = () => damageTarget != null && damageTarget.HpAlpha > 0f;
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            closestPosition = damageTarget.GetClosestPosition(transform.position);
            float magnitude = (closestPosition - transform.position).magnitude;
            if (magnitude > data.AttackDistance)
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
