using UnityEngine;

[CreateAssetMenu(
    menuName = "Combat/Distance Damage Scaler",
    fileName = "DistanceDamageScaler"
)]
public class DistanceDamageScalerSO : ScriptableObject
{
    [Header("Distances (meters)")]
    [Tooltip("Distance where damage starts to fall off")]
    public float optimalDistance = 20f;

    [Tooltip("Distance where damage reaches minimum")]
    public float maxDistance = 150f;

    [Header("Damage Multipliers")]
    [Tooltip("Minimum damage multiplier at max distance")]
    [Range(0f, 1f)]
    public float minMultiplier = 0.3f;

    [Header("Curve")]
    [Tooltip("Controls how smooth or aggressive the falloff is")]
    public AnimationCurve falloffCurve =
        AnimationCurve.EaseInOut(0, 1, 1, 0);

    public float GetMultiplier(float distance)
    {
        // 🔒 Seguridad
        if (distance <= optimalDistance)
            return 1f;

        if (distance >= maxDistance)
            return minMultiplier;

        // Normalizamos entre 0 y 1
        float t = Mathf.InverseLerp(
            optimalDistance,
            maxDistance,
            distance
        );

        // Aplicamos curva
        float curveValue = falloffCurve.Evaluate(t);

        // Interpolamos daño
        return Mathf.Lerp(1f, minMultiplier, curveValue);
    }
}
