using UnityEngine;
using System;

/// <summary>
/// Sistema de salud LETAL - Ultra optimizado
/// </summary>
public class Health : MonoBehaviour
{
    [Header("═══════ CONFIGURACIÓN ═══════")]
    public float maxHealth = 100f;

    [Header("═══════ ESTADO ═══════")]
    public float currentHealth;

    // ═══════ EVENTOS ═══════
    public event Action OnDeath;
    public event Action<float> OnDamageTaken;

    // ═══════ FLAGS ═══════
    bool isDead;

    // ═══════ CACHE ═══════
    EnemyController enemyController;
    EnemyLootDrop lootDrop;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;

        // Cache componentes
        enemyController = GetComponent<EnemyController>();
        lootDrop = GetComponent<EnemyLootDrop>();
    }

    void OnEnable()
    {
        // Reset cuando vuelve del pool
        currentHealth = maxHealth;
        isDead = false;
    }

    // ═══════ API PÚBLICA ═══════

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || isDead) return;

        // 🔥 No procesar daño en Sleep LOD
        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        currentHealth -= amount;
        OnDamageTaken?.Invoke(amount);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;

        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public bool IsDead() => isDead;

    public float GetHealthPercent() => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    // ═══════ MUERTE ═══════

    void Die()
    {
        if (isDead) return;

        isDead = true;

        // No procesar muerte en Sleep
        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        // Evento de muerte
        OnDeath?.Invoke();

        // Notificar al spawn manager
        EnemySpawnManager sm = FindFirstObjectByType<EnemySpawnManager>();
        if (sm != null && enemyController != null)
            sm.NotifyEnemyDied(enemyController);

        // Drop de loot
        if (lootDrop != null)
            lootDrop.DropLoot();

        // Devolver al pool
        gameObject.SetActive(false);
    }
}