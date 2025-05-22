using AYellowpaper.SerializedCollections;
using RuntimeStatics;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static Player;

public class PawnUnit : SimpleUnit
{
    public bool CanCarryResources => GetHeldResourcesCount() < maxHeldResourceCount;

    private const string ANIMATION_BOOL_CHOPPING = "IsChopping";
    private const string ANIMATION_BOOL_BUILDING = "IsBuilding";

    //TODO: Implement flag use
    private const string ANIMATION_BOOL_CARRYING = "IsCarrying";

    private const string ANIMSTATE_CARRYING_IDLE = "Carry_Idle";
    private const string ANIMSTATE_CARRYING_RUN = "Carry_Run";

    private const float resourceItemSeekDistance = 3f;

    [SerializeField]
    private HeldResourcesVisual heldResourcesVisual;

    [SerializeField]
    private GameObject resourcePrefab;

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
        bool isCarryingResources = GetHeldResourcesCount() > 0;
        animator.SetBool(ANIMATION_BOOL_CARRYING, isCarryingResources);

        if (isCarryingResources)
        {
            heldResourcesVisual.SetResource(GetMostHeldType());
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            heldResourcesVisual.gameObject.SetActive(stateInfo.IsName(ANIMSTATE_CARRYING_IDLE) || stateInfo.IsName(ANIMSTATE_CARRYING_RUN));
        }
        else 
        {
            heldResourcesVisual.gameObject.SetActive(false);
        }

        //TODO: If full of resources after a gathering task, move to deposit the resources

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
        else if (target is GoldMine)
        {
            MoveTo(target.transform, EnterMine);
            interactionTarget = target;
        }
        else if (target is ResourceItem) 
        {
            MoveTo(target.transform, PickupResource);
            interactionTarget = target;
        }
        else
        {
            throw new NotImplementedException($"[{nameof(PawnUnit)}.{nameof(ResolveResourceInteraction)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    protected override void ResolveDamagableInteraction(ABaseUnitInteractable target, UnitInteractContexts context)
    {
        if (target is Sheep)
        {
            MoveTo(target.transform, StartAttacking);
            interactionTarget = target;
        }
        else
        {
            throw new NotImplementedException($"[{nameof(PawnUnit)}.{nameof(ResolveDamagableInteraction)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    protected override void TriggerDeath()
    {
        base.TriggerDeath();

        //TODO: Add some flair for when these appear
        //Spawn resource items corresponding to carried items, slightly offset their positions from the death prefab
        foreach (var kvp in currentResources)
        {
            if (kvp.Value > 0) 
            {
                //Get a random position within a sphere of a given radius around a point
                Vector3 basePosition = transform.position;
                float radius = UnityEngine.Random.Range(0.5f, 1f);
                Vector3 randomPosition = basePosition + (UnityEngine.Random.insideUnitSphere * radius);

                GameObject gameObject = Instantiate(resourcePrefab, randomPosition, Quaternion.identity, transform.parent);
                ResourceItem resourceItem = gameObject.GetComponent<ResourceItem>();
                resourceItem.Spawn(kvp.Key, kvp.Value);
            }
        }
    }

    private void MoveToNearestBuildingDepositAndReturn(Action onMoveComplete = null)
    {
        if (SimpleBuilding.GetAllBuildings(Faction).Count() == 0) 
        {
            return;
        }

        MoveToNearestBuilding(DepositResources, false);
        MoveTo(interactionTarget.transform, onMoveComplete, false);
    }

    //TODO: Use A* to find the closest rather than linear distance using transform.position
    private void MoveToNearestBuilding(Action onMoveComplete = null, bool clearTarget = true)
    {
        var buildings = SimpleBuilding.GetAllBuildings(Faction);
        if (buildings.Count() == 0) 
        {
            return;
        }

        var target = transform.GetClosestTransform(buildings.Select(x => x.transform));
        MoveTo(target, onMoveComplete, clearTarget);
    }

    private void DepositResources()
    {
        ResourceManager resourceManager = GameManager.GetPlayer(Faction).Resources;
        foreach (var kvp in currentResources) 
        {
            if (kvp.Value > 0) 
            {
                resourceManager.AddResource(kvp.Key, kvp.Value);
            }
        }
    }

    //Start a cycle of MoveTo unit until close and attack (using chop animation as no alternative)
    private void StartAttacking()
    {
        ClearAllAnimationActionFlags();
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
                animator.SetBool(ANIMATION_BOOL_CHOPPING, false);
                MoveTo(interactionTarget.transform, StartAttacking, false);
                yield break;
            }
            else 
            {
                animator.SetBool(ANIMATION_BOOL_CHOPPING, true);
                damageTarget.ApplyDamage(data.BaseAttackDamage);
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }
        }

        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);

        //If the target was a sheep, its dead, and there is some meat on the floor, pick it up and bring it back
        //NOTE: HANDLE CASE WHERE MULTIPLE PAWNS MAY BE TRYING TO ACCESS THE SAME ITEM!
        if (interactionTarget is Sheep)
        {
            OnSheepKill();
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
                //Debug.LogWarning("TODO: Overflow from tree chopped. Need to drop excess as a new wood prefab!");
                break;
            }
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);

        if (overflow > 0 || tree.IsDepleted && GetHeldResourcesCount() > 0) 
        {
            if (tree.IsDepleted) 
            {
                MoveToNearestBuilding(DepositResources);
            }
            else 
            {
                MoveToNearestBuildingDepositAndReturn(StartChoppingTree);
            }
        }
    }

    //Start a cycle of mining until full or mine depleted
    private void EnterMine()
    {
        ClearAllAnimationActionFlags();
        spriteRenderer.enabled = false;
        (interactionTarget as GoldMine).EnterMine(this);
        StartCoroutine(Mining());
    }

    private IEnumerator Mining()
    {
        GoldMine mine = interactionTarget as GoldMine;
        int overflow = 0;
        Func<bool> condition = () => mine != null && !mine.IsDepleted && overflow == 0 && !isMoving;
        while (condition.Invoke())
        {
            AddResource(ResourceType.Gold, mine.Mined(gatherAmountPerSecond), out overflow);
            if (overflow > 0)
            {
                //Debug.LogWarning("TODO: Overflow from mining gold. Need to drop excess as a new gold prefab!");
                break;
            }
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        mine.ExitMine(this);
        spriteRenderer.enabled = true;

        if (overflow > 0 || mine.IsDepleted && GetHeldResourcesCount() > 0)
        {
            if (mine.IsDepleted)
            {
                MoveToNearestBuilding(DepositResources);
            }
            else
            {
                MoveToNearestBuildingDepositAndReturn(EnterMine);
            }
        }
    }

    private void PickupResource()
    {
        if (!CanCarryResources) 
        {
            return;
        }

        //POLISH/TODO: Still in the moving animation when this occurs, looks janky
        ResourceItem item = interactionTarget as ResourceItem;

        if (item == null)
        {
            return;
        }

        int collectedAmount = item.Collect(maxHeldResourceCount - GetHeldResourcesCount());
        if (collectedAmount > 0)
        {
            currentResources[item.ResourceType] += collectedAmount;
        }
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

    /// <summary>
    /// Get the resource type that this unit is holding the most of
    /// </summary>
    /// <returns></returns>
    private ResourceType GetMostHeldType()
        => currentResources.OrderByDescending(x => x.Value).FirstOrDefault().Key;

    private void OnSheepKill()
    {
        var nearbyResourceItem = FindObjectsOfType<ResourceItem>()
                .FirstOrDefault(x => x.ResourceType == ResourceType.Food
                    && Vector3.Distance(transform.position, x.transform.position) <= resourceItemSeekDistance);

        if (nearbyResourceItem == null)
        {
            return;
        }
        else
        {
            interactionTarget = nearbyResourceItem;
            MoveTo(interactionTarget.transform, () =>
            {
                PickupResource();
                if (GetHeldResourcesCount() > 0)
                {
                    MoveToNearestBuilding(DepositResources);
                }
            }, false);
        }
    }
}
