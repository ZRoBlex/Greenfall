using UnityEngine;
using System.Collections.Generic;

public class EnemySpawnManager : MonoBehaviour
{
    public EnemySpawnerArea area;
    public EnemyPool pool;

    [Header("Spawn Settings")]
    public int initialCount = 15;
    public float minSpawnDistanceFromPlayer = 10f;

    [Header("Surface Detection")]
    public LayerMask groundMask;
    public float rayHeight = 150f;

    [Header("Despawn Rules")]
    public float maxLifetime = 180f; // ⏱️ segundos antes de permitir despawn
    public bool enableLifetimeDespawn = true;
    public bool enableDistanceDespawn = true;

    Dictionary<EnemyController, float> aliveTimeByEnemy =
    new Dictionary<EnemyController, float>();

    [Header("Respawn Control")]
    public bool maintainPopulation = true;
    public int maxSpawnPerFrame = 2; // para no meter picos de CPU



    List<EnemyController> aliveEnemies = new List<EnemyController>();

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



    // 🔹 SPAWN MASIVO
    void SpawnAll()
    {
        aliveEnemies.Clear();

        int spawned = 0;
        int safety = 0;

        while (spawned < initialCount && safety < initialCount * 20)
        {
            if (TrySpawnOne())
            {
                spawned++;
            }

            safety++;
        }

        if (spawned < initialCount)
        {
            Debug.LogWarning(
                $"⚠️ Solo se pudieron spawnear {spawned}/{initialCount} enemigos. " +
                $"Revisa área, groundMask o distancias."
            );
        }
    }


    bool TrySpawnOne()
    {
        Vector3 pos;
        if (!TryGetValidSpawnPosition(out pos))
            return false;

        EnemyController e = pool.Get();
        e.transform.position = pos;
        e.transform.rotation = Quaternion.identity;
        e.gameObject.SetActive(true);

        aliveEnemies.Add(e);
        aliveTimeByEnemy[e] = 0f; // ⏱️ empieza su vida

        return true;
    }


    // 🔹 DESPAWN SOLO SI SALE DEL ÁREA
    void HandleDespawnByDistance()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            var e = aliveEnemies[i];
            if (e == null || !e.gameObject.activeSelf)
            {
                aliveEnemies.RemoveAt(i);
                continue;
            }

            if (!area.IsInsideArea(e.transform.position))
            {
                pool.Release(e);
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    // 🔹 POSICIÓN VÁLIDA SOBRE SUPERFICIE
    bool TryGetValidSpawnPosition(out Vector3 pos)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 random = GetRandomPointInArea();
            random.y += rayHeight;

            if (Physics.Raycast(random, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundMask))
            {
                if (Vector3.Distance(hit.point, area.player.position) < minSpawnDistanceFromPlayer)
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

    // 🔹 Para que el pool notifique muertes
    public void NotifyEnemyDied(EnemyController e)
    {
        aliveEnemies.Remove(e);
        aliveTimeByEnemy.Remove(e);
    }


    void UpdateAliveTimes()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            var e = aliveEnemies[i];
            if (e == null || !e.gameObject.activeSelf)
                continue;

            aliveTimeByEnemy[e] += Time.deltaTime;
        }
    }

    void HandleDespawnByRules()
    {
        if (EnemyManager.Instance == null)
            return;

        float sleepDist = EnemyManager.Instance.sleepDistance;

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            var e = aliveEnemies[i];

            if (e == null || !e.gameObject.activeSelf)
            {
                aliveEnemies.RemoveAt(i);
                aliveTimeByEnemy.Remove(e);
                continue;
            }

            // 🔒 Regla 1: jamás despawnear si está Active
            if (e.CurrentLOD == EnemyLOD.Active)
                continue;

            float aliveTime = aliveTimeByEnemy.TryGetValue(e, out var t) ? t : 0f;

            float distToPlayer = Vector3.Distance(
                e.transform.position,
                area.player.position
            );

            bool tooFar = enableDistanceDespawn && distToPlayer > sleepDist;
            bool tooOld = enableLifetimeDespawn && aliveTime >= maxLifetime;

            // 🔥 Regla 2:
            // solo despawnear si:
            // - está en SemiActive o Sleep
            // - y cumple distancia O tiempo
            if ((e.CurrentLOD == EnemyLOD.SemiActive || e.CurrentLOD == EnemyLOD.Sleep)
                && (tooFar || tooOld))
            {
                pool.Release(e);
                aliveEnemies.RemoveAt(i);
                aliveTimeByEnemy.Remove(e);
            }
        }
    }

    void CleanupDeadOrInactive()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            var e = aliveEnemies[i];

            if (e == null || !e.gameObject.activeSelf)
            {
                aliveEnemies.RemoveAt(i);
                aliveTimeByEnemy.Remove(e);
            }
        }
    }

    void MaintainPopulation()
    {
        if (!maintainPopulation)
            return;

        int missing = initialCount - aliveEnemies.Count;
        if (missing <= 0)
            return;

        int spawnedThisFrame = 0;
        int safety = 0;

        while (missing > 0 &&
               spawnedThisFrame < maxSpawnPerFrame &&
               safety < initialCount * 5)
        {
            if (TrySpawnOne())
            {
                missing--;
                spawnedThisFrame++;
            }

            safety++;
        }
    }

}
