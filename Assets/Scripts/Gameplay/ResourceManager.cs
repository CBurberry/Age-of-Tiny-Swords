using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using System;
using System.Linq;
using UniRx;
using UnityEngine;

/// <summary>
/// Tracks the maximums and current values of a set of resources. Can be used for both players and AI.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    BehaviorSubject<SerializedDictionary<ResourceType, int>> _currentResourceUpdated = new(null);

    [SerializeField]
    private SerializedDictionary<ResourceType, int> resourceLimits;

    [SerializeField]
    private SerializedDictionary<ResourceType, int> currentResources;

#if UNITY_EDITOR
    private int maxFoodCount => resourceLimits[ResourceType.Food];
    private int maxGoldCount => resourceLimits[ResourceType.Gold];
    private int maxWoodCount => resourceLimits[ResourceType.Wood];

    [SerializeField]
    [ProgressBar("Food", "maxFoodCount", EColor.Red)]
    private int food;

    [SerializeField]
    [ProgressBar("Gold", "maxGoldCount", EColor.Yellow)]
    private int gold;

    [SerializeField]
    [ProgressBar("Wood", "maxWoodCount", EColor.Green)]
    private int wood;
#endif

    public IObservable<SerializedDictionary<ResourceType, int>> ObserveCurrentResourcesUpdated() => _currentResourceUpdated;

    private void Awake()
    {
        _currentResourceUpdated.OnNext(currentResources);
    }

    private void Update()
    {
        //Just to show current levels without developing a custom UI
        UpdateEditorProgressBars();
    }

    public int GetResourceLimit(ResourceType type) => resourceLimits[type];
    public int GetResourceCount(ResourceType type) => currentResources[type];

    public void AddResource(ResourceType type, int amount)
    {
        currentResources[type] = Math.Clamp(currentResources[type] + amount, 0, resourceLimits[type]);
        _currentResourceUpdated.OnNext(currentResources);
    }

    public void RemoveResource(ResourceType type, int amount)
    {
        currentResources[type] = Math.Clamp(currentResources[type] - amount, 0, resourceLimits[type]);
        _currentResourceUpdated.OnNext(currentResources);
    }

    public bool HaveResources(SerializedDictionary<ResourceType, int> resources)
    {
        return resources.All(x => currentResources[x.Key] >= x.Value);
    }

    public void RemoveResources(SerializedDictionary<ResourceType, int> resources)
    {
        foreach (var iter in resources)
        {
            RemoveResource(iter.Key, iter.Value);
        }
    }

    private void UpdateEditorProgressBars()
    {
#if UNITY_EDITOR
        food = currentResources[ResourceType.Food];
        gold = currentResources[ResourceType.Gold];
        wood = currentResources[ResourceType.Wood];
#endif
    }

    private void OnValidate()
    {
        UpdateEditorProgressBars();
    }
}

public enum ResourceType
{
    Food = 0,
    Gold = 1,
    Wood = 2,
}
