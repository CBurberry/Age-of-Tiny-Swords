using NaughtyAttributes;
using RuntimeStatics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeleeUnit : AUnitInteractableUnit
{
    private const string ANIMATION_BOOL_ATTACKING = "IsAttacking";
    private const string ANIMATION_INT_FACING = "FacingDirection";

    public override bool IsAttacking() => attackTarget != null;

    [SerializeField]
    [MinValue(1f)]
    private float delayForTargetAcquisition;

    //Not goldmine
    private IDamageable attackTarget;
    private float targetAcquireTimer;

    private enum FacingDirection
    {
        Front = 0,
        Down = 1,
        Up = 2
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (attackTarget != null)
        {
            attackTarget.OnDeath -= OnTargetKilled;
        }
    }

    protected override void Update()
    {
        base.Update();

        //Check for enemies in the area, attack them until they leave range (or are killed)
        if (!isMoving && !IsAttacking())
        {
            targetAcquireTimer += Time.deltaTime;
            if (targetAcquireTimer > delayForTargetAcquisition)
            {
                CheckForNewEnemyTarget();
                targetAcquireTimer = 0f;
            }
        }
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
        IBuilding building = target as IBuilding;
        if (building != null && building.HpAlpha > 0f && building.Faction != Faction)
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
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
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
        if (!(interactionTarget as MonoBehaviour))
        {
            if (attackTarget != null)
            {
                attackTarget.OnDeath -= OnTargetKilled;
                attackTarget = null;
            }
            yield break;
        }

        attackTarget = interactionTarget as IDamageable;
        attackTarget.OnDeath += OnTargetKilled;
        Func<bool> condition = () => attackTarget != null && !attackTarget.IsKilled && (interactionTarget as MonoBehaviour);
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            if (!IsTargetWithinDistance(attackTarget, out _))
            {
                animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
                MoveTo((interactionTarget as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
                yield break;
            }
            else
            {
                Vector3 direction = ((interactionTarget as MonoBehaviour).transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                FaceTarget(angle);
                animator.SetBool(ANIMATION_BOOL_ATTACKING, true);
                attackTarget.ApplyDamage(data.BaseAttackDamage, this);
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }
        }

        if (attackTarget != null)
        {
            attackTarget.OnDeath -= OnTargetKilled;
        }

        attackTarget = null;
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
                mine.Attack();
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }
        }

        animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
    }

    protected void CheckForNewEnemyTarget()
    {
        IEnumerable<IDamageable> potentialTargets = DamageableUtilities.GetDamageablesInArea(transform.position, data.DetectionDistance,
            (x) => x.Faction != Player.Faction.None && x.Faction != Faction && !x.IsKilled);

        //Select the closest one (underlying implemenation uses pre-sorted results)
        attackTarget = potentialTargets.FirstOrDefault();
        if (attackTarget != null)
        {
            interactionTarget = attackTarget as IUnitInteractable;
            StartAttacking();
        }
    }

    protected override void OnDamaged(IDamageable attacker)
    {
        base.OnDamaged(attacker);

        if (!isMoving && !IsAttacking())
        {
            attackTarget = attacker;
            interactionTarget = attackTarget as IUnitInteractable;
            StartAttacking();
        }
    }

    protected virtual void FaceTarget(float angle)
    {
        //Set facing for melee units (similar to ranged implementation but with 4 directions)
        FacingDirection facing = (FacingDirection)animator.GetInteger(ANIMATION_INT_FACING);
        if (angle > -45f && angle <= 45f)
        {
            //Forward
            spriteRenderer.flipX = false;
            facing = FacingDirection.Front;
        }
        else if (angle > 45f && angle <= 135f)
        {
            //Up
            spriteRenderer.flipX = false;
            facing = FacingDirection.Up;
        }
        else if ((angle > 135f && angle <= 180f) || (angle > -180f && angle <= -135f))
        {
            //Flipped Forward
            spriteRenderer.flipX = true;
            facing = FacingDirection.Front;
        }
        else if (angle > -135f && angle <= -45f)
        {
            //Down
            spriteRenderer.flipX = false;
            facing = FacingDirection.Down;
        }

        animator.SetInteger(ANIMATION_INT_FACING, (int)facing);
    }

    protected virtual void OnTargetKilled()
    {
        attackTarget = null;
        if (animator != null)
        {
            animator.SetBool(ANIMATION_BOOL_ATTACKING, false);
        }
    }
}
