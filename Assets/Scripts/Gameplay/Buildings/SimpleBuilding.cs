using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using static Player;

/// <summary>
/// Base class governing behaviours that all buildings share e.g. Construction, Destruction.
/// </summary>
public class SimpleBuilding : AUnitInteractableNonUnit, IBuilding
{
    public static event Action<SimpleBuilding> OnAnyBuildingBuilt;
    public static event Action<SimpleBuilding> OnAnyBuildingDestroyed;
    public event Action OnDeath;

    public static readonly int MAX_UNITS_QUEUE = 5;

    public bool IsAnyGarrisonedUnitAttacking => hasGarrisonedRangedUnits && garrisonedRangedUnits.Any(x => x.IsAttacking());
    public BuildingStates State => state;
    public bool IsKilled => currentHp == 0;
    public bool IsDamaged => state != BuildingStates.Destroyed && currentHp < maxHp;
    public Faction Faction => faction;
    public float HpAlpha => (float)currentHp / maxHp;
    public IReadOnlyList<UnitCost> SpawnableUnits => data.SpawnableUnits;
    public Sprite Icon => data.BuildingSpriteVisuals[_buildingState.Value];
    public int PopulationIncrease => data.PopulationIncrease;
    public float FOV
    { 
        get 
        {
            switch (_buildingState.Value)
            {
                // adding in case we want to do something about this
                case BuildingStates.Destroyed:
                    return 0f;
                case BuildingStates.PreConstruction:
                    return 0f;
                case BuildingStates.Constructed:
                    return data.FOV;
            }
            return 0f;
        } 
    }

    [SerializeField]
    //[Expandable] Naughty Attributes can't handle this data
    protected BuildingData data;

    protected int maxHp => data.MaxHp;
    [ShowNonSerializedField]
    protected int currentHp;

    [SerializeField]
    protected GameObject visuals;
    protected GameObject animatedPrefabInstance;
    protected SpriteRenderer spriteRenderer => visuals.GetComponent<SpriteRenderer>();

    [ShowNonSerializedField]
    protected BuildingStates state;

    [SerializeField]
    protected bool buildOnStart;

    [SerializeField]
    protected bool enableAutoRepair;

    [ShowIf("enableAutoRepair")]
    [SerializeField]
    [MinValue(1)]
    protected int repairHpPerSecond;

    [SerializeField]
    private bool hasGarrisonedRangedUnits;

    [ShowIf("hasGarrisonedRangedUnits")]
    [SerializeField]
    private GarrisonedRangedUnit[] garrisonedRangedUnits;

    [SerializeField]
    protected Transform unitsParent;

    [SerializeField]
    protected Transform spawnPoint;

    protected Faction faction;

    private float repairTimer;

    private const float damageVisualThreshold = 0.75f;

    Player _player;
    ResourceManager _resourceManager;
    CompositeDisposable _disposables = new();
    Collider2D[] _colliders;
    BehaviorSubject<BuildingStates> _buildingState = new(BuildingStates.PreConstruction);
    BehaviorSubject<List<UnitCost>> _buildQueue = new(new List<UnitCost>());
    BehaviorSubject<float> _currentUnitBuildTime = new(0f);
    BehaviorSubject<float> _constructionProgress = new(0f);
    BehaviorSubject<int> _currentHp = new(0);

    public Vector3 ColliderOffset => _colliders.Length > 0 ? _colliders[0].offset : Vector3.zero;
    public IObservable<BuildingStates> ObserveBuildingState() => _buildingState;
    public IObservable<List<UnitCost>> ObserveUnitBuildQueue() => _buildQueue;
    public IObservable<float> ObserveConstructionProgress() => _constructionProgress;
    public IObservable<float> ObserveUnitBuildProgress() => _currentUnitBuildTime.Select(x =>
    {
        if (_buildQueue.Value.Count > 0f)
        {
            return x / _buildQueue.Value[0].BuildTime;
        }
        else
        {
            return 0f;
        }
    });

    protected void Awake()
    {
        if (visuals == null)
        {
            visuals = new GameObject("Visuals", typeof(SpriteRenderer));
            visuals.transform.parent = transform;
            visuals.transform.localPosition = Vector3.zero;
            visuals.transform.localRotation = Quaternion.identity;
            visuals.transform.localScale = Vector3.one;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }
        _colliders = GetComponentsInChildren<Collider2D>();
        _buildingState.Select(x => x != BuildingStates.Destroyed)
            .DistinctUntilChanged()
            .Subscribe(requiresCollider =>
            {
                foreach (var collider in _colliders)
                {
                    collider.enabled = requiresCollider;
                }
                if (spawnPoint && requiresCollider && _colliders.Length > 0)
                {
                    Collider2D mainCollider = _colliders[0];
                    ContactFilter2D filter = new ContactFilter2D().NoFilter();
                    Collider2D[] results = new Collider2D[20]; // adjust size as needed

                    int count = mainCollider.OverlapCollider(filter, results);

                    for (int i = 0; i < count; i++)
                    {
                        var result = results[i];
                        if (result.TryGetComponent<SimpleUnit>(out var unit))
                        {
                            unit.transform.position = spawnPoint.transform.position;
                        }
                    }
                }
            }).AddTo(_disposables);
        //To allow for later overriding if needed
        faction = data.Faction;
        repairTimer = 0f;
    }

    protected void Start()
    {
        _player = GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction);
        _resourceManager = _player.Resources;

        if (buildOnStart)
        {
            HidePreview();
            Construct();
            Build(maxHp);
        }
        else 
        {
            SetState(BuildingStates.PreConstruction);
        }
    }

    protected void OnDestroy()
    {
        _disposables.Clear();
    }


    protected void OnValidate()
    {
        if (buildOnStart)
        {
            ShowPreview();
        }
        else 
        {
            HidePreview();
        }
    }

    protected void Update()
    {
        if (enableAutoRepair && state == BuildingStates.Constructed && currentHp < maxHp) 
        {
            repairTimer += Time.deltaTime;
            if (repairTimer > 1f) 
            {
                Repair(repairHpPerSecond);
                repairTimer = 0f;
            }
        }

        var buildQueue = _buildQueue.Value;
        var currentBuildTime = _currentUnitBuildTime.Value;
        if (buildQueue.Count > 0 && _player.CanBuildMoreUnits)
        {
            currentBuildTime += Time.deltaTime;
            if (currentBuildTime >= buildQueue[0].BuildTime)
            {
                var newUnit = Instantiate(buildQueue[0].UnitToSpawn, spawnPoint.transform.position, Quaternion.identity, unitsParent);
                _currentUnitBuildTime.OnNext(0f);
                buildQueue.RemoveAt(0);
                _buildQueue.OnNext(buildQueue);
            }
            else
            {
                _currentUnitBuildTime.OnNext(currentBuildTime);
            }
        }
    }

    public static IEnumerable<GameObject> GetAllBuildings(Faction faction, BuildingStates state = BuildingStates.Constructed)
        => GameObject.FindGameObjectsWithTag("Building")
            .Where(x => x.TryGetComponent(out IBuilding building) && building.Faction == faction && building.State == state);

    public override UnitInteractContexts GetApplicableContexts(SimpleUnit unit)
    {
        if (state == BuildingStates.Destroyed)
        {
            return UnitInteractContexts.None;
        }

        //Check unit faction to determine if can be attacked or used as a resource deposit
        UnitInteractContexts contexts = UnitInteractContexts.None;

        if (unit.Faction != Faction)
        {
            //ABe able to attack any enemy building
            contexts = UnitInteractContexts.Attack;
        }
        else 
        {
            if (state == BuildingStates.PreConstruction)
            {
                contexts |= UnitInteractContexts.Build;
            }
            else if (state == BuildingStates.Constructed)
            {
                if (currentHp < maxHp)
                {
                    contexts |= UnitInteractContexts.Repair;
                }
                else 
                {
                    //Allow deposit resource to any building (when constructed & repaired)
                    contexts |= UnitInteractContexts.Gather;
                }
            }
        }

        //TODO: implement garrison context check (perhaps in derived class e.g. garrisonbuilding)

        return contexts;
    }

    /// <summary>
    /// Initial placement of a building
    /// </summary>
    [Button("Construct (PlayMode)", EButtonEnableMode.Playmode)]
    public virtual void Construct()
    {
        if (currentHp > 0) 
        {
            return;
        }

        if (data.BuildingSpriteVisuals.Keys.Contains(BuildingStates.PreConstruction))
        {
            SetState(BuildingStates.PreConstruction);
            SetHp(10);
            spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        }
        else 
        {
            //Some enemy buildings don't have a PreConstructed state so just skip to full build
            SetHp(maxHp);
            CompleteConstruction();
        }
        _constructionProgress.OnNext(_currentHp.Value / (float)maxHp);
    }

    /// <summary>
    /// Progressive construction of a building. Adds HP to building and when full, updates the visuals and state.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>Boolean indicating completion</returns>
    public virtual bool Build(int amount)
    {
        if (currentHp == maxHp) 
        {
            return false;
        }

        SetHp(Math.Clamp(currentHp + amount, currentHp, maxHp));
        _constructionProgress.OnNext(_currentHp.Value / (float)maxHp);
        if (state == BuildingStates.PreConstruction && currentHp == maxHp)
        {
            CompleteConstruction();
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Progressive repair of a building. Adds HP to building and when full, updates the visuals
    /// e.g. Removing any fire vfx
    /// </summary>
    /// <param name="amount"></param>
    public virtual void Repair(int amount)
    {
        if (currentHp == 0) 
        {
            return;
        }

        SetHp(Math.Clamp(currentHp + amount, 0, maxHp));
        if (state == BuildingStates.Constructed && currentHp > damageVisualThreshold) 
        {
            RemoveDamagedVisual();
        }
    }

    public virtual Vector3 GetClosestPosition(Vector3 position)
    => spriteRenderer.bounds.ClosestPoint(position);

    /// <summary>
    /// Apply damage to building e.g. when attacked
    /// </summary>
    /// <param name="amount"></param>
    public virtual void ApplyDamage(int amount)
    {
        Debug.Log($"Damage to {gameObject.name}, amount {amount}");
        repairTimer = 0f;

        //Note: maybe make this two tier of intensity when damaged?
        SetHp(Math.Clamp(currentHp - amount, 0, maxHp));

        if (currentHp == 0)
        {
            DestroyBuilding();
            return;
        }

        if (state == BuildingStates.Constructed && HpAlpha < damageVisualThreshold) 
        {
            ApplyDamagedVisual();
        }
    }

    public bool TryAddUnitToQueue(UnitCost unitCost)
    {
        if (_buildingState.Value != BuildingStates.Constructed)
        {
            return false;
        }
        var buildQueue = _buildQueue.Value;

        if (_buildQueue.Value.Count >=  MAX_UNITS_QUEUE)
        {
            return false;
        }
        if (!_resourceManager.HaveResources(unitCost.Cost))
        {
            return false;
        }

        _resourceManager.RemoveResources(unitCost.Cost);
        buildQueue.Add(unitCost);
        _buildQueue.OnNext(buildQueue);

        return true;
    }

    public bool TryRemoveUnitFromQueue(int i)
    {
        if (_buildingState.Value != BuildingStates.Constructed)
        {
            return false;
        }

        var buildQueue = _buildQueue.Value;
        if (i >= buildQueue.Count)
        {
            return false;
        }

        if (i == 0)
        {
            _currentUnitBuildTime.OnNext(0f);
        }

        _resourceManager.AddResources(buildQueue[i].Cost);

        buildQueue.RemoveAt(i);
        _buildQueue.OnNext(buildQueue);
        return true;
    }

    public void SpawnUnitInstance(int index)
    {
        Instantiate(SpawnableUnits[index].UnitToSpawn, spawnPoint.transform.position, Quaternion.identity, unitsParent);
    }

    protected virtual void CompleteConstruction()
    {
        SetState(BuildingStates.Constructed);
        if (data.HasAnimatedPrefab)
        {
            spriteRenderer.enabled = false;
            animatedPrefabInstance = Instantiate(data.ConstructedAnimatedPrefab, visuals.transform);
        }
        else 
        {
            spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        }

        if (hasGarrisonedRangedUnits) 
        {
            foreach (var archer in garrisonedRangedUnits) 
            {
                archer.gameObject.SetActive(true);
            }
        }

        OnAnyBuildingBuilt?.Invoke(this);
    }

    protected virtual void ApplyDamagedVisual()
    {
        //TODO: Apply VFX
    }

    protected virtual void RemoveDamagedVisual()
    {
        //TODO: Remove VFX
    }

    private void ShowPreview()
    {
        spriteRenderer.color = BuildingData.GetPreviewColor();
        spriteRenderer.sprite = data.GetConstructedPreview();
    }

    private void HidePreview()
    {
        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = null;
    }

    private void DestroyBuilding()
    {
        if (state == BuildingStates.Destroyed)
        {
            return;
        }

        //TODO: Play vfx e.g. smoke
        //TODO: Remove any fire vfx playing (if any)

        if (hasGarrisonedRangedUnits)
        {
            foreach (var unit in garrisonedRangedUnits)
            {
                unit.gameObject.SetActive(false);
            }
        }

        //Replace visual with destroyed visual
        SetState(BuildingStates.Destroyed);

        if (animatedPrefabInstance != null) 
        {
            Destroy(animatedPrefabInstance);
            animatedPrefabInstance = null;
            spriteRenderer.enabled = true;
        }

        spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        OnDeath?.Invoke();
        OnAnyBuildingDestroyed?.Invoke(this);
    }

    [Button("Build 100% (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void Build()
    {
        if (currentHp > 0 && state == BuildingStates.PreConstruction) 
        {
            Build(maxHp);
        }
    }

    [Button("Damage 10% (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void ApplyDamage()
    {
        ApplyDamage(maxHp / 10);
    }

    [Button("Repair 10% (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void Repair()
    {
        Repair(maxHp / 10);
    }

    [Button("Trigger Destruction (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void TriggerDestroy()
    {
        ApplyDamage(maxHp);
    }

    void SetState(BuildingStates newState)
    { 
        state = newState;
        _buildingState.OnNext(newState);
        switch (newState)
        {
            case BuildingStates.PreConstruction:
            case BuildingStates.Destroyed:
                var buildQueue = _buildQueue.Value;
                buildQueue.Clear();
                _buildQueue.OnNext(buildQueue);
                _currentUnitBuildTime.OnNext(0f);
                break;
        }
    }
    
    void SetHp(int hp)
    {
        currentHp = hp;
        _currentHp.OnNext(hp);
    }
}
