using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth player;

    [Header("UI Elements")]
    public Image fillImage;

    [Header("Opciones")]
    public float smoothSpeed = 8f;

    float targetFill = 1f;

    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerHealth>();

        if (player != null)
        {
            // Suscribirse a eventos del PlayerHealth
            player.OnDamaged += UpdateFromDamage;
            player.OnHealed += UpdateFromHeal;
            player.OnDeath += OnPlayerDeath;
        }
    }

    void Update()
    {
        // Animación suave
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
    }

    void UpdateFromDamage(float dmg)
    {
        targetFill = player.currentHealth / player.maxHealth;
    }

    void UpdateFromHeal(float heal)
    {
        targetFill = player.currentHealth / player.maxHealth;
    }

    void OnPlayerDeath()
    {
        targetFill = 0f;
    }
}
