using UnityEngine;

public class DamageHitRelay : MonoBehaviour
{
    public Vector3 LastHitPoint { get; private set; }

    public void RegisterHit(Vector3 hitPoint)
    {
        LastHitPoint = hitPoint;
    }
}
