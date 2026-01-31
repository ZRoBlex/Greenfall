using UnityEngine;

public class ZombieFogInfluence : MonoBehaviour
{
    public float influenceRadius = 12f;
    public float densityBoost = 0.01f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, influenceRadius);
    }
}
