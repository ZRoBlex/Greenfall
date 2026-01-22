using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action OnDeath;
    public event Action<float> OnDamageTaken;

    EnemyController ec;

    void Awake()
    {
        currentHealth = maxHealth;
        ec = GetComponent<EnemyController>();
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;

        // 🔴 Si está en Sleep LOD → ignorar completamente el daño
        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        currentHealth -= amount;

        // 🔒 Solo notificar si no está en Sleep
        OnDamageTaken?.Invoke(amount);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        OnDeath?.Invoke();

        var sm = FindFirstObjectByType<EnemySpawnManager>();
        if (sm != null && ec != null)
            sm.NotifyEnemyDied(ec);

        gameObject.SetActive(false);
    }



    public void ResetHealth()
    {
        currentHealth = maxHealth;

        // Opcional: podrías notificar algo si luego haces UI o efectos
        // OnDamageTaken?.Invoke(0f);
    }

}
