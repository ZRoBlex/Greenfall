using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Perception : MonoBehaviour
{
    public EnemyStats stats;
    public LayerMask targetMask;     // layer(s) donde está el player
    public LayerMask obstacleMask;   // layers que bloquean la visión

    SphereCollider closeTrigger;

    void Awake()
    {
        closeTrigger = GetComponent<SphereCollider>();
        closeTrigger.isTrigger = true;
        closeTrigger.radius = (stats != null) ? stats.closeDetectionRadius : 1f;
    }

    void Update()
    {
        // mantener radio sincronizado si se modifica en runtime
        if (closeTrigger != null && stats != null)
            closeTrigger.radius = stats.closeDetectionRadius;
    }

    // -------------------------------------------------------------------
    // SOLO IMPRIME EN CONSOLA CUANDO EL PLAYER ENTRE AL RADIO CERCANO
    // -------------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetMask.value) != 0)
        {
            Debug.Log("Cerca del enemigo");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetMask.value) == 0)
            return;

        // Obtener referencia al enemigo
        EnemyController owner = GetComponent<EnemyController>();
        if (owner == null) return;

        // Referencia al jugador
        Transform player = other.transform;
        if (player == null) return;

        // Rotar hacia el jugador continuamente
        owner.movement.RotateTowards(player.position);
    }


    // -------------------------------------------------------------------
    // DETECCIÓN SOLO POR VISIÓN (FOV + RAYCAST)
    // -------------------------------------------------------------------
    public bool HasDetectedTarget(out Transform target)
    {
        target = null;

        if (stats == null)
        {
            Debug.LogWarning($"{name} Perception: stats no asignado");
            return false;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, stats.perceptionRange, targetMask);

        foreach (var h in hits)
        {
            Vector3 dir = h.transform.position - transform.position;
            float dist = dir.magnitude;

            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > stats.fieldOfView * 0.5f)
                continue;

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

    // -------------------------------------------------------------------
    // Chequeo directo si el target está visible por FOV + raycast
    // -------------------------------------------------------------------
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
            if (hit.transform != target) return false;
        }

        return true;
    }

    // -------------------------------------------------------------------
    // Método utilitario: comprobar distancia cercana (sin causar efectos)
    // -------------------------------------------------------------------
    public bool IsPlayerWithinCloseRange(Transform target)
    {
        if (target == null || stats == null) return false;
        float dist = Vector3.Distance(transform.position, target.position);
        return dist <= stats.closeDetectionRadius;
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

        // Círculo de detección cercana (visual)
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
