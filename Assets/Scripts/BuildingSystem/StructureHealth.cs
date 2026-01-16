using UnityEngine;

public class StructureHealth : MonoBehaviour
{
    public StructureData data;
    public int CurrentHealth { get; private set; }

    void OnEnable()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        CurrentHealth = data.maxHealth;
    }

    public void ApplyDamage(int amount)
    {
        if (!data.destructible) return;

        float resistance = data.damageResistance;
        int finalDamage = Mathf.RoundToInt(amount * (1f - resistance));

        CurrentHealth -= finalDamage;

        // 🎯 FEEDBACK VISUAL
        DamageIndicatorSpawner.Spawn(
            finalDamage,
            HitPointRelay.LastHitPoint,
            HitPointRelay.LastHitNormal
        );

        if (CurrentHealth <= 0)
            DestroyStructure();
    }

    void DestroyStructure()
    {
        // DEVOLVER AL POOL
        StructurePool.Instance.Release(gameObject);
    }
}
