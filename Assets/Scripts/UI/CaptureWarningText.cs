using UnityEngine;
using TMPro;

public class CaptureWarningText : MonoBehaviour
{
    [Header("References")]
    public NonLethalHealth targetNonLethal;
    public TMP_Text text;

    [Header("Appearance")]
    public Color textColor = Color.red;
    public float fadeSpeed = 4f;

    [Header("Pulse")]
    public float pulseScale = 1.25f;
    public float pulseSpeed = 3f;

    [Header("Glow (TMP Only)")]
    public bool enableGlow = true;
    public float glowIntensity = 1.2f;

    float baseScale;
    float baseGlow;
    float currentAlpha = 0f;

    void Start()
    {
        if (text == null) text = GetComponent<TextMeshPro>();

        baseScale = transform.localScale.x;

        if (targetNonLethal != null && text != null)
        {
            text.alpha = 0f;
            text.color = textColor;

            if (enableGlow && text.fontMaterial.HasProperty("_GlowPower"))
            {
                baseGlow = text.fontMaterial.GetFloat("_GlowPower");
                text.fontMaterial.SetFloat("_GlowPower", 0f);   // glow apagado al inicio
            }
        }
    }

    void Update()
    {
        if (targetNonLethal == null || text == null) return;

        bool full = targetNonLethal.currentCapture >= targetNonLethal.maxCapture;

        float targetAlpha = full ? 1f : 0f;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        text.alpha = currentAlpha;

        // Pulse suave cuando está lleno
        if (full)
        {
            float scale = baseScale * Mathf.Lerp(1f, pulseScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = new Vector3(scale, scale, scale);

            // Glow animado
            if (enableGlow && text.fontMaterial.HasProperty("_GlowPower"))
            {
                float glow = Mathf.Lerp(0f, glowIntensity, currentAlpha);
                text.fontMaterial.SetFloat("_GlowPower", glow);
            }
        }
        else
        {
            // Volver a tamaño normal
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * baseScale, Time.deltaTime * 6f);

            // Glow apagándose
            if (enableGlow && text.fontMaterial.HasProperty("_GlowPower"))
            {
                float glow = Mathf.Lerp(text.fontMaterial.GetFloat("_GlowPower"), 0f, Time.deltaTime * fadeSpeed);
                text.fontMaterial.SetFloat("_GlowPower", glow);
            }
        }

        // Billboard (mira a la cámara)
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }
}
