using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "TinyWorld")]
public class UnitData : ScriptableObject
{
    [Header("References")]
    public GameObject Prefab;
    public List<AnimationClip> Animations;

    [Header("Settings")]
    public bool IsSelectable;
    public bool IsMultiSelectable;
    public bool CanPlayerControl;
}
