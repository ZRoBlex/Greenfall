using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Perception : MonoBehaviour
{
    public EnemyStats stats;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    // -----------------------------------------
    // DETECCIÓN NORMAL (FOV + visión)
    // -----------------------------------------
    public bool HasDetectedTarget(out Transform target)
    {
        target = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, stats.perceptionRange, targetMask);

        foreach (var h in hits)
        {
            Vector3 dir = h.transform.position - transform.position;
            float dist = dir.magnitude;

            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > stats.fieldOfView * 0.5f) continue;

            if (!Physics.Raycast(transform.position + Vector3.up * 1.2f,
                                 dir.normalized,
                                 out RaycastHit hit,
                                 dist,
                                 obstacleMask))
            {
                target = h.transform;
                return true;
            }
            else if (hit.transform == h.transform)
            {
                target = h.transform;
                return true;
            }
        }

        return false;
    }

    // -----------------------------------------
    // ¿PUEDE VER AL TARGET?
    // -----------------------------------------
    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        if (dist > stats.perceptionRange) return false;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > stats.fieldOfView * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up * 1.2f,
                            dir.normalized,
                            out RaycastHit hit,
                            dist,
                            obstacleMask))
        {
            if (hit.transform != target)
                return false;
        }

        return true;
    }

    // -----------------------------------------
    // DETECCIÓN POR PROXIMIDAD SIN VISIÓN
    // -----------------------------------------
    public bool IsTargetClose(Transform target)
    {
        if (target == null) return false;

        float dist = Vector3.Distance(transform.position, target.position);
        return dist <= stats.closeDetectionRadius;
    }

    // -----------------------------------------
    // GIZMOS (visión + cercanía)
    // -----------------------------------------
    void OnDrawGizmos()
    {
        if (stats == null) return;

        // rango visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange);

        // cono FOV
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Vector3 left = DirectionFromAngle(-stats.fieldOfView / 2);
        Vector3 right = DirectionFromAngle(stats.fieldOfView / 2);

        Gizmos.DrawLine(transform.position, transform.position + left * stats.perceptionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * stats.perceptionRange);

        // círculo pequeño: detección por proximidad
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stats.closeDetectionRadius);
    }

    Vector3 DirectionFromAngle(float angleDegrees)
    {
        float rad = (angleDegrees + transform.eulerAngles.y) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
    }
}
