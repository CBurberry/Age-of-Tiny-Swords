using NaughtyAttributes;
using System;
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

    private void OnUnitSpawned(Faction faction)
    {
        if (this.faction != faction) 
        {
            return;
        }

        SetLivingPopulation(livingPopulation + 1);
    }

    private void OnUnitKilled(Faction faction)
    {
        if (this.faction != faction)
        {
            return;
        }

        SetLivingPopulation(livingPopulation - 1);

        HasDiedCheck();
    }

    private void OnBuildingBuilt(BuildingData buildingData)
    {
        if (this.faction != buildingData.Faction)
        {
            return;
        }

        activeBuildings++;
        SetPopulationCap(populationCap + buildingData.PopulationIncrease);
    }

    private void OnBuildingDestroyed(BuildingData buildingData)
    {
        if (this.faction != buildingData.Faction)
        {
            return;
        }

        SetPopulationCap(populationCap - buildingData.PopulationIncrease);
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
