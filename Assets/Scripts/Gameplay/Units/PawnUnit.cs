using System;
using System.Collections;
using UnityEngine;

public class PawnUnit : SimpleUnit
{
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

    private WaitForSeconds oneSecondWait = new WaitForSeconds(1f);
    private IBuilding buildingTarget => interactionTarget as IBuilding;

    protected override void Update()
    {
        if (isMoving && IsActioning())
        {
            ClearAllAnimationActionFlags();
        }

        base.Update();
    }

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
                throw new NotImplementedException($"[{nameof(SimpleUnit)}.{nameof(Interact)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    //Start a cycle of building until 100% (need to allow interruptions)
    private void StartBuilding() 
    {
        ClearAllAnimationActionFlags();
        animator.SetBool(ANIMATION_BOOL_BUILDING, true);
        StartCoroutine(Building());
    }

    private IEnumerator Building()
    {
        while (buildingTarget.State == BuildingStates.PreConstruction && animator.GetBool(ANIMATION_BOOL_BUILDING)) 
        {
            yield return oneSecondWait;
            buildingTarget.Build(buildAmountPerSecond);
        }

        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
    }

    //Start a cycle of repairing until 100% (need to allow interruptions)
    private void StartRepairing()
    {
        ClearAllAnimationActionFlags();
        animator.SetBool(ANIMATION_BOOL_BUILDING, true);
        StartCoroutine(Repairing());
    }

    private IEnumerator Repairing()
    {
        while (buildingTarget.State == BuildingStates.Constructed 
            && buildingTarget.IsDamaged && animator.GetBool(ANIMATION_BOOL_BUILDING))
        {
            yield return oneSecondWait;
            buildingTarget.Repair(repairAmountPerSecond);
        }

        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
    }

    private void Gather()
    {
        throw new NotImplementedException();
    }

    private void ClearAllAnimationActionFlags()
    {
        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);
    }

    private bool IsActioning()
    {
        return animator.GetBool(ANIMATION_BOOL_BUILDING) && animator.GetBool(ANIMATION_BOOL_CHOPPING);
    }
}
