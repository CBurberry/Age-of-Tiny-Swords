using UnityEngine;
using static Player;

/// <summary>
/// Common settings for all units that share this data.
/// </summary>
[CreateAssetMenu(fileName = "NewUnitData", menuName = "TinyWorld/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("Settings")]
    public Faction Faction;
    public int MaxHp = 100;
    public float MovementSpeed = 1f;
    public float AttackSpeed = 1f;          //Calculated as 1 / attackspeed per tick
    public float AttackDistance = 2f;       //Comapred against vector magnitude to target closest point
    public int BaseAttackDamage = 5;
    public float FOV;
}
