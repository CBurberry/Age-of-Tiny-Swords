using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using System.Collections.Generic;
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

    [HideIf("HasAnimatedPrefab")]
    [Tooltip("Alternate visuals for fully constructed building (if any)")]
    public List<Sprite> ConstructedVariants;

    [ShowIf("HasAnimatedPrefab")]
    public GameObject ConstructedAnimatedPrefab;

    [ShowIf("HasAnimatedPrefab")]
    public List<GameObject> ConstructedAnimatedVariantPrefabs;

    //TODO: Resources required to build
}

public enum BuildingStates
{
    Constructed = 0,
    PreConstruction,
    Destroyed
}