using NaughtyAttributes;
using RuntimeStatics;
using System;
using UnityEngine;

public abstract class AUnitInteractableUnit : SimpleUnit, IUnitInteractable
{
    [SerializeField]
    [Tooltip("What interaction types can this unit trigger?")]
    [EnumFlags]
    private UnitInteractContexts interactableContexts;

    //Set this to true when calling Destroy
    private bool destroyCalled;

    public bool DestructionPending => destroyCalled;

    public Vector3 Position => transform.position;

    public UnitInteractContexts GetContexts() => interactableContexts;

    public abstract UnitInteractContexts GetApplicableContexts(SimpleUnit unit);

    public virtual bool CanInteract(SimpleUnit unit, UnitInteractContexts contexts, out UnitInteractContexts interactableContexts)
    {
        interactableContexts = contexts & GetApplicableContexts(unit);
        return interactableContexts != UnitInteractContexts.None;
    }

    /// <summary>
    /// Get this unit to interact with a given target.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="context">Singular context</param>
    /// <returns>Success/Failure</returns>
    public virtual bool Interact(IUnitInteractable target, UnitInteractContexts context)
    {
        Debug.Log($"Selected Unit '{this.name}', target to interact with '{(target as MonoBehaviour)?.name}', param contexts '{context}'");

        if (!target.CanInteract(this, context, out UnitInteractContexts availableContexts))
        {
            Debug.Log("Could not interact");
            return false;
        }

        if (BitwiseHelpers.GetSetBitCount((long)availableContexts) != 1)
        {
            throw new ArgumentException($"[{nameof(SimpleUnit)}.{nameof(Interact)}]: Cannot resolve more than 1 interaction context at once!");
        }

        if (target is IBuilding)
        {
            ResolveBuildingInteraction(target, availableContexts);
        }
        else if (target is IResourceSource)
        {
            ResolveResourceInteraction(target, availableContexts);
        }
        else if (target is IDamageable && (target as IDamageable).Faction != Faction)
        {
            ResolveDamagableInteraction(target, availableContexts);
        }
        else
        {
            throw new NotImplementedException("TODO: define interaction behaviour with '" + (target as MonoBehaviour)?.name + "'");
        }
        return true;
    }

    protected virtual void ResolveBuildingInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(ResolveBuildingInteraction)}]: Context resolution not implemented for {nameof(SimpleUnit)}!");
    }

    protected virtual void ResolveDamagableInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(ResolveDamagableInteraction)}]: Context resolution not implemented for {nameof(SimpleUnit)}!");
    }

    protected virtual void ResolveResourceInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(ResolveResourceInteraction)}]: Context resolution not implemented for {nameof(SimpleUnit)}!");
    }

    protected bool IsTargetWithinDistance(IDamageable target, out Vector3 closestPosition)
    {
        closestPosition = target.GetClosestPosition(transform.position);
        float magnitude = (closestPosition - transform.position).magnitude;
        //Debug.Log($"Unit '{name}' (position: {transform.position}) checking distance against target (closest position: {closestPosition}), magnitude {magnitude}, attackDistance {data.AttackDistance}, result: {magnitude <= data.AttackDistance}");
        return magnitude <= data.AttackDistance;
    }

    [Button("Interact with target (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void InteractWith()
    {
        Interact(moveToTargetTransform.gameObject.GetComponent<IUnitInteractable>(), interactableContexts);
    }

    protected override void TriggerDeath()
    {
        if (!spriteRenderer.enabled)
        {
            return;
        }

        //Hide self
        spriteRenderer.enabled = false;

        if (spawnDeadPrefab)
        {
            //Replace this prefab with a spawned instance of the death prefab
            DeadUnit deadUnit = Instantiate(deadPrefab, transform.position, Quaternion.identity, GameManager.Instance.UnitsParent);
            deadUnit.name = deadUnit.UnitName = name;
        }

        InvokeDeathEvents();
        DestroySelf();
    }

    protected virtual void DestroySelf()
    {
        destroyCalled = true;
        Destroy(gameObject);
    }
}
