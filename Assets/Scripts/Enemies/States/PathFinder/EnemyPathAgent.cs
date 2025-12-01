using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementGrounded))]
public class EnemyPathAgent : MonoBehaviour
{
    [Header("Pathfinder")]
    public LocalGridPathfinder pathfinder; // si está vacío, lo toma en Awake
    [Tooltip("Capas que el pathfinder debe IGNORAR (p. ej. Enemies).")]
    public LayerMask ignoreMask = 0;

    [Header("Movimiento")]
    public float waypointStopDistance = 0.5f;   // cuándo consideramos alcanzado un waypoint
    public float defaultSpeed = 3.5f;

    [Header("Stuck detection")]
    public bool enableAutoUnstick = true;
    public float stuckThresholdTime = 0.6f;     // segundos sin moverse -> stuck
    public float minMovementEpsilon = 0.03f;    // movimiento mínimo para considerarse movimiento
    public float unstickDuration = 0.35f;       // tiempo que aplica empuje para salir
    public float unstickSpeedMultiplier = 1.2f; // multiplicador de velocidad durante unstick

    // estado interno
    MovementGrounded movement;
    List<Vector3> path = new List<Vector3>();
    int pathIndex = 0;
    float speedForMove = 0f;
    bool isMoving = false;

    // stuck detection
    Vector3 lastPos;
    float stuckTimer = 0f;
    float unstickTimer = 0f;
    Vector3 unstickDir = Vector3.zero;

    // Exposición pública
    public bool IsMoving => isMoving;

    void Awake()
    {
        movement = GetComponent<MovementGrounded>();
        if (pathfinder == null) pathfinder = GetComponent<LocalGridPathfinder>();
    }

    void Update()
    {
        // --- UNSTICK EN PROGRESO ---
        if (unstickTimer > 0f)
        {
            unstickTimer -= Time.deltaTime;
            movement.MoveDirection_NoRotate(unstickDir, speedForMove * unstickSpeedMultiplier);
            return;
        }

        if (!isMoving || path == null || path.Count == 0)
        {
            isMoving = false;
            return;
        }

        // --- FAILSAFE: SI EL PATH TIENE SOLO 1 PUNTO, CANCELAR Y NO QUEDARSE QUIETO ---
        if (path.Count <= 1)
        {
            ClearPath();
            return;
        }

        // Proteger el índice
        pathIndex = Mathf.Clamp(pathIndex, 0, path.Count - 1);
        Vector3 node = path[pathIndex];

        // --- MOVER HACIA EL NODO ---
        movement.MoveTowards(node, speedForMove);

        // Debug target
        var ec = GetComponent<EnemyController>();
        if (ec != null) ec.debugTarget = node;

        float dist = Vector3.Distance(transform.position, node);

        // --- LLEGADA CON COLCHÓN DE TOLERANCIA ---
        // --- LLEGADA CON TOLERANCIA REALISTA ---
        float reachDistance = Mathf.Max(waypointStopDistance, 1.1f);  // antes era demasiado pequeño

        if (dist <= reachDistance)
        {
            // Avanzamos al siguiente nodo
            pathIndex++;

            // Empuje suave para evitar quedarse atorado exactamente en la transición
            Vector3 nudge = (node - transform.position).normalized;
            movement.MoveDirection_NoRotate(nudge, 0.5f);

            // Si no hay más nodos → terminar el path
            if (pathIndex >= path.Count)
            {
                ClearPath();
                return;
            }

            // Evitar loop de 2 nodos repetidos
            if (pathIndex == 1)
            {
                float d0 = Vector3.Distance(path[0], path[1]);
                if (d0 < 0.6f)
                {
                    ClearPath();
                    return;
                }
            }
        }


        UpdateStuckDetection();
    }


    void UpdateStuckDetection()
    {
        // si no activado, salir
        if (!enableAutoUnstick) return;

        // si nos movemos lo suficiente (por CurrentSpeed o por posición) => no stuck
        float movedSqr = (transform.position - lastPos).sqrMagnitude;
        if (movedSqr > (minMovementEpsilon * minMovementEpsilon) || movement.CurrentSpeed > 0.01f)
        {
            stuckTimer = 0f;
            lastPos = transform.position;
            return;
        }

        // si no se movió: aumenta timer
        stuckTimer += Time.deltaTime;
        lastPos = transform.position;

        if (stuckTimer >= stuckThresholdTime)
        {
            // estamos stuck => intentar unstick aleatorio
            TryUnstick();
            stuckTimer = 0f;
        }
    }

    void TryUnstick()
    {
        // generar una dirección lateral/aleatoria sobre XZ para empujar
        Vector2 rnd = Random.insideUnitCircle.normalized;
        unstickDir = new Vector3(rnd.x, 0f, rnd.y);
        unstickTimer = unstickDuration;
        // nota: seguimos moviendo con MoveDirection_NoRotate en Update mientras unstickTimer > 0
    }

    /// <summary>
    /// Pide ruta UNA vez y la sigue. No recalcule automáticamente.
    /// </summary>
    public void MoveTo(Vector3 worldTarget, float speed)
    {
        if (pathfinder == null)
        {
            pathfinder = GetComponent<LocalGridPathfinder>();
            if (pathfinder == null)
            {
                Debug.LogWarning($"{name} EnemyPathAgent: no LocalGridPathfinder encontrado.");
                return;
            }
        }

        // pasar ignoreMask al pathfinder
        //pathfinder.ignoreL = ignoreMask;

        // conseguir ruta (lista de puntos world)
        var result = pathfinder.FindPath(transform.position, worldTarget);
        if (result == null || result.Count == 0)
        {
            // sin path
            ClearPath();
            isMoving = false;
            return;
        }

        path = new List<Vector3>(result);
        pathIndex = 0;
        speedForMove = (speed <= 0f) ? defaultSpeed : speed;
        isMoving = true;

        // reset stuck tracker
        lastPos = transform.position;
        stuckTimer = 0f;
    }

    public void Stop()
    {
        ClearPath();
        movement.StopInstantly();
        isMoving = false;
    }

    void ClearPath()
    {
        path?.Clear();
        path = new List<Vector3>();
        pathIndex = 0;
        isMoving = false;
    }

    /// <summary>
    /// Indica si el agente está atascado en su último movimiento.
    /// (puedes llamarlo desde estados si quieres)
    /// </summary>
    public bool IsStuck()
    {
        // consideramos stuck si llevamos tiempo sin movimiento (sin necesidad de cambiar timers aquí)
        // Exponemos el chequeo simple: si lastPos está próximo y movement.CurrentSpeed ~ 0
        float movedSqr = (transform.position - lastPos).sqrMagnitude;
        bool notMoved = movedSqr <= (minMovementEpsilon * minMovementEpsilon) && movement.CurrentSpeed <= 0.01f;
        return notMoved && stuckTimer >= (stuckThresholdTime * 0.5f); // indicador conservador
    }
}
