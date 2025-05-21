using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using UnityEngine;

public class PawnUnit : SimpleUnit
{
    public bool CanCarryResources => GetHeldResourcesCount() < maxHeldResourceCount;

    private const string ANIMATION_BOOL_CHOPPING = "IsChopping";
    private const string ANIMATION_BOOL_BUILDING = "IsBuilding";

    //TODO: Implement flag use
    private const string ANIMATION_BOOL_CARRYING = "IsCarrying";

    [SerializeField]
    private int buildAmountPerSecond = 10;

    [SerializeField]
    private int repairAmountPerSecond = 12;

    [SerializeField]
    private int gatherAmountPerSecond = 2;

    [SerializeField]
    private int maxHeldResourceCount = 30;

    [SerializeField]
    private SerializedDictionary<ResourceType, int> currentResources;

    private IBuilding buildingTarget => interactionTarget as IBuilding;

    protected override void Update()
    {
        if (isMoving && IsActioning())
        {
            ClearAllAnimationActionFlags();
        }

        //TODO: Set the resource prefab visual being set above the unit's head.
        animator.SetBool(ANIMATION_BOOL_CARRYING, GetHeldResourcesCount() > 0);

        base.Update();
    }

    public void AddResource(ResourceType resource, int amount, out int overflow)
    {
        int heldCount = GetHeldResourcesCount();
        if (heldCount == maxHeldResourceCount) 
        {
            overflow = amount;
            return;
        }

        if (heldCount + amount > maxHeldResourceCount)
        {
            overflow = heldCount + amount - maxHeldResourceCount;
            currentResources[resource] += amount - overflow;
        }
        else 
        {
            overflow = 0;
            currentResources[resource] += amount;
        }
    }

    public void RemoveResource(ResourceType resource, int amount, out int partialfill)
    {
        if (currentResources[resource] == 0)
        {
            partialfill = amount;
            return;
        }

        if (currentResources[resource] - amount < 0)
        {
            partialfill = currentResources[resource];
            currentResources[resource] = 0;
        }
        else 
        {
            partialfill = 0;
            currentResources[resource] -= amount;
        }
    }

    public int GetHeldResourcesCount()
        => currentResources[ResourceType.Food] + currentResources[ResourceType.Gold] + currentResources[ResourceType.Wood];

    protected override void ResolveBuildingInteraction(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        switch (context)
        {
            case UnitInteractContexts.Build:
                MoveTo(target.transform, StartBuilding);
                interactionTarget = target;
                break;
            case UnitInteractContexts.Repair:
                MoveTo(target.transform, StartRepairing);
                interactionTarget = target;
                break;
            default:
                throw new NotImplementedException($"[{nameof(PawnUnit)}.{nameof(ResolveBuildingInteraction)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    protected override void ResolveResourceInteraction(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        if (target is Tree)
        {
            MoveTo(target.transform, StartChoppingTree);
            interactionTarget = target;
        }
        else 
        {
            throw new NotImplementedException($"[{nameof(PawnUnit)}.{nameof(ResolveResourceInteraction)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    //Start a cycle of building until 100%
    private void StartBuilding() 
    {
        ClearAllAnimationActionFlags();
        animator.SetBool(ANIMATION_BOOL_BUILDING, true);
        StartCoroutine(Building());
    }

    private IEnumerator Building()
    {
        Func<bool> condition = () => buildingTarget != null && buildingTarget.State == BuildingStates.PreConstruction && !isMoving;
        while (condition.Invoke()) 
        {
            buildingTarget.Build(buildAmountPerSecond);
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
    }

    //Start a cycle of repairing until 100
    private void StartRepairing()
    {
        ClearAllAnimationActionFlags();
        animator.SetBool(ANIMATION_BOOL_BUILDING, true);
        StartCoroutine(Repairing());
    }

    private IEnumerator Repairing()
    {
        Func<bool> condition = () => buildingTarget != null && buildingTarget.State == BuildingStates.Constructed
            && buildingTarget.IsDamaged && !isMoving;

        while (condition.Invoke())
        {
            buildingTarget.Repair(repairAmountPerSecond);
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
    }

    //Start a cycle of building until full or tree felled
    private void StartChoppingTree()
    {
        ClearAllAnimationActionFlags();
        animator.SetBool(ANIMATION_BOOL_CHOPPING, true);
        StartCoroutine(Chopping());
    }

    private IEnumerator Chopping()
    {
        Tree tree = interactionTarget as Tree;
        int overflow = 0;
        Func<bool> condition = () => tree != null && !tree.IsDepleted && overflow == 0 && !isMoving;
        while (condition.Invoke()) 
        {
            AddResource(ResourceType.Wood, tree.Chopped(gatherAmountPerSecond), out overflow);
            if (overflow > 0) 
            {
                Debug.LogWarning("TODO: Overflow from tree chopped. Need to drop excess as a new wood prefab!");
                break;
            }
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);
    }

    private void Gather()
    {
        if (!CanCarryResources) 
        {
            return;
        }

        throw new NotImplementedException();
    }

    private void ClearAllAnimationActionFlags()
    {
        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);
    }

    private bool IsActioning()
    {
        return animator.GetBool(ANIMATION_BOOL_BUILDING) || animator.GetBool(ANIMATION_BOOL_CHOPPING);
    }
}
