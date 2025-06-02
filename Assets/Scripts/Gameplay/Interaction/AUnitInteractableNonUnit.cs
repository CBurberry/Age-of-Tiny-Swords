using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Abstract class to represent a Monobehaviour that is NOT a unit type.
/// N.B. Use AUnitInteractableUnit if you want something that a unit can interact with that is also a unit.
/// </summary>
public abstract class AUnitInteractableNonUnit : MonoBehaviour, IUnitInteractable
{
    [SerializeField]
    [Tooltip("If a unit interacts with this what can it do?")]
    [EnumFlags]
    private UnitInteractContexts interactableContexts;

    //Set this to true when calling Destroy
    private bool destroyCalled;

    public bool DestructionPending => destroyCalled;
    public Vector3 Position => transform.position;

    public UnitInteractContexts GetContexts() => interactableContexts;

    public abstract UnitInteractContexts GetApplicableContexts(SimpleUnit unit);

    public virtual bool CanInteract(SimpleUnit unit, UnitInteractContexts contexts, out UnitInteractContexts interactableContexts)
    {
        interactableContexts = contexts & GetApplicableContexts(unit);
        return interactableContexts != UnitInteractContexts.None;
    }

    protected virtual void DestroySelf()
    {
        destroyCalled = true;
        Destroy(gameObject);
    }
}
