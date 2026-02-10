using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn manager ULTRA optimizado
/// Spawns masivos al inicio + mantenimiento de población
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    public EnemySpawnerArea area;
    public EnemyPool pool;

    [Header("═══════ SPAWN INICIAL ═══════")]
    [Range(1, 100)]
    public int initialCount = 15;

    [Range(5f, 30f)]
    public float minSpawnDistanceFromPlayer = 10f;

    [Header("═══════ DETECCIÓN DE SUPERFICIE ═══════")]
    public LayerMask groundMask;
    public float rayHeight = 150f;

    [Header("═══════ DESPAWN ═══════")]
    [Range(60f, 600f)]
    public float maxLifetime = 180f;

    public bool enableLifetimeDespawn = true;
    public bool enableDistanceDespawn = true;

    [Header("═══════ RESPAWN ═══════")]
    public bool maintainPopulation = true;

    [Range(1, 5)]
    public int maxSpawnPerFrame = 2;

    // ═══════ TRACKING ═══════

    readonly List<EnemyController> aliveEnemies = new List<EnemyController>();
    readonly Dictionary<EnemyController, float> aliveTimeByEnemy = new Dictionary<EnemyController, float>();

    // ═══════ LIFECYCLE ═══════

    void Start()
    {
        SpawnAll();
    }

    void Update()
    {
        CleanupDeadOrInactive();
        UpdateAliveTimes();
        HandleDespawnByRules();
        MaintainPopulation();
    }

    // ═══════ SPAWN MASIVO ═══════

    void SpawnAll()
    {
        aliveEnemies.Clear();
        aliveTimeByEnemy.Clear();

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = initialCount * 20;

        while (spawned < initialCount && attempts < maxAttempts)
        {
            if (TrySpawnOne())
                spawned++;

            attempts++;
        }

        if (spawned < initialCount)
        {
            Debug.LogWarning($"⚠️ Solo {spawned}/{initialCount} enemigos spawneados. Revisa groundMask y área.");
        }
        else
        {
            Debug.Log($"✅ {spawned} enemigos spawneados correctamente");
        }
    }

    bool TrySpawnOne()
    {
        if (!TryGetValidSpawnPosition(out Vector3 pos))
            return false;

        EnemyController enemy = pool.Get();

        if (enemy == null)
        {
            Debug.LogError("❌ EnemyPool.Get() devolvió null!");
            return false;
        }

        // 🔥 CRÍTICO: Configurar posición y rotación
        enemy.transform.position = pos;
        enemy.transform.rotation = Quaternion.identity;

        // 🔥 CRÍTICO: Activar el GameObject
        enemy.gameObject.SetActive(true);

        // 🔥 CRÍTICO: Establecer LOD a Active
        enemy.SetLOD(EnemyLOD.Active);

        // 🔥 CRÍTICO: Asegurar que el controller esté enabled
        enemy.enabled = true;

        // 🔥 CRÍTICO: Asegurar que el Motor esté enabled
        //if (enemy.Movement != null)
        //    enemy.Movement.SetEnabled(true);

        // 🔥 CRÍTICO: Asegurar que Perception esté enabled
        if (enemy.Perception != null)
            enemy.Perception.enabled = true;

        // Tracking
        aliveEnemies.Add(enemy);
        aliveTimeByEnemy[enemy] = 0f;

        return true;
    }

    bool TryGetValidSpawnPosition(out Vector3 pos)
    {
        if (area == null || area.player == null)
        {
            Debug.LogError("❌ EnemySpawnerArea o player es null!");
            pos = Vector3.zero;
            return false;
        }

        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPoint = GetRandomPointInArea();
            randomPoint.y += rayHeight;

            if (Physics.Raycast(
                randomPoint,
                Vector3.down,
                out RaycastHit hit,
                rayHeight * 2f,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                // Muy cerca del jugador
                float distSqr = (hit.point - area.player.position).sqrMagnitude;
                if (distSqr < minSpawnDistanceFromPlayer * minSpawnDistanceFromPlayer)
                    continue;

                pos = hit.point;
                return true;
            }
        }

        pos = Vector3.zero;
        return false;
    }

    Vector3 GetRandomPointInArea()
    {
        Vector2 half = area.areaSize * 0.5f;

        float x = Random.Range(-half.x, half.x);
        float z = Random.Range(-half.y, half.y);

        return area.transform.position + new Vector3(x, 0f, z);
    }

    // ═══════ TRACKING ═══════

    void UpdateAliveTimes()
    {
        foreach (var enemy in aliveEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeSelf)
                continue;

            if (aliveTimeByEnemy.ContainsKey(enemy))
                aliveTimeByEnemy[enemy] += Time.deltaTime;
        }
    }

    void CleanupDeadOrInactive()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = aliveEnemies[i];

            if (enemy == null || !enemy.gameObject.activeSelf)
            {
                aliveEnemies.RemoveAt(i);
                aliveTimeByEnemy.Remove(enemy);
            }
        }
    }

    // ═══════ DESPAWN ═══════

    void HandleDespawnByRules()
    {
        if (EnemyManager.Instance == null || area.player == null)
            return;

        float sleepDist = EnemyManager.Instance.sleepDistance;

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = aliveEnemies[i];

            if (enemy == null || !enemy.gameObject.activeSelf)
            {
                aliveEnemies.RemoveAt(i);
                aliveTimeByEnemy.Remove(enemy);
                continue;
            }

            // No despawnear Active
            if (enemy.CurrentLOD == EnemyLOD.Active)
                continue;

            float aliveTime = aliveTimeByEnemy.TryGetValue(enemy, out float t) ? t : 0f;
            float distSqr = (enemy.transform.position - area.player.position).sqrMagnitude;

            bool tooFar = enableDistanceDespawn && distSqr > sleepDist * sleepDist;
            bool tooOld = enableLifetimeDespawn && aliveTime >= maxLifetime;

            if ((enemy.CurrentLOD == EnemyLOD.SemiActive || enemy.CurrentLOD == EnemyLOD.Sleep)
                && (tooFar || tooOld))
            {
                pool.Release(enemy);
                aliveEnemies.RemoveAt(i);
                aliveTimeByEnemy.Remove(enemy);
            }
        }
    }

    // ═══════ RESPAWN ═══════

    void MaintainPopulation()
    {
        if (!maintainPopulation)
            return;

        int missing = initialCount - aliveEnemies.Count;
        if (missing <= 0)
            return;

        int spawnedThisFrame = 0;
        int attempts = 0;
        int maxAttempts = initialCount * 5;

        while (missing > 0 &&
               spawnedThisFrame < maxSpawnPerFrame &&
               attempts < maxAttempts)
        {
            if (TrySpawnOne())
            {
                missing--;
                spawnedThisFrame++;
            }

            attempts++;
        }
    }

    // ═══════ API PÚBLICA ═══════

    public void NotifyEnemyDied(EnemyController enemy)
    {
        aliveEnemies.Remove(enemy);
        aliveTimeByEnemy.Remove(enemy);
    }
}