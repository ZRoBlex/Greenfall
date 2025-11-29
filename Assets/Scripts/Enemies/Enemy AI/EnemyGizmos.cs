using UnityEngine;

[ExecuteAlways]
public class EnemyGizmos : MonoBehaviour
{
    EnemyController enemy;

    void OnDrawGizmos()
    {
        if (enemy == null) enemy = GetComponent<EnemyController>();
        if (enemy == null || enemy.stats == null) return;

        // ====== PERCEPTION RANGE ======
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemy.stats.perceptionRange);

        // ====== FOV LINES ======
        Gizmos.color = new Color(1, 0, 0, 0.5f);

        Vector3 left = DirFromAngle(-enemy.stats.fieldOfView / 2);
        Vector3 right = DirFromAngle(enemy.stats.fieldOfView / 2);

        Gizmos.DrawLine(transform.position, transform.position + left * enemy.stats.perceptionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * enemy.stats.perceptionRange);

        // ====== CURRENT TARGET POINT (wander/patrol) ======
        if (enemy.debugTarget != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(enemy.debugTarget, 0.3f);
            Gizmos.DrawLine(transform.position, enemy.debugTarget);
        }

        // ====== TEXT ABOVE ENEMY ======
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, enemy.debugStateName);
#endif
    }

    Vector3 DirFromAngle(float angle)
    {
        angle += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }
}
