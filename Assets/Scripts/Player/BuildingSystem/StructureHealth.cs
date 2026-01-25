using UnityEngine;

public class StructureHealth : MonoBehaviour
{
    public StructureData data;
    public int CurrentHealth { get; private set; }

    DamageHitRelay relay;

    void Awake()
    {
        relay = GetComponent<DamageHitRelay>();
    }

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

        int finalDamage = Mathf.RoundToInt(
            amount * (1f - data.damageResistance)
        );

        CurrentHealth -= finalDamage;

        if (relay != null)
        {
            DamageIndicatorSpawner.Spawn(
                finalDamage,
                relay.LastHitPoint,
                Vector3.up
            );
        }

        if (CurrentHealth <= 0)
            DestroyStructure();
    }

    void DestroyStructure()
    {
        StructurePool.Instance.Release(gameObject);
    }
}
