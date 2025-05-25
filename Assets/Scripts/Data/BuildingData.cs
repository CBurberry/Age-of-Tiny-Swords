using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Player;

/// <summary>
/// Common settings for all buildings that share this data.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "TinyWorld/BuildingData")]
public class BuildingData : ScriptableObject
{
    public const string PreviewColorString = "#999999";

    public int MaxHp;
    public int PopulationIncrease = 5;
    public bool HasAnimatedPrefab;
    public Faction Faction;

    [Tooltip("All the visual states of the building type")]
    public SerializedDictionary<BuildingStates, Sprite> BuildingSpriteVisuals;

    [ShowIf("HasAnimatedPrefab")]
    public GameObject ConstructedAnimatedPrefab;

    [Tooltip("All units this building can spawn")]
    public List<UnitCost> SpawnableUnits;


    //TODO: Resources required to build

    /// <summary>
    /// Get the sprite that shows the visual of the constructed building
    /// </summary>
    /// <returns></returns>
    public Sprite GetConstructedPreview()
    {
        return BuildingSpriteVisuals[BuildingStates.Constructed];
    }

    /// <summary>
    /// Get the color tint for preview visuals
    /// </summary>
    /// <returns></returns>
    public static Color GetPreviewColor()
    {
        ColorUtility.TryParseHtmlString(PreviewColorString, out var previewColor);
        return previewColor;
    }
}

public enum BuildingStates
{
    Constructed = 0,
    PreConstruction,
    Destroyed
}

[Serializable]
public class UnitCost
{
    public float BuildTime = 5f;
    public SerializedDictionary<ResourceType, int> Cost;
    public AUnitInteractableUnit UnitToSpawn;
}