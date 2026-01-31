using UnityEngine;

public class TimeOfDayManager : MonoBehaviour
{
    [Header("Time Settings")]
    [Range(0f, 24f)]
    public float timeOfDay = 12f;

    public float dayDurationInMinutes = 30f; // día completo
    public bool autoAdvance = true;

    [Header("Sun")]
    public Light sun;

    [Header("Lighting Preset")]
    public LightingPreset preset;

    float timeSpeed;

    void Start()
    {
        timeSpeed = 24f / (dayDurationInMinutes * 60f);
    }

    void Update()
    {
        if (!sun || !preset) return;

        if (autoAdvance)
        {
            timeOfDay += Time.deltaTime * timeSpeed;
            if (timeOfDay >= 24f)
                timeOfDay = 0f;
        }

        UpdateSun();
        UpdateLighting();
    }

    void UpdateSun()
    {
        float sunRotation = (timeOfDay / 24f) * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunRotation, 170f, 0);
    }

    void UpdateLighting()
    {
        float t = timeOfDay / 24f;

        sun.color = preset.sunColor.Evaluate(t);
        sun.intensity = preset.sunIntensity.Evaluate(t);

        RenderSettings.ambientLight =
            preset.ambientColor.Evaluate(t);
    }

    public bool IsNight()
    {
        return timeOfDay < 6f || timeOfDay > 18f;
    }
}
