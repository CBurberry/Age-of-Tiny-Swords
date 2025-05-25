using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinAI : MonoBehaviour
{
    [SerializeField]
    [MinMaxSlider(0f, 300f)]
    private Vector2 randomRangeSpawnTimer;

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
    }

    private void ResetSpawnTimer()
    {
        elapsedSpawnTimer = 0f;
        spawnAtTime = Random.Range(randomRangeSpawnTimer.x, randomRangeSpawnTimer.y);
    }

    private void TriggerAllSpawns()
    {
        foreach (SimpleBuilding building in player.Buildings) 
        {
            //Spawn a unit for the given building type (goblins only have 1 per building)
            building.SpawnUnitInstance(0);
        }
    }
}
