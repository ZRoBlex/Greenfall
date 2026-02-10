using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pool optimizado de enemigos con pre-calentamiento y limpieza automática
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Header("═══════ PREFAB ═══════")]
    [SerializeField] EnemyController enemyPrefab;

    [Header("═══════ CONFIGURACIÓN ═══════")]
    [SerializeField] int initialSize = 20;
    [SerializeField] int maxSize = 100;

    [Tooltip("Pre-calentar el pool al inicio?")]
    [SerializeField] bool warmupOnStart = true;

    [Header("═══════ DEBUG ═══════")]
    [SerializeField] bool showDebugInfo = false;

    // Pool principal
    readonly Queue<EnemyController> availableEnemies = new();

    // Tracking de enemigos activos
    readonly HashSet<EnemyController> activeEnemies = new();

    // Estadísticas
    int totalCreated = 0;
    int totalReused = 0;

    // ═══════ UNITY LIFECYCLE ═══════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (warmupOnStart)
            Warmup();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ═══════ PUBLIC API ═══════

    /// <summary>
    /// Obtiene un enemigo del pool (o crea uno nuevo si es necesario)
    /// </summary>
    public EnemyController Get()
    {
        EnemyController enemy;

        if (availableEnemies.Count > 0)
        {
            enemy = availableEnemies.Dequeue();
            totalReused++;
        }
        else
        {
            if (totalCreated >= maxSize)
            {
                Debug.LogWarning($"[EnemyPool] Límite alcanzado ({maxSize}). Reutilizando enemigo más antiguo.");
                enemy = GetOldestActive();
            }
            else
            {
                enemy = CreateNew();
            }
        }

        if (enemy != null)
        {
            PrepareForUse(enemy);
            activeEnemies.Add(enemy);
        }

        return enemy;
    }

    /// <summary>
    /// Devuelve un enemigo al pool
    /// </summary>
    public void Release(EnemyController enemy)
    {
        if (enemy == null) return;

        if (!activeEnemies.Contains(enemy))
        {
            if (showDebugInfo)
                Debug.LogWarning($"[EnemyPool] Intentando devolver enemigo que no estaba activo: {enemy.name}");
            return;
        }

        PrepareForStorage(enemy);

        activeEnemies.Remove(enemy);
        availableEnemies.Enqueue(enemy);

        if (showDebugInfo)
            Debug.Log($"[EnemyPool] Devuelto al pool. Disponibles: {availableEnemies.Count}, Activos: {activeEnemies.Count}");
    }

    /// <summary>
    /// Devuelve un enemigo al pool (alias para compatibilidad)
    /// </summary>
    public void Return(EnemyController enemy) => Release(enemy);

    /// <summary>
    /// Pre-calienta el pool creando enemigos por adelantado
    /// </summary>
    public void Warmup()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNew();
        }

        if (showDebugInfo)
            Debug.Log($"[EnemyPool] Pre-calentado con {initialSize} enemigos");
    }

    /// <summary>
    /// Limpia enemigos destruidos o inválidos
    /// </summary>
    public void Cleanup()
    {
        // Limpiar activos
        activeEnemies.RemoveWhere(e => e == null);

        // Limpiar disponibles
        int count = availableEnemies.Count;
        for (int i = 0; i < count; i++)
        {
            EnemyController e = availableEnemies.Dequeue();
            if (e != null)
                availableEnemies.Enqueue(e);
        }

        if (showDebugInfo)
            Debug.Log($"[EnemyPool] Limpieza completada. Disponibles: {availableEnemies.Count}, Activos: {activeEnemies.Count}");
    }

    // ═══════ INTERNAL METHODS ═══════

    EnemyController CreateNew()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemyPool] No hay prefab asignado!");
            return null;
        }

        EnemyController enemy = Instantiate(enemyPrefab, transform);
        enemy.name = $"Enemy_{totalCreated:D3}";
        enemy.gameObject.SetActive(false);

        availableEnemies.Enqueue(enemy);
        totalCreated++;

        if (showDebugInfo)
            Debug.Log($"[EnemyPool] Creado nuevo enemigo ({totalCreated}/{maxSize})");

        return enemy;
    }

    void PrepareForUse(EnemyController enemy)
    {
        if (enemy == null) return;

        // Reset completo del enemigo
        ResetEnemy(enemy);

        // Activar
        enemy.gameObject.SetActive(true);
    }

    void PrepareForStorage(EnemyController enemy)
    {
        if (enemy == null) return;

        // Desactivar
        enemy.gameObject.SetActive(false);

        // Limpiar referencias
        enemy.transform.SetParent(transform);
        enemy.transform.localPosition = Vector3.zero;
        enemy.transform.localRotation = Quaternion.identity;
    }

    void ResetEnemy(EnemyController enemy)
    {
        if (enemy == null) return;

        // Salud letal
        Health health = enemy.GetComponent<Health>();
        if (health != null)
            health.ResetHealth();

        // Salud no letal
        NonLethalHealth nonLethal = enemy.GetComponent<NonLethalHealth>();
        if (nonLethal != null)
            nonLethal.ResetHealth();

        // Motor
        if (enemy.Motor != null)
            enemy.Motor.rotateTowardsMovement = true;

        // Perception
        if (enemy.Perception != null)
            enemy.Perception.SetExternalTarget(null);

        // LOD
        enemy.SetLOD(EnemyLOD.Active);

        // FSM - lo haremos después cuando tengamos los estados
    }

    EnemyController GetOldestActive()
    {
        // Devuelve el primer activo que encuentre
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
                return enemy;
            }
        }

        return CreateNew();
    }

    // ═══════ DEBUG ═══════

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        float y = 150;
        GUI.Label(new Rect(10, y, 300, 25), $"═══ ENEMY POOL ═══", style);
        y += 25;
        GUI.Label(new Rect(10, y, 300, 25), $"Disponibles: {availableEnemies.Count}", style);
        y += 22;
        GUI.Label(new Rect(10, y, 300, 25), $"Activos: {activeEnemies.Count}", style);
        y += 22;
        GUI.Label(new Rect(10, y, 300, 25), $"Total Creados: {totalCreated}", style);
        y += 22;
        GUI.Label(new Rect(10, y, 300, 25), $"Total Reusados: {totalReused}", style);
    }
}