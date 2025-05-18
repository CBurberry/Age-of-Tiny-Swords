using UnityEngine;

/// <summary>
/// Common settings for all units that share this data.
/// </summary>
[CreateAssetMenu(fileName = "NewUnitData", menuName = "TinyWorld/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("References")]
    public GameObject UnitPrefab;

    [Header("Settings")]
    public int MaxHp = 100;
    public float MovementSpeed = 1f;
    public float AttackSpeed = 1f;
}
