using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Player Reference")]
    [SerializeField] Transform player;

    [Header("Spawn Area (relative to player)")]
    public float minSpawnRadius = 20f;
    public float maxSpawnRadius = 60f;

    [Header("Spawn Settings")]
    public int maxAliveEnemies = 20;
    public float spawnInterval = 3f;

    [Header("Ground Detection")]
    public float raycastHeight = 100f;
    public float raycastDistance = 200f;
    public LayerMask groundMask;

    [Header("Forbidden Surface Tags")]
    public string[] forbiddenTags;

    [Header("Retry")]
    public int maxPositionTries = 15;
    public float nearbySearchRadius = 5f;

    float timer;

    readonly List<EnemyController> alive = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (player == null)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            TrySpawn();
            timer = spawnInterval;
        }
    }

    void TrySpawn()
    {
        CleanupDead();

        if (alive.Count >= maxAliveEnemies)
            return;

        if (!TryFindSpawnPoint(out Vector3 spawnPos))
            return;

        EnemyController enemy = EnemyPool.Instance.Get();
        enemy.transform.position = spawnPos;
        enemy.transform.rotation = Quaternion.identity;

        enemy.gameObject.SetActive(true);
        enemy.SetLOD(EnemyLOD.Active);

        alive.Add(enemy);
    }

    void CleanupDead()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null || !alive[i].gameObject.activeInHierarchy)
            {
                alive.RemoveAt(i);
            }
        }
    }

    bool TryFindSpawnPoint(out Vector3 result)
    {
        for (int i = 0; i < maxPositionTries; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle.normalized *
                                 Random.Range(minSpawnRadius, maxSpawnRadius);

            Vector3 candidate = player.position +
                                new Vector3(randCircle.x, 0f, randCircle.y);

            candidate.y += raycastHeight;

            if (Physics.Raycast(
                candidate,
                Vector3.down,
                out RaycastHit hit,
                raycastDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                if (IsSurfaceForbidden(hit.collider))
                {
                    if (TryFindNearbyValidPoint(hit.point, out result))
                        return true;

                    continue;
                }

                result = hit.point;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    bool TryFindNearbyValidPoint(Vector3 origin, out Vector3 result)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 rand = Random.insideUnitCircle * nearbySearchRadius;
            Vector3 candidate = origin + new Vector3(rand.x, raycastHeight, rand.y);

            if (Physics.Raycast(
                candidate,
                Vector3.down,
                out RaycastHit hit,
                raycastDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                if (!IsSurfaceForbidden(hit.collider))
                {
                    result = hit.point;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    bool IsSurfaceForbidden(Collider col)
    {
        foreach (string tag in forbiddenTags)
        {
            if (!string.IsNullOrEmpty(tag) && col.CompareTag(tag))
                return true;
        }

        return false;
    }

    public void NotifyEnemyDespawned(EnemyController enemy)
    {
        alive.Remove(enemy);
    }
}
