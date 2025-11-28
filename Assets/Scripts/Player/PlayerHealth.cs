// PlayerHealth.cs
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;            // Vida máxima
    public float currentHealth;               // Vida actual
    public bool isDead = false;               // Flag para evitar duplicar muerte

    [Header("Regeneración")]
    public bool enableRegen = false;          // Activar regeneración
    public float regenRate = 5f;              // Vida por segundo
    public float regenDelay = 3f;             // Tiempo antes de empezar regeneración
    private float regenTimer = 0f;

    [Header("Invencibilidad temporal")]
    public float invincibleTime = 0.5f;       // frames de invencibilidad
    private float invincibleTimer = 0f;

    // --- Eventos ---
    // Otros sistemas (UI, sonido, etc.) pueden suscribirse sin acoplamiento
    public event Action<float> OnDamaged;     // Envía el daño recibido
    public event Action<float> OnHealed;      // Envía la cantidad curada
    public event Action OnDeath;              // Notifica muerte

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        // --- Temporizador de invencibilidad ---
        if (invincibleTimer > 0)
            invincibleTimer -= Time.deltaTime;

        // --- Regeneración ---
        if (enableRegen && !isDead)
        {
            if (regenTimer > 0)
            {
                regenTimer -= Time.deltaTime; // Esperamos a iniciar regeneración
            }
            else
            {
                RegenerateHealth();
            }
        }
    }

    // ============================================
    // 👇 DAÑO
    // ============================================
    public void TakeDamage(float amount)
    {
        // --- Evitar daño si estás en frames de invencibilidad ---
        if (invincibleTimer > 0f || isDead)
            return;

        // Activar invencibilidad temporal
        invincibleTimer = invincibleTime;

        // Reducir vida
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Notificar UI o efectos
        OnDamaged?.Invoke(amount);

        // Reiniciar regeneración
        regenTimer = regenDelay;

        // Si llega a cero → muerte
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // ============================================
    // 👇 CURACIÓN
    // ============================================
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealed?.Invoke(amount);
    }

    // ============================================
    // 👇 MUERTE
    // ============================================
    void Die()
    {
        isDead = true;
        currentHealth = 0;

        Debug.Log("El jugador ha muerto.");
        OnDeath?.Invoke();

        // Aquí puedes:
        // - Reproducir animación de muerte
        // - Desactivar movimiento
        // - Abrir menú GameOver
        // - Respawnear después de X segundos
        //
        // Ejemplo: desactivar CharacterController
        var controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
    }

    // ============================================
    // 👇 REGENERACIÓN
    // ============================================
    void RegenerateHealth()
    {
        if (currentHealth >= maxHealth) return;

        // Sumar vida por segundo:
        // matemática: currentHealth += regenRate * Time.deltaTime
        // Time.deltaTime es el tiempo entre frames → regeneración suave y estable
        currentHealth += regenRate * Time.deltaTime;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
}
