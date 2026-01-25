using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(StructureHealth))]
public class StructureHealthAdapter : MonoBehaviour
{
    Health health;
    StructureHealth structureHealth;

    void Awake()
    {
        health = GetComponent<Health>();
        structureHealth = GetComponent<StructureHealth>();

        // ESCUCHAR DAÑO DEL SISTEMA EXISTENTE
        health.OnDamageTaken += OnDamageTaken;
        health.OnDeath += OnDeath;
    }

    void OnDamageTaken(float amount)
    {
        structureHealth.ApplyDamage(Mathf.RoundToInt(amount));
    }

    void OnDeath()
    {
        // Evitar que Health desactive el objeto permanentemente
        StructurePool.Instance.Release(gameObject);
    }

    void OnDestroy()
    {
        health.OnDamageTaken -= OnDamageTaken;
        health.OnDeath -= OnDeath;
    }
}
