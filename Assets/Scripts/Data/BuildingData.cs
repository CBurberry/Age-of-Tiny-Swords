using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Common settings for all buildings that share this data.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "TinyWorld/BuildingData")]
public class BuildingData : ScriptableObject
{
    public const string PreviewColorString = "#999999";

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