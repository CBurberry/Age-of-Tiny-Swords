/// <summary>
/// Interface shared by all building types
/// </summary>
public interface IBuilding : IDamageable
{
    BuildingStates State { get; }
    bool IsDamaged { get; }

    /// <summary>
    /// Initial placement of a building
    /// </summary>
    void Construct();

    /// <summary>
    /// Progressive construction of a building. Adds HP to building and when full, updates the visuals and state.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>Boolean indicating completion</returns>
    bool Build(int amount);

    /// <summary>
    /// Progressive repair of a building. Adds HP to building and when full, updates the visuals
    /// e.g. Removing any fire vfx
    /// </summary>
    /// <param name="amount"></param>
    void Repair(int amount);
}
