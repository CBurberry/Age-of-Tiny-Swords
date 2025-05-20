using NaughtyAttributes;
using UnityEngine;

public abstract class ABaseUnitInteractable : MonoBehaviour, IUnitInteractable
{
    [SerializeField]
    [Tooltip("If a unit interacts with this what can it do?")]
    [EnumFlags]
    private UnitInteractContexts interactableContexts;

    public UnitInteractContexts GetContexts() => interactableContexts;

    public abstract UnitInteractContexts GetApplicableContexts(SimpleUnit unit);

    public virtual bool CanInteract(SimpleUnit unit, UnitInteractContexts contexts, out UnitInteractContexts interactableContexts)
    {
        interactableContexts = contexts & GetApplicableContexts(unit);
        return interactableContexts != UnitInteractContexts.None;
    }
}
