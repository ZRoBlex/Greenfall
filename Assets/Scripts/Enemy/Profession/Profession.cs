using UnityEngine;

public enum ProfessionType
{
    Warrior,
    Archer,
    Mage,
    Healer,
    Merchant,
    // Agrega más según tus necesidades
}

[CreateAssetMenu(menuName = "Enemies/Profession", fileName = "NewProfession")]
public class Profession : ScriptableObject
{
    [Header("Identidad")]
    public ProfessionType professionType;
    public string displayName = "New Profession";

    [Header("Modelo y Prefab")]
    [Tooltip("Prefab del enemigo con este modelo")]
    public GameObject modelPrefab;

    [Tooltip("Animator Controller específico para esta profesión")]
    public RuntimeAnimatorController animatorController;

    [Header("Animaciones Opcionales")]
    [Tooltip("Animación de ataque principal de la profesión")]
    public string attackAnim = "Attack";

    [Tooltip("Animación de caminar / desplazamiento")]
    public string walkAnim = "Walk";

    [Tooltip("Animación de idle / quieto")]
    public string idleAnim = "Idle";

    [Tooltip("Animación especial / habilidad (comentada para futuras habilidades)")]
    public string specialAnim = "Special";

    [Header("Estados Especiales")]
    [Tooltip("Tiempo que dura el estado de 'special' (comentado, para futuras habilidades)")]
    public float specialDuration = 2f;

    // -----------------------------
    // CAMPOS DE ATRIBUTOS DUPLICADOS ELIMINADOS
    // Se usarán los valores del EnemyStats para movimiento, ataque, rango, etc.
    // -----------------------------

    // [Header("Atributos Base de Profesión")]
    // public float baseHealth = 100f;
    // public float baseMoveSpeed = 3.5f;
    // public float baseAttackDamage = 10f;
    // public float baseAttackRange = 2f;
    // public float baseAttackCooldown = 1.5f;

    //[Header("Otros Ajustes (comentados para futuras mejoras)")]
    // public float perceptionRange = 12f;
    // public float minDistanceToTarget = 1f;
}
