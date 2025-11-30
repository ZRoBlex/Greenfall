using UnityEngine;
using UnityEngine.UI;

public class EnemyBarsUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;

    // Vida (sin ghost)
    public Image healthFill;

    // Captura (barra real + ghost con FADE)
    public Image captureFill;
    public Image captureGhostFill;

    [Header("Smooth Settings")]
    public float barSmoothSpeed = 6f;
    public float ghostDelay = 0.35f;
    public float ghostFallSpeed = 2.5f;
    public float ghostFadeOutSpeed = 2f;

    [Header("Pulse Effect")]
    public float pulseScale = 1.15f;
    public float pulseSpeed = 4f;

    [Header("Shake Warning (85% Capture)")]
    public float shakeAmount = 6f;
    public float shakeSpeed = 25f;

    [Header("Fade")]
    public float fadeSpeed = 4f;

    Transform enemy;
    Camera cam;

    public Health hp;
    public NonLethalHealth nl;

    float smoothHealth = 1f;
    float smoothCapture = 0f;
    float ghostCapture;
    float ghostCaptureTimer;

    Vector3 originalScale;
    Vector3 originalPos;

    void Awake()
    {
        enemy = transform.parent;
        cam = Camera.main;

        canvasGroup.alpha = 0f;

        originalScale = transform.localScale;
        originalPos = transform.localPosition;

        ghostCapture = smoothCapture;
    }

    void Update()
    {
        if (!enemy) return;

        HandleFade();
        UpdateBars();
        UpdateGhostBarsCapture();
        PulseEffect();
        ShakeWarning();

        // Billboard (opcional): mirar a cámara (descomenta si quieres)
        // transform.LookAt(transform.position + cam.transform.forward);
    }

    // Fade in/out según necesidad
    void HandleFade()
    {
        bool show = false;

        if (hp != null && hp.currentHealth < hp.maxHealth) show = true;
        if (nl != null && nl.currentCapture > 0) show = true;

        float targetAlpha = show ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    // Suavizado básico para barras
    void UpdateBars()
    {
        // VIDA (sin color change)
        if (hp != null)
        {
            float targetHealth = hp.currentHealth / hp.maxHealth;
            smoothHealth = Mathf.Lerp(smoothHealth, targetHealth, Time.deltaTime * barSmoothSpeed);
            if (healthFill != null)
                healthFill.fillAmount = smoothHealth;
        }

        // CAPTURA
        if (nl != null)
        {
            float targetCapture = nl.currentCapture / nl.maxCapture;
            smoothCapture = Mathf.Lerp(smoothCapture, targetCapture, Time.deltaTime * barSmoothSpeed);
            if (captureFill != null)
                captureFill.fillAmount = smoothCapture;

            if (targetCapture < ghostCapture)
                ghostCaptureTimer = ghostDelay;
        }
    }

    // Ghost capture behavior (fade out when capture == 0)
    void UpdateGhostBarsCapture()
    {
        if (captureGhostFill == null) return;

        if (nl != null && nl.currentCapture <= 0.001f)
        {
            ghostCapture = Mathf.Lerp(ghostCapture, 0f, Time.deltaTime * ghostFadeOutSpeed);

            Color c = captureGhostFill.color;
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * ghostFadeOutSpeed);
            captureGhostFill.color = c;

            captureGhostFill.fillAmount = ghostCapture;
            return;
        }

        if (ghostCaptureTimer > 0f)
            ghostCaptureTimer -= Time.deltaTime;
        else
            ghostCapture = Mathf.Lerp(ghostCapture, smoothCapture, Time.deltaTime * ghostFallSpeed);

        captureGhostFill.fillAmount = ghostCapture;

        float alpha = Mathf.Lerp(captureGhostFill.color.a, ghostCapture, Time.deltaTime * 6f);
        Color cc = captureGhostFill.color;
        cc.a = alpha;
        captureGhostFill.color = cc;
    }

    // Pulso cuando capture está al 100%
    void PulseEffect()
    {
        if (nl == null) return;

        if (nl.currentCapture >= nl.maxCapture)
        {
            float scale = Mathf.Lerp(1f, pulseScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = originalScale * scale;
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 6f);
        }
    }

    // Shake warning cuando la captura está alta (85%+)
    void ShakeWarning()
    {
        if (nl == null) return;

        float ratio = nl.currentCapture / nl.maxCapture;

        if (ratio >= 0.85f && ratio < 1f)
        {
            float x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            transform.localPosition = originalPos + new Vector3(x, 0, 0);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, Time.deltaTime * 6f);
        }
    }
}
