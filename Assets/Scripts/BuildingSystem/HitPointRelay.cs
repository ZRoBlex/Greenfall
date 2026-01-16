using UnityEngine;

public class HitPointRelay : MonoBehaviour
{
    public static Vector3 LastHitPoint;
    public static Vector3 LastHitNormal;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount > 0)
        {
            LastHitPoint = collision.contacts[0].point;
            LastHitNormal = collision.contacts[0].normal;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        LastHitPoint = other.ClosestPoint(transform.position);
        LastHitNormal = (transform.position - LastHitPoint).normalized;
    }
}
