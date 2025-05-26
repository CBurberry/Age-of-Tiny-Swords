using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
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
    public bool CanBuildMoreUnits => _currentPopulation.Value < populationCap;
    public IObservable<int> ObserveCurrentPopulation() => _currentPopulation;
    public IObservable<int> ObservePopuplationCap() => _currentPopuplationCap;
    public IReadOnlyList<SimpleUnit> Units => _units;
    public IReadOnlyList<SimpleBuilding> Buildings => _buildings;

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

    BehaviorSubject<int> _currentPopulation = new(0);
    BehaviorSubject<int> _currentPopuplationCap = new(0);
    List<SimpleUnit> _units = new();
    List<SimpleBuilding> _buildings = new();

    

    private void Awake()
    {
        Resources = GetComponent<ResourceManager>();
        SetPopulationCap(startingPopulationCap);
        SetLivingPopulation(0);
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

    private void OnUnitSpawned(SimpleUnit simpleUnit)
    {
        if (this.faction != simpleUnit.Faction) 
        {
            return;
        }
        _units.Add(simpleUnit);
        SetLivingPopulation(livingPopulation + 1);
    }

    private void OnUnitKilled(SimpleUnit simpleUnit)
    {
        if (this.faction != simpleUnit.Faction)
        {
            return;
        }

        _units.Remove(simpleUnit);
        SetLivingPopulation(livingPopulation - 1);
        HasDiedCheck();
    }

    private void OnBuildingBuilt(SimpleBuilding building)
    {
        if (this.faction != building.Faction)
        {
            return;
        }

        _buildings.Add(building);
        activeBuildings++;
        SetPopulationCap(populationCap + building.PopulationIncrease);
    }

    private void OnBuildingDestroyed(SimpleBuilding building)
    {
        if (this.faction != building.Faction)
        {
            return;
        }

        _buildings.Remove(building);
        SetPopulationCap(populationCap - building.PopulationIncrease);
        activeBuildings--;
        HasDiedCheck();
    }

    private void HasDiedCheck()
    {
        if (livingPopulation <= 0 && activeBuildings <= 0) 
        {
            OnPlayerDied?.Invoke(Team);
        }
    }

    public static Player GetPlayerByName(string name)
        => GameObject.FindGameObjectsWithTag(PLAYER_TAG)
        .FirstOrDefault(x => x.name == name)
        .GetComponent<Player>();

    void SetLivingPopulation(int newPopuplation)
    {
        livingPopulation = newPopuplation;
        _currentPopulation.OnNext(newPopuplation);
    }
    void SetPopulationCap(int newCap)
    {
        populationCap = newCap;
        _currentPopuplationCap.OnNext(newCap);
    }
}
