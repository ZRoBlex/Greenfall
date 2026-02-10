using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Enemy Stats", fileName = "NewEnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("═══════ IDENTIDAD ═══════")]
    public string displayName = "Enemy";

    [Tooltip("ID único para este tipo de enemigo")]
    public string enemyID = "default";

    [Header("═══════ MOVIMIENTO ═══════")]
    [Range(0.5f, 10f)]
    public float moveSpeed = 3.5f;

    [Range(0.5f, 15f)]
    public float runSpeed = 6f;

    [Range(1f, 20f)]
    public float turnSpeed = 8f;

    [Header("═══════ PERCEPCIÓN ═══════")]
    [Range(5f, 50f)]
    public float perceptionRange = 12f;

    [Range(30, 180)]
    public float fieldOfView = 120f;

    [Range(1f, 10f)]
    public float closeDetectionRadius = 2f;

    [Header("═══════ COMBATE ═══════")]
    [Range(0.5f, 10f)]
    public float attackRange = 2f;

    [Range(0f, 100f)]
    public float attackDamage = 10f;

    [Range(0.1f, 5f)]
    public float attackCooldown = 1.2f;

    [Tooltip("¿Puede hacer daño al jugador?")]
    public bool canDealDamage = true;

    [Header("═══════ SALUD ═══════")]
    [Range(10f, 500f)]
    public float maxHealth = 100f;

    [Tooltip("Salud se regenera con el tiempo?")]
    public bool canRegenerate = false;

    [Range(0f, 20f)]
    public float healthRegenPerSecond = 0f;

    [Header("═══════ CAPTURA (No Letal) ═══════")]
    [Range(10f, 200f)]
    public float captureMeterMax = 100f;

    [Range(1f, 20f)]
    public float captureDecayPerSecond = 5f;

    [Range(1f, 50f)]
    public float capturePerHit = 20f;

    [Range(5f, 60f)]
    public float unconsciousDuration = 30f;

    [Header("═══════ COMPORTAMIENTO ═══════")]
    [Tooltip("Tipo de comportamiento por defecto")]
    public CannibalType defaultType = CannibalType.Aggressive;

    [Tooltip("¿Puede cambiar de tipo dinámicamente?")]
    public bool canChangeType = true;

    [Header("═══════ WANDER (Patrulla) ═══════")]
    [Range(2f, 20f)]
    public float wanderRadius = 8f;

    [Range(0.5f, 10f)]
    public float wanderWaitTime = 2f;

    [Range(1f, 10f)]
    public float lookDuration = 3f;

    [Header("═══════ FOLLOWING (Persecución) ═══════")]
    [Range(1, 10)]
    public int followMinDistance = 1;

    [Range(2, 15)]
    public int followMaxDistance = 3;

    [Header("═══════ SCARED (Pasivo/Huida) ═══════")]
    [Range(2f, 15f)]
    [Tooltip("Distancia mínima segura del jugador")]
    public float scaredSafeDistance = 6f;

    [Range(2f, 15f)]
    [Tooltip("Distancia para dejar de huir (mismo que scaredSafeDistance normalmente)")]
    public float passiveSafeDistance = 6f;

    [Range(0.5f, 5f)]
    public float scaredRepathTime = 1.5f;

    [Range(3, 15)]
    public int scaredSearchRadius = 8;

    [Header("═══════ OBSTÁCULOS ═══════")]
    public LayerMask obstacleLayers;
    public string[] obstacleTags;

    [Range(0.5f, 5f)]
    public float obstacleCheckHeight = 1f;

    [Range(0.5f, 5f)]
    public float cellHeight = 2f;

    [Header("═══════ GRID PATHFINDING ═══════")]
    [Range(0.25f, 2f)]
    public float cellSize = 1f;

    [Range(5, 30)]
    public int gridRadius = 15;

    [Header("═══════ ANIMACIONES ═══════")]
    public string idleAnim = "Idle";
    public string walkAnim = "Walk";
    public string chaseAnim = "Chase";
    public string scaredAnim = "Scared";
    public string lookAnim = "Look";
    public string attackAnim = "Attack";
    public string stunnedAnim = "Stunned";

    [Header("═══════ OPTIMIZACIÓN ═══════")]
    [Tooltip("Prioridad en LOD (mayor = más importante)")]
    [Range(0, 10)]
    public int lodPriority = 5;

    [Tooltip("¿Puede ser pausado completamente en Sleep?")]
    public bool canSleep = true;

    // ═══════ VALIDACIÓN ═══════

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0.5f, moveSpeed);
        runSpeed = Mathf.Max(moveSpeed, runSpeed);
        attackRange = Mathf.Max(0.5f, attackRange);
        followMaxDistance = Mathf.Max(followMinDistance + 1, followMaxDistance);

        // Mantener passiveSafeDistance sincronizado con scaredSafeDistance
        passiveSafeDistance = scaredSafeDistance;

        if (string.IsNullOrEmpty(enemyID))
            enemyID = name.ToLower().Replace(" ", "_");
    }
}