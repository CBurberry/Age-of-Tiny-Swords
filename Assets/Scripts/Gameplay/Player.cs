using NaughtyAttributes;
using System;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum Faction
    {
        None,
        Knights,
        Goblins
    }

    private const string PLAYER_TAG = "Player";

    public Faction Team => faction;
    public event Action<Faction> OnPlayerDied;

    [HideInInspector]
    public ResourceManager Resources;

    [SerializeField]
    private Faction faction;

    [SerializeField]
    private int startingPopulationCap;

    [SerializeField]
    private int populationCap;

    [SerializeField]
    [ReadOnly]
    private int livingPopulation;

    [SerializeField]
    [ReadOnly]
    private int activeBuildings;

    private void Awake()
    {
        Resources = GetComponent<ResourceManager>();
        livingPopulation  = activeBuildings = 0;
    }

    private void Start()
    {
        populationCap = startingPopulationCap;
    }

    private void OnEnable()
    {
        SimpleUnit.OnAnyUnitSpawned += OnUnitSpawned;
        SimpleUnit.OnAnyUnitDied += OnUnitKilled;
        SimpleBuilding.OnAnyBuildingBuilt += OnBuildingBuilt;
        SimpleBuilding.OnAnyBuildingDestroyed += OnBuildingDestroyed;
    }

    private void OnDisable()
    {
        SimpleUnit.OnAnyUnitSpawned -= OnUnitSpawned;
        SimpleUnit.OnAnyUnitDied -= OnUnitKilled;
        SimpleBuilding.OnAnyBuildingBuilt -= OnBuildingBuilt;
        SimpleBuilding.OnAnyBuildingDestroyed -= OnBuildingDestroyed;
    }

    private void OnUnitSpawned(Faction faction)
    {
        if (this.faction != faction) 
        {
            return;
        }

        livingPopulation++;
    }

    private void OnUnitKilled(Faction faction)
    {
        if (this.faction != faction)
        {
            return;
        }

        livingPopulation--;
        HasDiedCheck();
    }

    private void OnBuildingBuilt(Faction faction)
    {
        if (this.faction != faction)
        {
            return;
        }

        activeBuildings++;
    }

    private void OnBuildingDestroyed(Faction faction)
    {
        if (this.faction != faction)
        {
            return;
        }

        activeBuildings--;
        HasDiedCheck();
    }

    private void HasDiedCheck()
    {
        if (livingPopulation == 0 && activeBuildings == 0) 
        {
            OnPlayerDied?.Invoke(Team);
        }
    }

    public static Player GetPlayerByName(string name)
        => GameObject.FindGameObjectsWithTag(PLAYER_TAG)
        .FirstOrDefault(x => x.name == name)
        .GetComponent<Player>();
}
