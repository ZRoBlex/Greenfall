using UnityEngine;

/// <summary>
/// Optimized Enemy Stats - Reduced serialization overhead and better data locality
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Optimized Enemy Stats", fileName = "OptimizedEnemyStats")]
public class OptimizedEnemyStats : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Enemy";

    [Header("Movement - Cached for performance")]
    public float moveSpeed = 3.5f;
    public float runSpeed = 6f;
    public float turnSpeed = 8f;
    public float stopDistance = 0.35f;

    [Header("Perception - Squared values calculated on load")]
    public float perceptionRange = 12f;
    [Range(10, 180)] public float fieldOfView = 120f;
    public float closeDetectionRadius = 2f;

    // Cached squared distances (avoid sqrt in gameplay)
    [System.NonSerialized] public float perceptionRangeSqr;
    [System.NonSerialized] public float closeDetectionRadiusSqr;
    [System.NonSerialized] public float halfFOV;

    [Header("Combat")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;
    public bool canDealDamage = true;

    [System.NonSerialized] public float attackRangeSqr;

    [Header("Wander")]
    public float wanderRadius = 8f;
    public float wanderWaitTime = 2f;

    [Header("Health & Capture")]
    public float maxHealth = 100f;
    public float captureMeterMax = 100f;
    public float captureDecayPerSecond = 5f;
    public float capturePerHit = 20f;

    [Header("Team / Faction")]
    public int team = 0;

    [Header("Obstacles")]
    public LayerMask obstacleLayers;
    public string[] obstacleTags;
    public float obstacleCheckHeight = 1f;

    [Header("Following")]
    public int followMinDistance = 1;
    public int followMaxDistance = 3;

    [Header("Scared / Passive AI")]
    public float scaredSafeDistance = 6f;
    public float scaredRepathTime = 1.5f;
    public int scaredSearchRadius = 8;

    [System.NonSerialized] public float scaredSafeDistanceSqr;

    [Header("Wander / Looking")]
    [Tooltip("Time enemy stays looking")]
    public float lookDuration = 3f;

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public int gridRadius = 15;
    public float cellHeight = 2f;

    /// <summary>
    /// Called when ScriptableObject is loaded - cache expensive calculations
    /// </summary>
    void OnEnable()
    {
        CacheExpensiveValues();
    }

    /// <summary>
    /// Pre-calculate squared distances to avoid sqrt during gameplay
    /// </summary>
    public void CacheExpensiveValues()
    {
        perceptionRangeSqr = perceptionRange * perceptionRange;
        closeDetectionRadiusSqr = closeDetectionRadius * closeDetectionRadius;
        attackRangeSqr = attackRange * attackRange;
        scaredSafeDistanceSqr = scaredSafeDistance * scaredSafeDistance;
        halfFOV = fieldOfView * 0.5f;
    }
}
