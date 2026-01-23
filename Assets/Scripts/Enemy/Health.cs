using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action OnDeath;
    public event Action<float> OnDamageTaken;

    EnemyController ec;

    bool isDead = false; // 🔒 PROTECCIÓN CLAVE

    void Awake()
    {
        currentHealth = maxHealth;
        ec = GetComponent<EnemyController>();
        isDead = false;
    }

    void OnEnable()
    {
        // 🔁 Reset cuando el enemigo vuelve del pool
        currentHealth = maxHealth;
        isDead = false;
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        if (isDead) return;

        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        currentHealth -= amount;

        OnDamageTaken?.Invoke(amount);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        if (isDead) return;   // 🔒 BLINDAJE TOTAL
        isDead = true;

        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        // 🔔 Notificar muerte
        OnDeath?.Invoke();

        var sm = FindFirstObjectByType<EnemySpawnManager>();
        if (sm != null && ec != null)
            sm.NotifyEnemyDied(ec);

        // 🎁 DROP (SOLO AQUÍ)
        GetComponent<EnemyLootDrop>()?.DropLoot();

        // ♻️ Volver al pool
        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }
}
