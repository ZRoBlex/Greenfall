using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Perception : MonoBehaviour
{
    public EnemyStats stats;
    public LayerMask targetMask;     // layer(s) donde está el player
    public LayerMask obstacleMask;   // layers que bloquean la visión

    /// <summary>
    /// Devuelve true si detectó un objetivo.
    /// PRIORIDAD: primero detección cercana (close radius) → luego FOV+raycast.
    /// </summary>
    public bool HasDetectedTarget(out Transform target)
    {
        target = null;

        if (stats == null)
        {
            Debug.LogWarning($"{name} Perception: stats no asignado");
            return false;
        }

        // 1) Detección cercana (auto-detect), SIN comprobar obstáculos.
        //    Esto permite que el enemigo "note" al jugador si entra muy cerca, aun sin verlo.
        Collider[] closeHits = Physics.OverlapSphere(transform.position, stats.closeDetectionRadius, targetMask);
        if (closeHits.Length > 0)
        {
            target = closeHits[0].transform;
            return true;
        }

        // 2) Detección por FOV + línea de visión
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.perceptionRange, targetMask);
        foreach (var h in hits)
        {
            Vector3 dir = h.transform.position - transform.position;
            float dist = dir.magnitude;

            // Ángulo entre forward y la dirección al objetivo
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > stats.fieldOfView * 0.5f) continue;

            // Raycast para comprobar si hay obstáculo entre ambos
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
                // Si el rayo impactó con el propio target (por ejemplo collider fino)
                target = h.transform;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Chequeo directo: si el target está dentro del radio pequeño.
    /// Útil para pruebas independientes o llamadas directas.
    /// </summary>
    public bool IsTargetClose(Transform target)
    {
        if (target == null || stats == null) return false;
        float dist = Vector3.Distance(transform.position, target.position);
        return dist <= stats.closeDetectionRadius;
    }

    /// <summary>
    /// Comprueba si el target es visible por FOV + raycast.
    /// NO considera la detección por proximidad (IsTargetClose) — esa es otra comprobación.
    /// </summary>
    public bool CanSeeTarget(Transform target)
    {
        if (target == null || stats == null) return false;

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

    // -----------------------
    // GIZMOS — visual debugging
    // -----------------------
    void OnDrawGizmos()
    {
        if (stats == null) return;

        // Rango general
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange);

        // Círculo de detección cercana
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stats.closeDetectionRadius);

        // Cono de visión
        Gizmos.color = new Color(1, 0, 0, 0.25f);

        Vector3 left = DirectionFromAngle(-stats.fieldOfView / 2f);
        Vector3 right = DirectionFromAngle(stats.fieldOfView / 2f);

        Gizmos.DrawLine(transform.position, transform.position + left * stats.perceptionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * stats.perceptionRange);
    }

    Vector3 DirectionFromAngle(float angleDegrees)
    {
        float rad = (angleDegrees + transform.eulerAngles.y) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}
