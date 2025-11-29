using UnityEngine;

public class RoutePath : MonoBehaviour
{
    public Transform[] points;

    public void OnDrawGizmos()
    {
        if (points == null || points.Length == 0)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawSphere(points[i].position, 0.2f);

            if (i < points.Length - 1)
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
}
