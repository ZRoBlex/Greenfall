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

    List<EnemyController> aliveEnemies = new List<EnemyController>();

    void Start()
    {
        SpawnAll();
    }

    void Update()
    {
        HandleDespawnByDistance();
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
    }
}
