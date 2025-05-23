using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class ExplodingUnit : AUnitInteractableUnit
{
    private const string ANIMATION_BOOL_ACTIVE = "IsActive";            //Goblin is peeking out of the barrel
    private const string ANIMATION_TRIG_FIRE = "Fire";                  //Trigger once to move to flashing with an in animation playing between
    private const string ANIMATION_TRIG_DISARM = "Disarm";              //Trigger once to move to active idle/run with an out animation playing

    private const string ANIMSTATE_IDLE_OPEN = "Idle_Out";
    private const string ANIMSTATE_PRIME_TRANSITION = "In (Fire)";
    private const string ANIMSTATE_PRIMED = "Fired";

    [SerializeField]
    private bool shouldExplodeOnDeath;

    [SerializeField]
    private float timeToExplode;

    [SerializeField]
    private float explosionRadius;

    [SerializeField]
    private float timeBeforeIdleIn;

    private bool hasExploded;
    private float elapsedIdleTime;

    //Other units can only attack this unit or do nothing
    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
        => unit.Faction != Faction ? UnitInteractContexts.Attack : UnitInteractContexts.None;

    protected override void Awake()
    {
        base.Awake();
        hasExploded = false;
        elapsedIdleTime = 0f;
    }

    protected override void Update()
    {
        base.Update();

        if (IsIdlingOpen())
        {
            elapsedIdleTime += Time.deltaTime;
            if (elapsedIdleTime >= timeBeforeIdleIn) 
            {
                elapsedIdleTime = 0f;
                animator.SetBool(ANIMATION_BOOL_ACTIVE, false);
            }
        }

        //TODO: Add a check for an enemy nearby, if so - set active bool and start attacking
    }

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

    public override bool MoveTo(Vector3 worldPosition, Action onComplete = null, bool clearTarget = true)
    {
        animator.SetBool(ANIMATION_BOOL_ACTIVE, true);
        return base.MoveTo(worldPosition, onComplete, clearTarget);
    }

    public override void ApplyDamage(int value)
    {
        //Taking damage should activate this unit
        if (value > 0)
        {
            animator.SetBool(ANIMATION_BOOL_ACTIVE, true);
        }

        base.ApplyDamage(value);
    }

    private bool IsPrimedToExplode()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(ANIMSTATE_PRIME_TRANSITION) || stateInfo.IsName(ANIMSTATE_PRIMED);
    }

    private bool IsIdlingOpen() => animator.GetCurrentAnimatorStateInfo(0).IsName(ANIMSTATE_IDLE_OPEN);

    //Start a cycle of MoveTo unit until close and start a timer to explode self
    private void StartAttacking()
    {
        //TODO: Properly handle behaviour changes between active and inactive
        //For now, just set to always be active once we start attacking
        animator.SetBool(ANIMATION_BOOL_ACTIVE, true);
        StartCoroutine(Attacking());
    }

    //Move to, countdown timer, if enemy still close, go boom, otherwise MoveTo again
    private IEnumerator Attacking()
    {
        IDamageable damageTarget = interactionTarget as IDamageable;
        Func<bool> condition = () => damageTarget != null && damageTarget.HpAlpha > 0f;
        while (condition.Invoke())
        {
            if (!IsTargetWithinDistance(damageTarget, out _))
            {
                //Disarm and move
                if (IsPrimedToExplode())
                {
                    animator.SetTrigger(ANIMATION_TRIG_DISARM);
                }

                MoveTo((interactionTarget as MonoBehaviour).transform, StartAttacking, false);
                yield break;
            }
            else
            {
                //Arm, set timer and explode after another proximity check
                animator.SetTrigger(ANIMATION_TRIG_FIRE);
                float time = Time.time;
                yield return new WaitForEndOfFrame();
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(timeToExplode,
                    () => !condition.Invoke() && !IsTargetWithinDistance(damageTarget, out _));

                //If we broke early out of the check, the previous yield was interrupted
                if (Time.time - time > timeToExplode)
                {
                    Explode();
                    yield break;
                }
            }
        }

        //TODO: Seek another target or return to an idle routine
        //For now just disarm if we lose a target
        animator.SetBool(ANIMATION_BOOL_ACTIVE, false);
    }

    //Disable skull prefab as we're blowing to bits when exploding
    protected override void TriggerDeath()
    {
        if (!hasExploded && shouldExplodeOnDeath)
        {
            Explode();
        }

        Destroy(gameObject);
    }

    private void Explode()
    {
        hasExploded = true;

        //Get all enemies in a radius around this unit and apply damage to them
        var damageables = Physics2D.OverlapCircleAll(transform.position, explosionRadius)
            .Select(x => x.gameObject.GetComponent<IDamageable>())
            .Where(x => x != null);

        foreach (var element in damageables) 
        {
            element.ApplyDamage(data.BaseAttackDamage);
        }

        //TODO: Play VFX on self
        Debug.Log("Boom");
    }
}