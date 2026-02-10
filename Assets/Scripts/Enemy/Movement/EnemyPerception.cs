using UnityEngine;

/// <summary>
/// Sistema de percepción ULTRA optimizado
/// - No hace raycasts innecesarios
/// - Percepción por capas (distancia → visión → audición)
/// - Cache agresivo
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    public EnemyStats stats;

    [Header("═══════ OPTIMIZACIÓN ═══════")]
    [Range(0.1f, 1f)]
    [Tooltip("Intervalo entre checks de percepción (segundos)")]
    public float perceptionInterval = 0.2f;

    [Range(0.1f, 1f)]
    [Tooltip("Intervalo entre checks de visión (raycasts)")]
    public float visionCheckInterval = 0.5f;

    // ═══════ ESTADO ═══════

    public bool CanSeePlayer { get; private set; }
    public bool IsPlayerClose { get; private set; }
    public bool HeardPlayer { get; private set; }
    public Transform CurrentTarget { get; private set; }

    // ═══════ CACHE ═══════

    Transform player;
    Transform cachedTransform;

    // Pre-calculados (para evitar multiplicaciones)
    float closeDetectionSqr;
    float perceptionRangeSqr;
    float hearingRangeSqr;
    float halfFieldOfView;

    // ═══════ TIMERS ═══════

    float perceptionTimer;
    float visionTimer;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        cachedTransform = transform;
        FindPlayer();
        RecalculateRanges();
    }

    void OnEnable()
    {
        perceptionTimer = Random.Range(0f, perceptionInterval); // Stagger inicial
        visionTimer = Random.Range(0f, visionCheckInterval);
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                ClearPerception();
                return;
            }
        }

        // 🔥 OPTIMIZACIÓN 1: Percepción por intervalos, no cada frame
        perceptionTimer -= Time.deltaTime;
        if (perceptionTimer > 0f)
            return;

        perceptionTimer = perceptionInterval;

        // 🔥 OPTIMIZACIÓN 2: Percepción por capas (más barato primero)
        UpdatePerception();
    }

    // ═══════ PERCEPCIÓN POR CAPAS ═══════

    void UpdatePerception()
    {
        CurrentTarget = null;

        // CAPA 1: Distancia (más barato - solo squared magnitude)
        float distSqr = (cachedTransform.position - player.position).sqrMagnitude;

        // Muy cerca → detectado automáticamente
        IsPlayerClose = distSqr <= closeDetectionSqr;
        if (IsPlayerClose)
        {
            CurrentTarget = player;
            CanSeePlayer = true;
            HeardPlayer = true;
            return;
        }

        // Fuera de rango de percepción → ignorar
        if (distSqr > perceptionRangeSqr)
        {
            ClearPerception();
            return;
        }

        // CAPA 2: Audición (barato - solo distancia)
        HeardPlayer = distSqr <= hearingRangeSqr;

        // CAPA 3: Visión (más caro - raycast)
        visionTimer -= Time.deltaTime;
        if (visionTimer <= 0f)
        {
            CheckVision(distSqr);
            visionTimer = visionCheckInterval;
        }

        // Asignar target si detectado
        if (CanSeePlayer || HeardPlayer)
            CurrentTarget = player;
    }

    void CheckVision(float distSqr)
    {
        // Ya fuera de rango
        if (distSqr > perceptionRangeSqr)
        {
            CanSeePlayer = false;
            return;
        }

        // Verificar ángulo (barato)
        Vector3 toPlayer = player.position - cachedTransform.position;
        float angle = Vector3.Angle(cachedTransform.forward, toPlayer);

        if (angle > halfFieldOfView)
        {
            CanSeePlayer = false;
            return;
        }

        // TODO: Raycast para verificar obstrucción (opcional)
        // Por ahora, si está en ángulo = puede ver
        CanSeePlayer = true;
    }

    void ClearPerception()
    {
        CurrentTarget = null;
        CanSeePlayer = false;
        IsPlayerClose = false;
        HeardPlayer = false;
    }

    // ═══════ UTILIDADES ═══════

    void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
        }
    }

    void RecalculateRanges()
    {
        if (stats == null) return;

        closeDetectionSqr = stats.closeDetectionRadius * stats.closeDetectionRadius;
        perceptionRangeSqr = stats.perceptionRange * stats.perceptionRange;
        hearingRangeSqr = (stats.perceptionRange * 0.6f) * (stats.perceptionRange * 0.6f);
        halfFieldOfView = stats.fieldOfView * 0.5f;
    }

    void OnValidate()
    {
        RecalculateRanges();
    }

    // ═══════ API PÚBLICA ═══════

    public void SetExternalTarget(Transform target)
    {
        CurrentTarget = target;
    }

    // ═══════ GIZMOS ═══════

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (stats == null) return;

        // Cerca (Rojo)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, stats.closeDetectionRadius);

        // Audición (Azul)
        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange * 0.6f);

        // Visión (Amarillo)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange);

        // Campo de visión
        Vector3 left = Quaternion.Euler(0, -stats.fieldOfView / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, stats.fieldOfView / 2f, 0) * transform.forward;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + left * stats.perceptionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * stats.perceptionRange);

        // Línea al target
        if (CurrentTarget != null && Application.isPlaying)
        {
            Gizmos.color = CanSeePlayer ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
#endif
}