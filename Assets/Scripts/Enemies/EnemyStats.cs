using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identidad")]
    public string displayName = "Enemy";

    [Header("Movimiento")]
    public float moveSpeed = 3.5f;
    public float runSpeed = 6f;
    public float turnSpeed = 8f; // grados por segundo para slerp

    [Header("Percepción")]
    public float perceptionRange = 12f;
    [Range(10, 180)] public float fieldOfView = 120f;

    [Header("Combate")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    [Header("Wander")]
    public float wanderRadius = 8f;
    public float wanderWaitTime = 2f;

    [Header("Comportamiento")]
    public CannibalType cannibalType = CannibalType.Aggressive;
    // Passive behavior
    public float passiveSafeDistance = 3f;
    public float passiveRetreatSpeed = 2f;

    [Header("Salud & Captura")]
    public float maxHealth = 100f;
    public float captureMeterMax = 100f;
    public float captureDecayPerSecond = 5f;
    public float capturePerHit = 20f;

    [Header("Equipo / Facción")]
    public int team = 0; // 0 enemigo por defecto, 1 jugador/equipo, etc.

    [Header("Percepción extra")]
    public float closeDetectionRadius = 2f;


}
