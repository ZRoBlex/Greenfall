using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    public Transform[] points;

    void OnDrawGizmos()
    {
        if (points == null || points.Length == 0)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            Gizmos.DrawSphere(points[i].position, 0.2f);

            int next = (i + 1) % points.Length;

            if (points[next] != null)
                Gizmos.DrawLine(points[i].position, points[next].position);
        }
    }
}
