using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sistema de movimiento con NavMesh - ULTRA OPTIMIZADO
/// Navegación fluida, esquiva obstáculos automáticamente
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavMeshMovement : MonoBehaviour, IEnemyMovement
{
    [Header("═══════ CONFIGURACIÓN ═══════")]
    public bool rotateTowardsMovement = true;

    [Header("═══════ DEBUG ═══════")]
    public bool showDebugPath = false;

    // ═══════ COMPONENTES ═══════

    NavMeshAgent agent;
    EnemyStats stats;

    // ═══════ SEGUIMIENTO ═══════

    Transform followTarget;
    float lastSetTargetTime;
    const float UPDATE_TARGET_INTERVAL = 0.3f;

    // ═══════ CACHE ═══════

    Transform cachedTransform;
    Vector3 lastDestination;

    // ═══════ PROPIEDADES ═══════

    public bool RotateTowardsMovement
    {
        get => rotateTowardsMovement;
        set
        {
            rotateTowardsMovement = value;
            if (agent != null)
                agent.updateRotation = value;
        }
    }

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        cachedTransform = transform;
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError($"[EnemyNavMeshMovement] {name} necesita NavMeshAgent!");
            return;
        }

        // Configuración inicial
        agent.updateRotation = rotateTowardsMovement;
        agent.updateUpAxis = false; // Para mayor control
    }

    void Update()
    {
        // Actualizar target dinámico
        if (followTarget != null && Time.time - lastSetTargetTime > UPDATE_TARGET_INTERVAL)
        {
            SetTarget(followTarget);
        }
    }

    // ═══════ INTERFAZ IEnemyMovement ═══════

    public void Initialize(EnemyStats enemyStats)
    {
        stats = enemyStats;

        if (agent != null && stats != null)
        {
            agent.speed = stats.moveSpeed;
            agent.acceleration = stats.moveSpeed * 4f;
            agent.angularSpeed = stats.turnSpeed * 60f; // Convertir a grados/segundo
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
        }
    }

    public void SetTarget(Transform target)
    {
        if (agent == null || !agent.enabled) return;

        followTarget = target;

        if (target != null)
        {
            SetDestination(target.position);
            lastSetTargetTime = Time.time;
        }
    }

    public void SetDestination(Vector3 position)
    {
        if (agent == null || !agent.enabled) return;

        // 🔥 Solo setear si cambió significativamente
        if (Vector3.Distance(position, lastDestination) > 0.5f)
        {
            agent.SetDestination(position);
            lastDestination = position;
        }
    }

    public bool HasReachedDestination()
    {
        if (agent == null || !agent.enabled)
            return true;

        // No tiene path o ya llegó
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
                return true;
        }

        return false;
    }

    public void SetEnabled(bool enabled)
    {
        if (agent != null)
            agent.enabled = enabled;
    }

    public void ResetMovement()
    {
        followTarget = null;
        lastDestination = Vector3.zero;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        rotateTowardsMovement = true;
        if (agent != null)
            agent.updateRotation = true;
    }

    // ═══════ UTILIDADES ═══════

    public void Stop()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    public void SetSpeed(float speed)
    {
        if (agent != null)
            agent.speed = speed;
    }

    public float GetSpeed()
    {
        return agent != null ? agent.speed : 0f;
    }

    public Vector3 GetVelocity()
    {
        return agent != null ? agent.velocity : Vector3.zero;
    }

    // ═══════ GIZMOS ═══════

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugPath || agent == null || !agent.hasPath)
            return;

        // Dibujar path del NavMesh
        Gizmos.color = Color.cyan;
        Vector3[] corners = agent.path.corners;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
            Gizmos.DrawSphere(corners[i], 0.1f);
        }

        if (corners.Length > 0)
            Gizmos.DrawSphere(corners[corners.Length - 1], 0.15f);

        // Dibujar destino
        if (followTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, followTarget.position);
            Gizmos.DrawWireSphere(followTarget.position, 0.5f);
        }
    }
#endif
}