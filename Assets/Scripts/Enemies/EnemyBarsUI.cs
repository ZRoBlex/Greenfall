using UnityEngine;
using UnityEngine.UI;

public class EnemyBarsUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;     // Para fade
    public Image healthFill;
    public Image captureFill;

    [Header("Config")]
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float fadeSpeed = 4f;

    Transform enemy;
    Camera cam;
    Health hp;
    NonLethalHealth nl;

    void Awake()
    {
        enemy = transform.parent;
        cam = Camera.main;

        hp = enemy.GetComponentInChildren<Health>();
        nl = enemy.GetComponentInChildren<NonLethalHealth>();

        canvasGroup.alpha = 0f; // Empieza invisible
    }

    void Update()
    {
        if (!enemy) return;

        // Mover con el enemigo
        transform.position = enemy.position + offset;

        // Billboard
        transform.LookAt(transform.position + cam.transform.forward);

        // ¿Debe mostrarse?
        bool show = false;

        if (hp != null && hp.currentHealth < hp.maxHealth) show = true;
        if (nl != null && nl.currentCapture > 0) show = true;

        // Fade in/out
        float targetAlpha = show ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // Actualizar barras
        if (hp != null)
            healthFill.fillAmount = hp.currentHealth / hp.maxHealth;

        if (nl != null)
            captureFill.fillAmount = nl.currentCapture / nl.maxCapture;
    }
}
