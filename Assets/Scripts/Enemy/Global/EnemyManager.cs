using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager global - ULTRA OPTIMIZADO para 100+ enemigos
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("═══════ PLAYER ═══════")]
    public Transform player;

    [Header("═══════ LÍMITES LOD ═══════")]
    [Range(10, 100)]
    public int maxActiveEnemies = 50;

    [Header("═══════ DISTANCIAS LOD ═══════")]
    [Range(10f, 50f)]
    public float activeDistance = 30f;

    [Range(30f, 100f)]
    public float semiActiveDistance = 60f;

    [Range(50f, 150f)]
    public float sleepDistance = 90f;

    [Header("═══════ CONTROL DE REPATH ═══════")]
    [Range(1, 20)]
    public int maxRepathsPerFrame = 5;

    [Header("═══════ OPTIMIZACIÓN ═══════")]
    [Range(0.1f, 2f)]
    [Tooltip("Intervalo entre actualizaciones de LOD (segundos)")]
    public float lodUpdateInterval = 0.2f;

    [Header("═══════ DEBUG ═══════")]
    public bool showDebugOverlay = true;
    public Vector2 debugPosition = new Vector2(10, 10);

    // ═══════ LISTAS ═══════

    readonly List<EnemyController> allEnemies = new List<EnemyController>();

    // ═══════ CONTADORES ═══════

    int repathsThisFrame;

    int activeCount;
    int semiActiveCount;
    int sleepCount;
    int totalCount;

    // ═══════ FPS ═══════

    float fps;
    float fpsTimer;
    int frameCount;

    // ═══════ CACHE OPTIMIZADO ═══════

    Vector3 lastPlayerPosition;
    float lodUpdateTimer;

    // Pre-calculados (para evitar multiplicar cada frame)
    float activeDistanceSqr;
    float semiActiveDistanceSqr;
    float sleepDistanceSqr;

    const float PLAYER_MOVE_THRESHOLD = 3f;
    const float CLEANUP_INTERVAL = 2f;
    float cleanupTimer;

    // ═══════ UNITY LIFECYCLE ═══════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Pre-calcular distancias al cuadrado (más rápido)
        RecalculateDistances();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        FindPlayer();
        if (player != null)
            lastPlayerPosition = player.position;
    }

    void Update()
    {
        UpdateLOD();
        UpdateFPS();

        // Limpieza periódica
        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= CLEANUP_INTERVAL)
        {
            CleanupDeadEnemies();
            cleanupTimer = 0f;
        }
    }

    void LateUpdate()
    {
        repathsThisFrame = 0;
    }

    // ═══════ REGISTRO ═══════

    public void Register(EnemyController enemy)
    {
        if (enemy == null || allEnemies.Contains(enemy)) return;
        allEnemies.Add(enemy);
    }

    public void Unregister(EnemyController enemy)
    {
        allEnemies.Remove(enemy);
    }

    // ═══════ REPATH CONTROL ═══════

    public bool CanRepath()
    {
        if (repathsThisFrame >= maxRepathsPerFrame)
            return false;

        repathsThisFrame++;
        return true;
    }

    // ═══════ LOD SYSTEM - ULTRA OPTIMIZADO ═══════

    void UpdateLOD()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // 🔥 OPTIMIZACIÓN 1: Update por intervalo, no cada frame
        lodUpdateTimer += Time.deltaTime;
        if (lodUpdateTimer < lodUpdateInterval)
            return;

        lodUpdateTimer = 0f;

        // 🔥 OPTIMIZACIÓN 2: Solo si el jugador se movió lo suficiente
        float playerMoveSqr = (player.position - lastPlayerPosition).sqrMagnitude;
        if (playerMoveSqr < PLAYER_MOVE_THRESHOLD * PLAYER_MOVE_THRESHOLD)
            return;

        lastPlayerPosition = player.position;

        // Reset contadores
        activeCount = 0;
        semiActiveCount = 0;
        sleepCount = 0;
        totalCount = 0;

        int activeAssigned = 0;

        // 🔥 OPTIMIZACIÓN 3: Iterar UNA SOLA VEZ sin sorting completo
        // En vez de ordenar toda la lista, encontramos los N más cercanos

        EnemyController[] closestActives = new EnemyController[maxActiveEnemies];
        float[] closestDistances = new float[maxActiveEnemies];

        // Inicializar con valores máximos
        for (int i = 0; i < maxActiveEnemies; i++)
            closestDistances[i] = float.MaxValue;

        // Primera pasada: clasificar y encontrar los más cercanos
        for (int i = 0; i < allEnemies.Count; i++)
        {
            EnemyController enemy = allEnemies[i];

            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            totalCount++;

            // 🔥 USAR SQUARED MAGNITUDE (mucho más rápido)
            float distSqr = (enemy.transform.position - player.position).sqrMagnitude;

            // Prioridad por stats
            float priority = enemy.stats != null ? enemy.stats.lodPriority * 100f : 0f;
            float scoreSqr = distSqr - priority;

            // Muy lejos → Sleep inmediato
            if (distSqr > sleepDistanceSqr)
            {
                enemy.SetLOD(EnemyLOD.Sleep);
                sleepCount++;
                continue;
            }

            // Distancia media → SemiActive
            if (distSqr > semiActiveDistanceSqr)
            {
                enemy.SetLOD(EnemyLOD.SemiActive);
                semiActiveCount++;
                continue;
            }

            // Cerca → Candidato para Active
            if (distSqr <= activeDistanceSqr)
            {
                // Insertar en array de más cercanos
                InsertIntoClosest(enemy, scoreSqr, closestActives, closestDistances);
            }
            else
            {
                // Entre active y semi → SemiActive
                enemy.SetLOD(EnemyLOD.SemiActive);
                semiActiveCount++;
            }
        }

        // Segunda pasada: Asignar Active a los más cercanos
        for (int i = 0; i < maxActiveEnemies; i++)
        {
            if (closestActives[i] != null)
            {
                closestActives[i].SetLOD(EnemyLOD.Active);
                activeCount++;
            }
        }
    }

    // Helper para insertar en array ordenado (más rápido que sort completo)
    void InsertIntoClosest(EnemyController enemy, float score, EnemyController[] array, float[] scores)
    {
        // Encontrar posición de inserción
        for (int i = 0; i < array.Length; i++)
        {
            if (score < scores[i])
            {
                // Mover todos hacia abajo
                for (int j = array.Length - 1; j > i; j--)
                {
                    array[j] = array[j - 1];
                    scores[j] = scores[j - 1];
                }

                // Insertar
                array[i] = enemy;
                scores[i] = score;
                return;
            }
        }
    }

    // ═══════ UTILIDADES ═══════

    void RecalculateDistances()
    {
        activeDistanceSqr = activeDistance * activeDistance;
        semiActiveDistanceSqr = semiActiveDistance * semiActiveDistance;
        sleepDistanceSqr = sleepDistance * sleepDistance;
    }

    void OnValidate()
    {
        RecalculateDistances();
    }

    void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
        }
    }

    void CleanupDeadEnemies()
    {
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            if (allEnemies[i] == null)
                allEnemies.RemoveAt(i);
        }
    }

    // ═══════ FPS ═══════

    void UpdateFPS()
    {
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 0.5f)
        {
            fps = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }
    }

    // ═══════ DEBUG GUI ═══════

    void OnGUI()
    {
        if (!showDebugOverlay) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        Rect area = new Rect(debugPosition.x, debugPosition.y, 300, 240);
        GUI.Box(area, "");

        float y = debugPosition.y + 10;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "═══ ENEMY MANAGER ═══", style);
        y += 25;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"FPS: {fps:F1}", style);
        y += 25;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"🟢 Active:        {activeCount}/{maxActiveEnemies}", style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"🟡 SemiActive:    {semiActiveCount}", style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"🔴 Sleep:         {sleepCount}", style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"━━━━━━━━━━━━━━━━", style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"Total Activos:    {totalCount}", style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"Registrados:      {allEnemies.Count}", style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), $"Repaths/Frame:    {repathsThisFrame}/{maxRepathsPerFrame}", style);
    }

    // ═══════ GIZMOS ═══════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
        }

        if (player == null) return;

        Vector3 center = player.position;

        // Active (Verde)
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(center, activeDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, activeDistance);

        // SemiActive (Amarillo)
        Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
        Gizmos.DrawSphere(center, semiActiveDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, semiActiveDistance);

        // Sleep (Rojo)
        Gizmos.color = new Color(1f, 0f, 0f, 0.08f);
        Gizmos.DrawSphere(center, sleepDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, sleepDistance);
    }
#endif
}