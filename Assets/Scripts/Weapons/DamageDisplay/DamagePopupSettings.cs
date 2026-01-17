using UnityEngine;

[CreateAssetMenu(menuName = "Damage/Damage Popup Settings")]
public class DamagePopupSettings : ScriptableObject
{
    [Header("General")]
    public bool enabled = true;
    public bool usePooling = true;
    public bool lookAtCamera = true;

    [Header("Display")]
    public bool usePercentage = true;
    public bool allowCritical = true;
    public float criticalChance = 0.15f;
    public float criticalMultiplier = 1.5f;

    [Header("Accumulation")]
    public bool accumulateDamage = true;
    public float accumulateWindow = 0.25f;

    [Header("Visual")]
    public Color healthColor = Color.red;
    public Color captureColor = Color.yellow;
    public Color healColor = Color.green;
    public Color critColor = new Color(1f, 0.4f, 0f);

    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);

    [Header("Critical Animation")]
    public float critStartScale = 1.6f;
    public float critPopScale = 2.1f;
    public float critPopSpeed = 10f;
    public float critShakeStrength = 0.08f;
    public float critShakeDuration = 0.25f;

}
