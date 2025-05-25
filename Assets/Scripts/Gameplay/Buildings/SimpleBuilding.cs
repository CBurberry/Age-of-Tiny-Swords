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
    public static event Action<Faction> OnAnyBuildingBuilt;
    public static event Action<Faction> OnAnyBuildingDestroyed;
    public static readonly int MAX_UNITS_QUEUE = 5;


    public BuildingStates State => state;
    public bool IsDamaged => state != BuildingStates.Destroyed && currentHp < maxHp;
    public Faction Faction => faction;
    public float HpAlpha => (float)currentHp / maxHp;
    public IReadOnlyList<UnitCost> SpawnableUnits => data.SpawnableUnits;

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
    private bool hasGarrisonedArchers;

    [ShowIf("hasGarrisonedArchers")]
    [SerializeField]
    private GarrisonedArcher[] garrisonedArchers;

    [SerializeField]
    protected Transform unitsParent;

    [SerializeField]
    protected Transform spawnPoint;

    protected Faction faction;

    private const float damageVisualThreshold = 0.75f;

    BehaviorSubject<List<UnitCost>> _buildQueue = new(new List<UnitCost>());
    BehaviorSubject<float> _currentBuildTime = new(0f);
    public IObservable<List<UnitCost>> ObserveUnitBuildQueue() => _buildQueue;
    public IObservable<float> ObserveUnitBuildProgress() => _currentBuildTime.Select(x =>
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

        //To allow for later overriding if needed
        faction = data.Faction;
    }

    protected void Start()
    {
        if (buildOnStart)
        {
            HidePreview();
            Construct();
            Build(maxHp);
        }
        else 
        {
            state = BuildingStates.PreConstruction;
        }
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
        var buildQueue = _buildQueue.Value;
        var currentBuildTime = _currentBuildTime.Value;
        if (buildQueue.Count > 0)
        {
            currentBuildTime += Time.deltaTime;
            if (currentBuildTime >= buildQueue[0].BuildTime)
            {
                var newUnit = Instantiate(buildQueue[0].UnitToSpawn, spawnPoint.transform.position, Quaternion.identity, unitsParent);
                _currentBuildTime.OnNext(0f);
                buildQueue.RemoveAt(0);
                _buildQueue.OnNext(buildQueue);
            }
            else
            {
                _currentBuildTime.OnNext(currentBuildTime);
            }
        }
    }

    public static IEnumerable<GameObject> GetAllBuildings(Faction faction)
        => GameObject.FindGameObjectsWithTag("Building")
            .Where(x => x.TryGetComponent(out IBuilding building) && building.Faction == faction);

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
            state = BuildingStates.PreConstruction;
            currentHp = 10;
            spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        }
        else 
        {
            //Some enemy buildings don't have a PreConstructed state so just skip to full build
            currentHp = maxHp;
            CompleteConstruction();
        }
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

        currentHp = Math.Clamp(currentHp + amount, currentHp, maxHp);
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

        currentHp = Math.Clamp(currentHp + amount, 0, maxHp);
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
        //Note: maybe make this two tier of intensity when damaged?
        currentHp = Math.Clamp(currentHp - amount, 0, maxHp);

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
        var buildQueue = _buildQueue.Value;
        var resourceManager = GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction).Resources;

        if (_buildQueue.Value.Count >=  MAX_UNITS_QUEUE)
        {
            return false;
        }
        if (!resourceManager.HaveResources(unitCost.Cost))
        {
            return false;
        }

        resourceManager.RemoveResources(unitCost.Cost);
        buildQueue.Add(unitCost);
        _buildQueue.OnNext(buildQueue);

        return true;
    }

    public bool TryRemoveUnitFromQueue(int i)
    {
        var buildQueue = _buildQueue.Value;
        if (i >= buildQueue.Count)
        {
            return false;
        }

        if (i == 0)
        {
            _currentBuildTime.OnNext(0f);
        }
        var resourceManager = GameManager.GetPlayer(GameManager.Instance.CurrentPlayerFaction).Resources;

        resourceManager.AddResources(buildQueue[i].Cost);

        buildQueue.RemoveAt(i);
        _buildQueue.OnNext(buildQueue);
        return true;
    }

    protected virtual void CompleteConstruction()
    {
        state = BuildingStates.Constructed;
        if (data.HasAnimatedPrefab)
        {
            spriteRenderer.enabled = false;
            animatedPrefabInstance = Instantiate(data.ConstructedAnimatedPrefab, visuals.transform);
        }
        else 
        {
            spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        }

        if (hasGarrisonedArchers) 
        {
            foreach (var archer in garrisonedArchers) 
            {
                archer.gameObject.SetActive(true);
            }
        }

        OnAnyBuildingBuilt?.Invoke(faction);
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

        if (hasGarrisonedArchers)
        {
            foreach (var archer in garrisonedArchers)
            {
                archer.gameObject.SetActive(false);
            }
        }

        //Replace visual with destroyed visual
        state = BuildingStates.Destroyed;

        if (animatedPrefabInstance != null) 
        {
            Destroy(animatedPrefabInstance);
            animatedPrefabInstance = null;
            spriteRenderer.enabled = true;
        }

        spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        OnAnyBuildingDestroyed?.Invoke(faction);
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
}
