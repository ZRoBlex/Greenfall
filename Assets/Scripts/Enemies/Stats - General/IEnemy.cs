using UnityEngine;

public interface IEnemy
{
    Transform Transform { get; }
    EnemyStats Stats { get; }
    Health Health { get; }
    void TakeDamage(float amount, GameObject source = null);
    void ApplyStun(float seconds);
    bool IsKnockedOut { get; }
}
