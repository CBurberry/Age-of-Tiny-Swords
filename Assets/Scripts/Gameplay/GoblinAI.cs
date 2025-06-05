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
    [Header("Idling")]
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

    [InfoBox("IDLING IS DISABLED DUE TO PATHFINDING LOAD!", EInfoBoxType.Warning)]

    [Header("Attacking")]
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

    //(1) Spawning logic will be capped to at most one spawn up to a maximum of 20 per spawn timer elapse
    //    cap increases for every time the spawn timer elapses
    //(2) Attack order will be suppressed for the first 10 minutes of the game
    private float gameTime;
    private const float attackMissiveSuppresion = 600f;
    private int spawnedThisWave;
    private int maxSpawns;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        gameTime = 0f;
        ResetSpawnTimer();
        ResetUnitAttackTimer();
        ResetUnitUpdateTimer();
    }

    private void Update()
    {
        gameTime += Time.deltaTime;
        maxSpawns = (int)Mathf.Clamp(gameTime * 0.0166f, 0f, 20f);

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

        //Disabling goblin idle movement due to pathfinding load limits
        /*elapsedUnitUpdateTimer += Time.deltaTime;
        if (elapsedUnitUpdateTimer > updateAtTime)
        {
            UpdateUnits();
            ResetUnitUpdateTimer();
        }*/
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
        spawnedThisWave = 0;
        AUnitInteractableUnit spawnedUnit = null;
        Vector3 randomPoint;
        foreach (SimpleBuilding building in player.Buildings)
        {
            //Exit early if we can't spawn yet
            if (spawnedThisWave >= maxSpawns) 
            {
                return;
            }

            //Spawn a unit for the given building type (goblins only have 1 per building)
            if (building != null && building.State == BuildingStates.Constructed)
            {
                spawnedUnit = building.SpawnUnitInstance(0);
                spawnedThisWave++;

                //Move the spawn slightly so they're not all stacked on each other
                randomPoint = Random.insideUnitCircle;
                spawnedUnit.MoveTo(building.SpawnPosition + new Vector3(randomPoint.x, randomPoint.y, 0f));
            }
        }

        //Spawn a Explosive Barrel at a random spawn
        SimpleBuilding randomBuilding = player.Buildings.Where(x => x != null && x.State == BuildingStates.Constructed)
            .ToList().PickRandom();

        randomPoint = Random.insideUnitCircle;
        spawnedUnit = Instantiate(barrel, randomBuilding.SpawnPosition, Quaternion.identity, GameManager.Instance.UnitsParent);
        spawnedUnit.MoveTo(randomBuilding.SpawnPosition + new Vector3(randomPoint.x, randomPoint.y, 0f));
    }

    private void IssueAttackMissive()
    {
        //Ignore ordering missives during the start of the game
        if (gameTime < attackMissiveSuppresion) 
        {
            return;
        }

        //Determine a random number of units (max all alive units), to send at the enemy at once
        int attackForceSize = Random.Range(randomRangeAttackingUnitCount.x, randomRangeAttackingUnitCount.y);
        if (attackForceSize == 0)
        {
            return;
        }

        //Select a random number of units from the unit pool that are idling
        List<SimpleUnit> allIdlingUnits = GetIdlingUnits();
        int idleCount = allIdlingUnits.Count;
        Debug.Log($"{nameof(IssueAttackMissive)}: Idle Unit Count ({allIdlingUnits.Count})");

        if (idleCount == 0) 
        {
            return;
        }

        int rand = Math.Min(attackForceSize, idleCount);
        IEnumerable<SimpleUnit> attackCandidates = allIdlingUnits.PickRandom(rand);

        //Pick a random attack target from the Knights faction
        MonoBehaviour attackTarget = GetRandomEnemyBuilding();
        if (attackTarget == null) 
        {
            attackTarget = GetRandomEnemyUnit();
        }

        if (attackTarget != null)
        {
            Debug.Log($"{nameof(IssueAttackMissive)}: Requested Force ({attackForceSize}), Actual Force ({attackCandidates.Count()}), Target: {attackTarget.name}");

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
        //For now, just move in a small radius about position
        Vector3 randomPoint = Random.insideUnitCircle;
        unit.MoveTo(unit.transform.position + new Vector3(randomPoint.x, randomPoint.y, 0f));
    }
}
