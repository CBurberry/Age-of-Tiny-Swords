using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using System;
using UnityEngine;

/// <summary>
/// Tracks the maximums and current values of a set of resources. Can be used for both players and AI.
/// </summary>
public class ResourceManager : MonoBehaviour
{
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
    [ProgressBar("Gold", "maxGoldCount", EColor.Blue)]
    private int gold;

    [SerializeField]
    [ProgressBar("Stamina", "maxWoodCount", EColor.Green)]
    private int wood;
#endif

    private void Update()
    {
        //Just to show current levels without developing a custom UI
        UpdateEditorProgressBars();
    }

    public int GetResourceLimit(ResourceType type) => resourceLimits[type];
    public int GetResourceCount(ResourceType type) => currentResources.Count;

    public void AddResource(ResourceType type, int amount)
    {
        currentResources[type] = Math.Clamp(currentResources[type] + amount, 0, resourceLimits[type]);
    }

    public void RemoveResource(ResourceType type, int amount)
    {
        currentResources[type] = Math.Clamp(currentResources[type] - amount, 0, resourceLimits[type]);
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
    Gold,
    Wood
}
