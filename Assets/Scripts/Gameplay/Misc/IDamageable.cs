using UnityEngine;

/// <summary>
/// All entities that can be attacked, have a HP bar etc should extend this.
/// </summary>
public interface IDamageable
{
    float HpAlpha { get; }
    Vector3 GetClosestPosition(Vector3 position);
    void ApplyDamage(int value);
}
