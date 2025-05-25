using NaughtyAttributes;
using RuntimeStatics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GoblinAI : MonoBehaviour
{
    [Header("Asset References")]
    [SerializeField]
    private ExplodingUnit barrel;

    [Header("Behaviours")]
    [SerializeField]
    private float unitIdleMinTime;

    [SerializeField]
    [MinMaxSlider(0f, 300f)]
    [Label("Delay Between Updates")]
    private Vector2 randomRangeDelayBetweenUnitUpdates;

    [SerializeField]
    private float elapsedUnitUpdateTimer;

    [SerializeField]
    [ReadOnly]
    private float updateAtTime;

    [SerializeField]
    [MinMaxSlider(1, 100)]
    [Label("Attack Force Size")]
    private Vector2Int randomRangeAttackingUnitCount;

    [SerializeField]
    [MinMaxSlider(0f, 300f)]
    [Label("Delay Between Attacks")]
    private Vector2 randomRangeDelayBetweenAttackOrders;

    [SerializeField]
    private float elapsedAttackOrderTimer;

    [SerializeField]
    [ReadOnly]
    private float attackAtTime;

    [Header("Spawning")]
    [SerializeField]
    [MinMaxSlider(0f, 300f)]
    [Label("Spawn Time")]
    private Vector2 randomRangeSpawnTime;

    [SerializeField]
    private float elapsedSpawnTimer;

    [SerializeField]
    [ReadOnly]
    private float spawnAtTime;
    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        ResetSpawnTimer();
        ResetUnitAttackTimer();
        ResetUnitUpdateTimer();
    }

    private void Update()
    {
        if (player.CanBuildMoreUnits)
        {
            elapsedSpawnTimer += Time.deltaTime;
            if (elapsedSpawnTimer > spawnAtTime)
            {
                TriggerAllSpawns();
                ResetSpawnTimer();
            }
        }

        elapsedAttackOrderTimer += Time.deltaTime;
        if (elapsedAttackOrderTimer > attackAtTime)
        {
            IssueAttackMissive();
            ResetUnitAttackTimer();
        }


        elapsedUnitUpdateTimer += Time.deltaTime;
        if (elapsedUnitUpdateTimer > updateAtTime)
        {
            //TODO: Test/Implement this one after attack missive is done
            //UpdateUnits();
            //ResetUnitUpdateTimer();
        }
    }

    private void ResetSpawnTimer()
    {
        elapsedSpawnTimer = 0f;
        spawnAtTime = Random.Range(randomRangeSpawnTime.x, randomRangeSpawnTime.y);
    }

    private void ResetUnitUpdateTimer()
    {
        elapsedUnitUpdateTimer = 0f;
        updateAtTime = Random.Range(randomRangeDelayBetweenUnitUpdates.x, randomRangeDelayBetweenUnitUpdates.y);
    }

    private void ResetUnitAttackTimer()
    {
        elapsedAttackOrderTimer = 0f;
        attackAtTime = Random.Range(randomRangeDelayBetweenAttackOrders.x, randomRangeDelayBetweenAttackOrders.y);
    }

    private void TriggerAllSpawns()
    {
        foreach (SimpleBuilding building in player.Buildings)
        {
            //Spawn a unit for the given building type (goblins only have 1 per building)
            if (building != null && building.State == BuildingStates.Constructed)
            {
                building.SpawnUnitInstance(0);
            }
        }

        //Spawn a Explosive Barrel at a random spawn
        SimpleBuilding randomBuilding = player.Buildings.Where(x => x != null && x.State == BuildingStates.Constructed)
            .ToList().PickRandom();

        Instantiate(barrel, randomBuilding.SpawnPosition, Quaternion.identity, GameManager.Instance.UnitsParent);
    }

    private void IssueAttackMissive()
    {
        //Determine a random number of units (max all alive units), to send at the enemy at once
        int attackForceSize = Random.Range(randomRangeAttackingUnitCount.x, randomRangeAttackingUnitCount.y);
        if (attackForceSize == 0)
        {
            return;
        }

        //Select a random number of units from the unit pool that are idling
        List<SimpleUnit> allIdlingUnits = GetIdlingUnits();
        Debug.Log($"{nameof(IssueAttackMissive)}: Idle Unit Count ({allIdlingUnits.Count})");
        int rand = Math.Min(attackForceSize, allIdlingUnits.Count);
        Debug.Log($"{nameof(IssueAttackMissive)}: rand ({rand})");
        IEnumerable<SimpleUnit> attackCandidates = allIdlingUnits.PickRandom(rand);

        //Pick a random attack target from the Knights faction
        MonoBehaviour attackTarget = GetRandomEnemyBuilding();
        if (attackTarget == null) 
        {
            attackTarget = GetRandomEnemyUnit();
        }

        Debug.Log($"{nameof(IssueAttackMissive)}: Requested Force ({attackForceSize}), Actual Force ({attackCandidates.Count()}), Target: {attackTarget.name}");
        if (attackTarget != null)
        {
            foreach (var unit in attackCandidates)
            {
                //Just move the forces there, proximity attack will deal with the rest
                unit.MoveTo(attackTarget.transform);
            }
        }
    }

    private void UpdateUnits()
    {
        var idlingUnits = GetIdlingUnits();
        idlingUnits.ForEach(unit => GiveUnitOptionalRoutine(unit));
    }

    private List<SimpleUnit> GetIdlingUnits()
    {
        List<SimpleUnit> idlingUnits = new List<SimpleUnit>();
        foreach (SimpleUnit unit in player.Units)
        {
            if (unit == null)
            {
                continue;
            }

            //Check for BehaviourTimer component, if it doesnt have one, create and continue
            if (!unit.TryGetComponent(out UnitIdleTimer unitIdleTimer))
            {
                unit.gameObject.AddComponent<UnitIdleTimer>();
                continue;
            }

            //Check is this unit idle, if so, assign a new behaviour
            if (unit.IsIdle && unitIdleTimer.Timer > unitIdleMinTime)
            {
                idlingUnits.Add(unit);
            }
        }

        return idlingUnits;
    }

    private SimpleBuilding GetRandomEnemyBuilding()
     => GameManager.Instance.Knights.Buildings.Where(x => x != null && x.State == BuildingStates.Constructed).PickRandom();

    private SimpleUnit GetRandomEnemyUnit()
     => GameManager.Instance.Knights.Units.Where(x => x != null && !x.IsKilled).PickRandom();

    private void GiveUnitOptionalRoutine(SimpleUnit unit)
    {
        //TODO: Select a routine between moving to a predetermined transform or to attack an enemy
        //TODO: Arrange a timer or condition group that gives a mandate to go attack the enemy all at once for a subset of units
    }
}
