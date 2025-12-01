using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyPathAgent1 - advanced lightweight agent:
/// - MoveTo(destination) -> finds path via LocalGridPathfinder1
/// - Stop() -> cancels current path
/// - GetNextNode(), AdvanceNode(distance)
/// - UpdateDynamicPath() -> frequent repathing + validate path
/// - Smooth steering, node skipping, local avoidance (enemy layer)
/// - Exposes HasPath / IsMoving
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MovementGrounded))]
public class EnemyPathAgent1 : MonoBehaviour
{
    public LocalGridPathfinder1 pf;
    public List<Vector3> path = new List<Vector3>();
    public int index = 0;
    public Vector3 currentTarget;

    [Header("Repath")]
    public float repathInterval = 0.12f;
    float repathTimer = 0f;

    [Header("Movement")]
    public float waypointStopDistance = 0.6f;
    public float defaultSpeed = 3.5f;

    [Header("Smoothing / steering")]
    public float rotationSlerp = 8f;
    public float steeringLookAhead = 2; // nodes to try skip
    public LayerMask localAvoidLayer = 0; // set to your Enemy layer

    [Header("Stuck & Unstick")]
    public bool enableAutoUnstick = true;
    public float stuckThresholdTime = 0.6f;
    public float minMovementEpsilon = 0.03f;
    public float unstickDuration = 0.3f;
    public float unstickSpeedMultiplier = 1.2f;

    MovementGrounded movement;
    Vector3 lastPos;
    float stuckTimer = 0f;
    float unstickTimer = 0f;
    Vector3 unstickDir = Vector3.zero;

    void Awake()
    {
        movement = GetComponent<MovementGrounded>();
        if (pf == null) pf = GetComponent<LocalGridPathfinder1>();
        repathTimer = 0f;
    }

    void Update()
    {
        // unstick handling
        if (unstickTimer > 0f)
        {
            unstickTimer -= Time.deltaTime;
            movement.MoveDirection_NoRotate(unstickDir, defaultSpeed * unstickSpeedMultiplier);
            return;
        }

        if (!HasPath) return;

        // Node skipping: try to skip forward to a later node that is visible
        TrySkipNodes();

        Vector3 node = GetNextNode();

        // smooth steering: compute desired direction and rotate
        Vector3 desiredDir = (node - transform.position);
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSlerp * Time.deltaTime);
        }

        float moveSpeed = defaultSpeed;
        movement.MoveTowards(node, moveSpeed);

        // update debugTarget if owner exists
        var ec = GetComponent<EnemyController>();
        if (ec != null) ec.debugTarget = node;

        // arrival check with tolerance
        float dist = Vector3.Distance(transform.position, node);
        if (dist <= Mathf.Max(waypointStopDistance, 0.45f) || dist < 0.2f)
        {
            index++;
            if (index >= path.Count)
            {
                ClearPath();
                return;
            }
        }

        UpdateStuckDetection();
        // local avoidance each frame (small steering)
        AvoidNearbyAgents();
    }

    void TrySkipNodes()
    {
        if (pf == null || path == null || path.Count == 0) return;

        // try lookAhead nodes and check line of sight
        int maxLook = Mathf.Min(path.Count - 1, index + Mathf.Max(1, (int)steeringLookAhead));
        for (int i = maxLook; i > index; i--)
        {
            Vector3 p = path[i];
            // if there's line of sight to p (no obstacles), skip
            if (!Physics.Linecast(transform.position + Vector3.up * 0.6f, p + Vector3.up * 0.6f, pf.obstacleMask))
            {
                index = i;
                break;
            }
        }
    }

    void AvoidNearbyAgents()
    {
        if (localAvoidLayer == 0) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.2f, localAvoidLayer);
        Vector3 push = Vector3.zero;
        foreach (var h in hits)
        {
            if (h.transform == transform) continue;
            Vector3 away = transform.position - h.transform.position;
            away.y = 0;
            float mag = away.magnitude;
            if (mag > 0.001f) push += away.normalized / Mathf.Max(1f, mag);
        }
        if (push.sqrMagnitude > 0.0001f)
        {
            // small lateral move to avoid crowding
            movement.MoveDirection(push.normalized, defaultSpeed * 0.8f);
        }
    }

    void UpdateStuckDetection()
    {
        if (!enableAutoUnstick) return;

        float movedSqr = (transform.position - lastPos).sqrMagnitude;
        if (movedSqr > (minMovementEpsilon * minMovementEpsilon) || movement.CurrentSpeed > 0.01f)
        {
            stuckTimer = 0f;
            lastPos = transform.position;
            return;
        }

        stuckTimer += Time.deltaTime;
        lastPos = transform.position;

        if (stuckTimer >= stuckThresholdTime)
        {
            stuckTimer = 0f;
            TryUnstick();
        }
    }

    void TryUnstick()
    {
        Vector2 rnd = Random.insideUnitCircle.normalized;
        unstickDir = new Vector3(rnd.x, 0f, rnd.y);
        unstickTimer = unstickDuration;
    }

    /// <summary>
    /// Pide ruta y la reemplaza.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (pf == null) pf = GetComponent<LocalGridPathfinder1>();
        currentTarget = destination;
        if (pf == null)
        {
            ClearPath();
            return;
        }
        path = pf.FindPath(transform.position, currentTarget) ?? new List<Vector3>();
        index = 0;
        repathTimer = repathInterval;
    }

    public void Stop()
    {
        ClearPath();
    }

    public Vector3 GetNextNode()
    {
        if (path == null || path.Count == 0) return transform.position;
        index = Mathf.Clamp(index, 0, path.Count - 1);
        return path[index];
    }

    public bool AdvanceNode(float distance)
    {
        if (path == null || path.Count == 0) return true;
        if (Vector3.Distance(transform.position, path[index]) < distance)
        {
            index++;
            if (index >= path.Count)
            {
                ClearPath();
                return true;
            }
            return false;
        }
        return false;
    }

    /// <summary>
    /// Repath frecuente (cada repathInterval) + validación extra
    /// </summary>
    public void UpdateDynamicPath()
    {
        if (pf == null) pf = GetComponent<LocalGridPathfinder1>();
        if (pf == null) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            // recalcula ruta hacia currentTarget (predicción ya aplicada por caller)
            if (currentTarget != Vector3.zero)
            {
                path = pf.FindPath(transform.position, currentTarget) ?? new List<Vector3>();
                index = 0;
            }
            return;
        }

        // si el path fue bloqueado por nuevos obstáculos, recalcula (sanity)
        if (!pf.IsPathStillValid(path))
        {
            if (currentTarget != Vector3.zero)
            {
                path = pf.FindPath(transform.position, currentTarget) ?? new List<Vector3>();
                index = 0;
            }
        }
    }

    void ClearPath()
    {
        path?.Clear();
        path = new List<Vector3>();
        index = 0;
    }

    public bool HasPath => (path != null && path.Count > 0);
    public bool IsMoving => HasPath; // semantic alias
}
