using NaughtyAttributes;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class governing behaviours that all buildings share e.g. Construction, Destruction.
/// </summary>
public class SimpleBuilding : MonoBehaviour
{
    public BuildingStates State => state;
    public bool IsDamaged => state != BuildingStates.Destroyed && currentHp < maxHp;

    [SerializeField]
    [Expandable]
    protected BuildingData data;

    protected float hpPercent => (float)currentHp / maxHp;
    protected int maxHp => data.MaxHp;
    [ShowNonSerializedField]
    protected int currentHp;

    protected GameObject visuals;
    protected GameObject animatedPrefabInstance;
    protected SpriteRenderer spriteRenderer;
    protected BuildingStates state;

    private const float damageVisualThreshold = 0.75f;

    private void Start()
    {
        visuals = new GameObject("Visuals", typeof(SpriteRenderer));
        visuals.transform.localPosition = Vector3.zero;
        visuals.transform.localRotation = Quaternion.identity;
        visuals.transform.localScale = Vector3.one;
        visuals.transform.parent = transform;
        spriteRenderer = visuals.GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initial placement of a building
    /// </summary>
    [Button("Construct (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void Construct()
    {
        if (currentHp > 0) 
        {
            return;
        }

        if (data.BuildingSpriteVisuals.Keys.Contains(BuildingStates.PreConstruction))
        {
            state = BuildingStates.PreConstruction;
            currentHp = 10;
            spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        }
        else 
        {
            //Some enemy buildings don't have a PreConstructed state so just skip to full build
            currentHp = maxHp;
            CompleteConstruction();
        }
    }

    /// <summary>
    /// Progressive construction of a building. Adds HP to building and when full, updates the visuals and state.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>Boolean indicating completion</returns>
    public virtual bool Build(int amount)
    {
        currentHp = Math.Clamp(currentHp + amount, currentHp, maxHp);
        if (currentHp == maxHp)
        {
            CompleteConstruction();
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Progressive repair of a building. Adds HP to building and when full, updates the visuals
    /// e.g. Removing any fire vfx
    /// </summary>
    /// <param name="amount"></param>
    public void Repair(int amount)
    {
        if (currentHp == 0) 
        {
            return;
        }

        currentHp = Math.Clamp(currentHp + amount, 0, maxHp);
        if (state == BuildingStates.Constructed && currentHp > damageVisualThreshold) 
        {
            RemoveDamagedVisual();
        }
    }

    public void TakeDamage(int amount)
    {
        //Note: maybe make this two tier of intensity when damaged?
        currentHp = Math.Clamp(currentHp - amount, 0, maxHp);

        if (currentHp == 0)
        {
            DestroyBuilding();
            return;
        }

        if (state == BuildingStates.Constructed && hpPercent < damageVisualThreshold) 
        {
            ApplyDamagedVisual();
        }
    }

    protected virtual void CompleteConstruction()
    {
        state = BuildingStates.Constructed;
        if (data.HasAnimatedPrefab)
        {
            spriteRenderer.enabled = false;
            animatedPrefabInstance = Instantiate(data.ConstructedAnimatedPrefab, visuals.transform);
        }
        else 
        {
            spriteRenderer.sprite = data.BuildingSpriteVisuals[state];
        }

        //TODO: Enable interactions
    }

    protected virtual void ApplyDamagedVisual()
    {
        //TODO: Apply VFX
    }

    protected virtual void RemoveDamagedVisual()
    {
        //TODO: Remove VFX
    }

    private void DestroyBuilding()
    {
        //TODO: Play vfx e.g. smoke
        //TODO: Remove any fire vfx playing (if any)

        //Replace visual with destroyed visual
        state = BuildingStates.Destroyed;

        if (animatedPrefabInstance != null) 
        {
            Destroy(animatedPrefabInstance);
            animatedPrefabInstance = null;
            spriteRenderer.enabled = true;
        }

        spriteRenderer.sprite = data.BuildingSpriteVisuals[state];

        //TODO: Disable interactions
    }

    [Button("Build 100% (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void Build()
    {
        if (currentHp > 0 && state == BuildingStates.PreConstruction) 
        {
            Build(maxHp);
        }
    }

    [Button("Damage 10% (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void ApplyDamage()
    {
        TakeDamage(maxHp / 10);
    }

    [Button("Repair 10% (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void Repair()
    {
        Repair(maxHp / 10);
    }

    [Button("Trigger Destruction (PlayMode)", EButtonEnableMode.Playmode)]
    protected virtual void TriggerDestroy()
    {
        TakeDamage(maxHp);
    }
}
