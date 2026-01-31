using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController), typeof(NPCPathfinder), typeof(NPCLocalGrid))]
public class NPCMotor : MonoBehaviour
{
    public NPCStats stats;

    CharacterController controller;
    NPCPathfinder pathfinder;
    NPCLocalGrid localGrid;

    List<Vector3> path;
    int pathIndex;

    Vector3 targetPosition;
    bool hasTarget = false;

    float waitTimer = 0f;
    float verticalVelocity = 0f;
    const float GRAVITY = -20f;

    // Suavizado de dirección
    Vector3 smoothDirection;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        pathfinder = GetComponent<NPCPathfinder>();
        localGrid = GetComponent<NPCLocalGrid>();

        if (stats == null)
            Debug.LogError($"[NPCMotor] Missing NPCStats on {name}");
    }

    void Update()
    {
        // Esperando en destino
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        // Si no hay destino, elige uno nuevo
        if (!hasTarget || path == null || pathIndex >= path.Count)
        {
            ChooseRandomDestination();
            return;
        }

        MoveAlongPath();
        ApplyGravity();
        CheckObstacles();
    }

    void ChooseRandomDestination()
    {
        Vector2Int centerCell = localGrid.WorldToCell(transform.position);
        Vector2Int destCell;

        int attempts = 0;
        do
        {
            int x = Random.Range(-stats.wanderRadius, stats.wanderRadius + 1);
            int y = Random.Range(-stats.wanderRadius, stats.wanderRadius + 1);
            destCell = centerCell + new Vector2Int(x, y);
            attempts++;
        }
        while (!localGrid.IsWalkable(destCell) && attempts < 20);

        targetPosition = localGrid.CellToWorld(destCell);
        path = pathfinder.FindPath(localGrid.WorldToCell(transform.position), destCell);
        pathIndex = 0;
        hasTarget = path != null && path.Count > 0;
    }

    void MoveAlongPath()
    {
        if (path == null || pathIndex >= path.Count)
            return;

        Vector3 nextPoint = path[pathIndex];
        Vector3 flatNext = new Vector3(nextPoint.x, transform.position.y, nextPoint.z);
        Vector3 toNext = flatNext - transform.position;
        float distance = toNext.magnitude;

        if (distance < 0.3f) // cerca del punto
        {
            pathIndex++;
            if (pathIndex >= path.Count)
            {
                // Llegó al destino → esperar
                waitTimer = Random.Range(stats.minWaitTime, stats.maxWaitTime);
                hasTarget = false;
            }
            return;
        }

        Vector3 desiredDir = toNext.normalized;
        smoothDirection = Vector3.Slerp(smoothDirection == Vector3.zero ? desiredDir : smoothDirection, desiredDir, Time.deltaTime * stats.turnSpeed);

        // Rotación suave
        if (smoothDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(smoothDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * stats.turnSpeed);
        }

        // Movimiento
        Vector3 velocity = smoothDirection * stats.moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += GRAVITY * Time.deltaTime;
    }

    void CheckObstacles()
    {
        // OverlapBox frontal para detectar obstáculos
        Vector3 boxCenter = transform.position + transform.forward * 0.5f + Vector3.up * (stats.cellHeight * 0.5f);
        Vector3 halfExtents = new Vector3(0.3f, stats.cellHeight * 0.5f, 0.3f);

        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, transform.rotation, stats.obstacleLayers);
        foreach (var hit in hits)
        {
            if (stats.obstacleTags != null && stats.obstacleTags.Length > 0)
            {
                foreach (string tag in stats.obstacleTags)
                    if (hit.CompareTag(tag))
                    {
                        // Obstáculo → recalcula destino
                        hasTarget = false;
                        path = null;
                        return;
                    }
            }
            else
            {
                hasTarget = false;
                path = null;
                return;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (stats == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.wanderRadius);

        // Obstáculo frontal
        Gizmos.color = Color.red;
        Vector3 boxCenter = transform.position + transform.forward * 0.5f + Vector3.up * (stats.cellHeight * 0.5f);
        Vector3 halfExtents = new Vector3(0.3f, stats.cellHeight * 0.5f, 0.3f);
        Gizmos.DrawWireCube(boxCenter, halfExtents * 2f);
    }
#endif
}
