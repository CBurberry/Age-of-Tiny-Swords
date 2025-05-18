using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Common settings for all buildings that share this data.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "TinyWorld/BuildingData")]
public class BuildingData : ScriptableObject
{
    public int MaxHp;
    public bool HasAnimatedPrefab;

    [Tooltip("All the visual states of the building type")]
    public SerializedDictionary<BuildingStates, Sprite> BuildingSpriteVisuals;

    [ShowIf("HasAnimatedPrefab")]
    public GameObject ConstructedAnimatedPrefab;

    //TODO: Resources required to build

    /// <summary>
    /// Get the sprite that shows the visual of the constructed building
    /// </summary>
    /// <returns></returns>
    public static Sprite GetConstructedPreview(BuildingData data)
    {
        return data.BuildingSpriteVisuals[BuildingStates.Constructed];
    }
}

public enum BuildingStates
{
    Constructed = 0,
    PreConstruction,
    Destroyed
}