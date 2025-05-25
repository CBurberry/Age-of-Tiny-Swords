using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildAreaData", menuName = "TinyWorld/BuildAreaData")]
public class BuildAreaData : ScriptableObject
{
    public List<ConstructionData> Constructions;
}


[System.Serializable]
public class ConstructionData
{
    public SerializedDictionary<ResourceType, int> Cost;
    public BuildingData BuildingData;
    public SimpleBuilding Prefab;
}
