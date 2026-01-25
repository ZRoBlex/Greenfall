using UnityEngine;

public class OccupancyVolume : MonoBehaviour
{
    public Bounds GetWorldBounds()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);

        foreach (var c in GetComponentsInChildren<BoxCollider>())
        {
            if (!c.isTrigger) continue;
            b.Encapsulate(c.bounds);
        }

        return b;
    }
}
