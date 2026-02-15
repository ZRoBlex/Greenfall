using UnityEngine;
using System;

/// <summary>
/// Sistema de salud del jugador ULTRA optimizado
/// - Eventos para UI sin acoplamiento
/// - Sistema de daño modular
/// - Regeneración configurable
/// - Invencibilidad temporal
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("═══════ HEALTH ═══════")]
    [Range(1f, 1000f)]
    [SerializeField] float maxHealth = 100f;

    [HideInInspector]
    public float currentHealth;

    [HideInInspector]
    public bool isDead = false;

    [Header("═══════ REGENERACIÓN ═══════")]
    [SerializeField] bool enableRegen = false;

    [Range(0f, 50f)]
    [SerializeField] float regenRate = 5f;

    [Range(0f, 10f)]
    [SerializeField] float regenDelay = 3f;

    float regenTimer;

    [Header("═══════ INVENCIBILIDAD ═══════")]
    [Range(0f, 3f)]
    [SerializeField] float invincibleTime = 0.5f;

    float invincibleTimer;

    [Header("═══════ UI ═══════")]
    [SerializeField] UIResource healthUI;

    // ═══════ EVENTOS ═══════

    /// <summary>Envía el daño recibido</summary>
    public event Action<float, Vector3> OnDamaged;

    /// <summary>Envía la cantidad curada</summary>
    public event Action<float> OnHealed;

    /// <summary>Notifica muerte</summary>
    public event Action OnDeath;

    /// <summary>Notifica respawn</summary>
    public event Action OnRespawn;

    /// <summary>Cambio de health (para UI reactiva)</summary>
    public event Action<float, float> OnHealthChanged; // (current, max)

    // ═══════ CACHE ═══════

    CharacterController characterController;
    FirstPersonController playerController;

    // ═══════ PROPIEDADES ═══════

    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? (currentHealth / maxHealth) * 100f : 0f;
    public bool IsInvincible => invincibleTimer > 0f;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<FirstPersonController>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    void Update()
    {
        UpdateTimers(Time.deltaTime);
        UpdateRegeneration(Time.deltaTime);
    }

    void UpdateTimers(float deltaTime)
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= deltaTime;

        if (regenTimer > 0f)
            regenTimer -= deltaTime;
    }

    void UpdateRegeneration(float deltaTime)
    {
        if (!enableRegen || isDead || currentHealth >= maxHealth)
            return;

        if (regenTimer <= 0f)
        {
            currentHealth += regenRate * deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            UpdateUI();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    // ═══════ DAÑO ═══════

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (IsInvincible || isDead)
            return;

        // Activar invencibilidad
        invincibleTimer = invincibleTime;

        // Reducir vida
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Reiniciar regeneración
        regenTimer = regenDelay;

        // Eventos
        OnDamaged?.Invoke(amount, hitPoint);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // UI
        UpdateUI();

        // Muerte
        if (currentHealth <= 0f)
            Die();
    }

    // ═══════ CURACIÓN ═══════

    public void Heal(float amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        UpdateUI();
    }

    public void HealToFull()
    {
        Heal(maxHealth - currentHealth);
    }

    // ═══════ MUERTE ═══════

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0f;

        Debug.Log("💀 El jugador ha muerto");

        // Desactivar controles
        if (characterController != null)
            characterController.enabled = false;

        if (playerController != null)
            playerController.enabled = false;

        // Evento
        OnDeath?.Invoke();

        UpdateUI();

        // Auto-respawn después de 3 segundos (opcional)
        // Invoke(nameof(Respawn), 3f);
    }

    // ═══════ RESPAWN ═══════

    public void Respawn()
    {
        Respawn(transform.position);
    }

    public void Respawn(Vector3 position)
    {
        isDead = false;
        currentHealth = maxHealth;
        invincibleTimer = 0f;
        regenTimer = 0f;

        transform.position = position;

        // Reactivar controles
        if (characterController != null)
            characterController.enabled = true;

        if (playerController != null)
            playerController.enabled = true;

        OnRespawn?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        UpdateUI();

        Debug.Log("✨ Jugador respawneado");
    }

    // ═══════ UI ═══════

    void UpdateUI()
    {
        if (healthUI != null)
            healthUI.SetAmount(currentHealth, maxHealth);
    }

    // ═══════ MODIFICADORES ═══════

    public void SetMaxHealth(float newMax)
    {
        float percent = HealthPercent;
        maxHealth = Mathf.Max(1f, newMax);
        currentHealth = (maxHealth * percent) / 100f;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateUI();
    }

    public void AddMaxHealth(float amount)
    {
        SetMaxHealth(maxHealth + amount);
    }

    // ═══════ QUERIES ═══════

    public bool IsAlive() => !isDead;

    public bool IsFullHealth() => currentHealth >= maxHealth;

    public bool IsCritical() => HealthPercent <= 25f;

    public float GetMissingHealth() => maxHealth - currentHealth;
}