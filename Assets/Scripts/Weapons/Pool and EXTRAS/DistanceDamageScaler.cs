using UnityEngine;

[System.Serializable]
public class DistanceDamageScaler
{
    public float minDistance = 5f;
    public float maxDistance = 200f;

    public float closeMultiplier = 1.3f;
    public float farMultiplier = 0.5f;

    public float GetMultiplier(float distance)
    {
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        return Mathf.Lerp(closeMultiplier, farMultiplier, t);
    }
}
