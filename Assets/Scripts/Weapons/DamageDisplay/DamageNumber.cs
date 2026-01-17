using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public TextMeshPro text;

    float timer;
    float lifetime = 1.2f;

    bool isCritical;

    Vector3 baseScale;
    Vector3 shakeOffset;

    DamagePopupSettings settings;

    float shakeTimer;

    public void Show(
        float value,
        DamageDisplayType type,
        bool critical,
        DamagePopupSettings popupSettings,
        Vector3 position)
    {
        settings = popupSettings;
        isCritical = critical;

        transform.position = position;
        timer = 0f;
        shakeTimer = 0f;
        shakeOffset = Vector3.zero;

        gameObject.SetActive(true);

        text.text = Mathf.RoundToInt(value).ToString();

        // 🎨 COLOR
        Color c = type switch
        {
            DamageDisplayType.Health => settings.healthColor,
            DamageDisplayType.Capture => settings.captureColor,
            DamageDisplayType.Heal => settings.healColor,
            _ => Color.white
        };

        if (critical)
            c = settings.critColor;

        text.color = c;

        // 🔍 ESCALA INICIAL
        baseScale = critical
            ? Vector3.one * settings.critStartScale
            : Vector3.one;

        transform.localScale = baseScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // ⬆ Movimiento vertical
        transform.position += Vector3.up * Time.deltaTime;

        // 🎥 Mirar cámara
        if (Camera.main)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - Camera.main.transform.position
            );
        }

        // ⭐ ANIMACIÓN DE CRÍTICO
        if (isCritical)
            AnimateCritical();

        // 🌫 Fade out
        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        Color c = text.color;
        c.a = alpha;
        text.color = c;

        // ⏱ Fin
        if (timer >= lifetime)
            DamageNumberPool.Instance.Release(this);
    }

    void AnimateCritical()
    {
        // 🔥 POP SCALE (crece y vuelve)
        float pop = Mathf.Sin(timer * settings.critPopSpeed);
        float scaleMultiplier = Mathf.Lerp(
            settings.critStartScale,
            settings.critPopScale,
            pop
        );

        transform.localScale = Vector3.one * scaleMultiplier;

        // 🎯 SHAKE
        if (shakeTimer < settings.critShakeDuration)
        {
            shakeTimer += Time.deltaTime;

            shakeOffset = Random.insideUnitSphere * settings.critShakeStrength;
            transform.position += shakeOffset;
        }
    }
}
