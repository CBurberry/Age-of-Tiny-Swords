using UnityEngine;

/// <summary>
/// Implementer of this interface defines interactions when a unit engages a target
/// (either automatically or by a player instruction)
/// </summary>
public interface IUnitInteractable
{
    bool DestructionPending { get; }
    Vector3 Position { get; }

    /// <summary>
    /// Get all possible contexts that this interactable supports, regardless of condition of this instance.
    /// </summary>
    /// <returns></returns>
    UnitInteractContexts GetContexts();

    /// <summary>
    /// Get the current interactable contexts that the state of this object instance supports.
    /// </summary>
    /// <param name="unit">Unit to interact (need to check factions)</param>
    /// <returns></returns>
    UnitInteractContexts GetApplicableContexts(SimpleUnit unit);

    /// <summary>
    /// Check whether the given context(s) can be applied to this interactable.
    /// </summary>
    /// <param name="unit">Unit to interact (need to check factions)</param>
    /// <param name="contexts">Contexts to check</param>
    /// <param name="interactableContexts">Contexts that apply</param>
    /// <returns></returns>
    bool CanInteract(SimpleUnit unit, UnitInteractContexts contexts, out UnitInteractContexts interactableContexts);
}

[System.Flags]
public enum UnitInteractContexts
{
    None        = 0,
    Attack      = 1 << 0,
    Build       = 1 << 1,
    Repair      = 1 << 2,
    Gather      = 1 << 3,
    Garrison    = 1 << 4
}
