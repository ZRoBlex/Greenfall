using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI de barra de vida optimizada
/// Compatible con el nuevo PlayerHealth
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("═══════ REFERENCIAS ═══════")]
    [SerializeField] PlayerHealth player;

    [Header("═══════ UI ═══════")]
    [SerializeField] Image fillImage;
    [SerializeField] Image damageFlashImage; // Opcional: flash rojo al recibir daño

    [Header("═══════ CONFIGURACIÓN ═══════")]
    [Range(1f, 20f)]
    [SerializeField] float smoothSpeed = 8f;

    [Range(0f, 2f)]
    [SerializeField] float flashDuration = 0.3f;

    // ═══════ ESTADO ═══════

    float targetFill = 1f;
    float currentFill = 1f;
    float flashTimer;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerHealth>();

        if (player == null)
        {
            Debug.LogError("❌ HealthBarUI: No se encontró PlayerHealth");
            enabled = false;
            return;
        }

        // Suscribirse a eventos
        player.OnHealthChanged += OnHealthChanged;
        player.OnDamaged += OnDamaged;
        player.OnDeath += OnDeath;
        player.OnRespawn += OnRespawn;

        // Inicializar
        UpdateFill(player.currentHealth, player.MaxHealth, true);
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= OnHealthChanged;
            player.OnDamaged -= OnDamaged;
            player.OnDeath -= OnDeath;
            player.OnRespawn -= OnRespawn;
        }
    }

    void Update()
    {
        // Animación suave
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);

        if (fillImage != null)
            fillImage.fillAmount = currentFill;

        // Flash de daño
        UpdateDamageFlash();
    }

    // ═══════ EVENTOS ═══════

    void OnHealthChanged(float current, float max)
    {
        UpdateFill(current, max, false);
    }

    void OnDamaged(float damage, Vector3 hitPoint)
    {
        // Activar flash rojo
        if (damageFlashImage != null)
            flashTimer = flashDuration;
    }

    void OnDeath()
    {
        targetFill = 0f;
    }

    void OnRespawn()
    {
        UpdateFill(player.currentHealth, player.MaxHealth, true);
    }

    // ═══════ UPDATES ═══════

    void UpdateFill(float current, float max, bool instant)
    {
        if (max <= 0f)
        {
            targetFill = 0f;
            if (instant)
                currentFill = 0f;
            return;
        }

        targetFill = current / max;

        if (instant)
            currentFill = targetFill;
    }

    void UpdateDamageFlash()
    {
        if (damageFlashImage == null)
            return;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;

            float alpha = Mathf.Clamp01(flashTimer / flashDuration);
            Color c = damageFlashImage.color;
            c.a = alpha;
            damageFlashImage.color = c;
        }
    }
}