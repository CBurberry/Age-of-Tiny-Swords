using AYellowpaper.SerializedCollections;
using RuntimeStatics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PawnUnit : AUnitInteractableUnit, IDamageable
{
    public bool CanCarryResources => GetHeldResourcesCount() < maxHeldResourceCount;

    private const string ANIMATION_BOOL_CHOPPING = "IsChopping";
    private const string ANIMATION_BOOL_BUILDING = "IsBuilding";
    private const string ANIMATION_BOOL_CARRYING = "IsCarrying";

    private const string ANIMSTATE_CARRYING_IDLE = "Carry_Idle";
    private const string ANIMSTATE_CARRYING_RUN = "Carry_Run";

    private const float RESOURCE_ITEM_SEEK_DISTANCE = 3f;
    private const float TREE_SEEK_DISTANCE = 3f;

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

        bool isCarryingResources = GetHeldResourcesCount() > 0;
        animator.SetBool(ANIMATION_BOOL_CARRYING, isCarryingResources);

        if (isCarryingResources)
        {
            //Set the resource prefab visual being set above the unit's head.
            heldResourcesVisual.SetResource(GetMostHeldType());
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            heldResourcesVisual.gameObject.SetActive(spriteRenderer.enabled 
                && stateInfo.IsName(ANIMSTATE_CARRYING_IDLE) || stateInfo.IsName(ANIMSTATE_CARRYING_RUN));
        }
        else 
        {
            heldResourcesVisual.gameObject.SetActive(false);
        }

        base.Update();
    }

    //Other units can only attack this unit or do nothing
    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
        => unit.Faction != Faction ? UnitInteractContexts.Attack : UnitInteractContexts.None;

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

    protected override void ResolveBuildingInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        IBuilding building = target as IBuilding;
        switch (context)
        {
            case UnitInteractContexts.Attack:
                
                if (building != null && building.HpAlpha > 0f && building.Faction != Faction) 
                {
                    interactionTarget = target;
                    MoveTo((target as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
                }
                break;
            case UnitInteractContexts.Build:
                interactionTarget = target;
                MoveTo((target as MonoBehaviour).transform, StartBuilding, false);
                break;
            case UnitInteractContexts.Repair:
                interactionTarget = target;
                MoveTo((target as MonoBehaviour).transform, StartRepairing, false);
                break;
            case UnitInteractContexts.Gather:
                //Deliver held resources
                if (GetHeldResourcesCount() > 0)
                {
                    interactionTarget = target;
                    MoveTo((target as MonoBehaviour).transform, DepositResources, true);
                    break;
                }
                break;
            default:
                throw new NotImplementedException($"[{nameof(PawnUnit)}.{nameof(ResolveBuildingInteraction)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    protected override void ResolveResourceInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        if (target is Tree)
        {
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, StartChoppingTree, false);
        }
        else if (target is GoldMine)
        {
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, EnterMine, false);
        }
        else if (target is ResourceItem) 
        {
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, PickupResource, false);
        }
        else
        {
            throw new NotImplementedException($"[{nameof(PawnUnit)}.{nameof(ResolveResourceInteraction)}]: Context resolution not implemented for {nameof(PawnUnit)} & {context}!");
        }
    }

    protected override void ResolveDamagableInteraction(IUnitInteractable target, UnitInteractContexts context)
    {
        if (target is AUnitInteractableUnit)
        {
            interactionTarget = target;
            MoveTo((target as MonoBehaviour).transform, StartAttacking, false, stopAtAttackDistance: true);
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

                GameObject gameObject = Instantiate(resourcePrefab, randomPosition, Quaternion.identity, GameManager.Instance.ResourcesParent);
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

        var cachedInteractionTarget = interactionTarget;
        MoveToNearestBuilding(() => 
        {
            DepositResources();
            interactionTarget = cachedInteractionTarget;
            if (interactionTarget != null && !interactionTarget.DestructionPending) 
            {
                MoveTo(interactionTarget.Position, onMoveComplete, false);
            }
        }, true);
    }

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
        var keys = new List<ResourceType>(currentResources.Keys);
        foreach (var key in keys) 
        {
            if (currentResources[key] > 0) 
            {
                resourceManager.AddResource(key, currentResources[key]);
                currentResources[key] = 0;
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
        if (!(interactionTarget as MonoBehaviour))
        {
            Debug.Log($"Unit '{name}' exited early on attacking due to a null interaction target! Was this target a sheep? {lastInteractionTargetType == typeof(Sheep)}");
            if (lastInteractionTargetType == typeof(Sheep)) 
            {
                CollectAllNearbyFoodItems();
            }
            yield break;
        }

        bool isAttackingSheep = interactionTarget is Sheep;
        IDamageable damageTarget = interactionTarget as IDamageable;
        Func<bool> condition = () => damageTarget != null && damageTarget.HpAlpha > 0f && IUnitInteractable.IsValid(interactionTarget);
        while (condition.Invoke())
        {
            //Check we are at the target (proximity check? bounds?)
            if (!IsTargetWithinDistance(damageTarget, out Vector3 closestPoint))
            {
                animator.SetBool(ANIMATION_BOOL_CHOPPING, false);
                MoveTo(closestPoint, StartAttacking, false, stopAtAttackDistance: true);
                yield return new WaitForEndOfFrame();
                yield break;
            }
            else 
            {
                animator.SetBool(ANIMATION_BOOL_CHOPPING, true);
                damageTarget.ApplyDamage(data.BaseAttackDamage, this);
                yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f / data.AttackSpeed, () => !condition.Invoke());
            }

            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"Unit '{name}' completed attacking. Was attacking sheep: {isAttackingSheep}");
        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);
        if (isAttackingSheep)
        {
            CollectAllNearbyFoodItems();
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
        Func<bool> condition = () => buildingTarget != null && buildingTarget.State == BuildingStates.PreConstruction && !isMoving && IUnitInteractable.IsValid(interactionTarget);
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
            && buildingTarget.IsDamaged && !isMoving && IUnitInteractable.IsValid(interactionTarget);

        while (condition.Invoke())
        {
            buildingTarget.Repair(repairAmountPerSecond);
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        animator.SetBool(ANIMATION_BOOL_BUILDING, false);
    }

    private void TryToFindNearbyTreeToChop()
    {
        var target = Physics2D.OverlapCircleAll(transform.position, TREE_SEEK_DISTANCE)
            .Where(x => x != null && x.gameObject.TryGetComponent(out Tree tree) && !tree.IsDepleted && !tree.DestructionPending)
            .Take(3)
            .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
            .FirstOrDefault();

        if (target != null && target.gameObject.TryGetComponent(out Tree tree)) 
        {
            interactionTarget = tree;
            MoveTo(interactionTarget.Position, StartChoppingTree, false);
        }
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
        Func<bool> condition = () => tree != null && !tree.IsDepleted && overflow == 0 && !isMoving && IUnitInteractable.IsValid(interactionTarget);
        while (condition.Invoke()) 
        {
            AddResource(ResourceType.Wood, tree.Chopped(gatherAmountPerSecond), out overflow);
            if (overflow > 0) 
            {
                break;
            }
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        animator.SetBool(ANIMATION_BOOL_CHOPPING, false);

        if (overflow > 0 || tree.IsDepleted && GetHeldResourcesCount() > 0) 
        {
            if (tree.IsDepleted) 
            {
                MoveToNearestBuildingDepositAndReturn(TryToFindNearbyTreeToChop);
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
        EnterBuilding();
        (interactionTarget as GoldMine).EnterMine(this);
        StartCoroutine(Mining());
    }

    private IEnumerator Mining()
    {
        GoldMine mine = interactionTarget as GoldMine;
        int overflow = 0;
        Func<bool> condition = () => mine != null && !mine.IsDepleted && mine.IsBeingMined && overflow == 0 && !isMoving && IUnitInteractable.IsValid(interactionTarget);
        while (condition.Invoke())
        {
            AddResource(ResourceType.Gold, mine.Mined(gatherAmountPerSecond), out overflow);
            if (overflow > 0)
            {
                break;
            }
            yield return RuntimeStatics.CoroutineUtilities.WaitForSecondsWithInterrupt(1f, () => !condition.Invoke());
        }

        mine.ExitMine(this);
        ExitBuilding();

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

    private void CollectAllNearbyFoodItems()
    {
        var allResourceItems = FindObjectsOfType<ResourceItem>();
        var nearbyFoodItems = allResourceItems.Where(x => x.ResourceType == ResourceType.Food
                    && Vector3.Distance(transform.position, x.transform.position) <= RESOURCE_ITEM_SEEK_DISTANCE);
        var firstFoodItem = nearbyFoodItems.FirstOrDefault();
        if (firstFoodItem == null)
        {
            Debug.Log($"Unit '{name}' found no resources to collect.");
            return;
        }
        else
        {
            interactionTarget = firstFoodItem;
            MoveTo(interactionTarget.Position, () =>
            {
                PickupResource();
                if (GetHeldResourcesCount() > 0)
                {
                    int collections = GetResourceCollectionIterations(nearbyFoodItems);
                    if (collections > 0)
                    {
                        MoveToNearestBuildingDepositAndReturn(CollectAllNearbyFoodItems);
                    }
                    else 
                    {
                        MoveToNearestBuilding(DepositResources);
                    }   
                }
            }, false);
        }
    }

    private int GetResourceCollectionIterations(IEnumerable<ResourceItem> resources)
    {
        int resourcesCount = 0;
        foreach (var resource in resources) 
        {
            resourcesCount += resource.ResourceCount;
        }
        return Mathf.CeilToInt((float)resourcesCount / maxHeldResourceCount);
    }

    private int GetResourceCollectionIterations(ResourceItem resource)
        => Mathf.CeilToInt((float)resource.ResourceCount / maxHeldResourceCount);
}
