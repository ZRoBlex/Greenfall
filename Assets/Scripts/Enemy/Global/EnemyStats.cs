using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Enemy Stats", fileName = "EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identidad")]
    public string displayName = "Enemy";

    [Header("Movimiento")]
    public float moveSpeed = 3.5f;
    public float runSpeed = 6f;
    public float turnSpeed = 8f;

    [Header("Percepción")]
    public float perceptionRange = 12f;
    [Range(10, 180)] public float fieldOfView = 120f;
    public float closeDetectionRadius = 2f;

    [Header("Combate")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    public bool canDealDamage = true; // 👈 NUEVO

    [Header("Wander")]
    public float wanderRadius = 8f;
    public float wanderWaitTime = 2f;

    [Header("Salud & Captura")]
    public float maxHealth = 100f;
    public float captureMeterMax = 100f;
    public float captureDecayPerSecond = 5f;
    public float capturePerHit = 20f;

    [Header("Equipo / Facción")]
    public int team = 0;

    [Header("Obstáculos")]
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

    [Header("Wander / Looking")]
    [Tooltip("Tiempo que el enemigo se queda mirando")]
    public float lookDuration = 3f;

    [Header("Animaciones")]
    public string idleAnim = "Idle";
    public string walkAnim = "Walk";
    public string chaseAnim = "Chase";
    public string scaredAnim = "Scared";
    public string lookAnim = "Look";
    public string attackAnim = "Attack";
    public string stunnedAnim = "Stunned";

    [Header("EXTRAS")]
    public float passiveSafeDistance = 2;

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public int gridRadius = 15;

    // 🔹 Altura del cubo que define si puede caminar
    public float cellHeight = 2f;

}
