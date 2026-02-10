using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawner simple ULTRA optimizado
/// Para spawns puntuales y controlados
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("═══════ PLAYER ═══════")]
    [SerializeField] Transform player;

    [Header("═══════ ÁREA DE SPAWN ═══════")]
    [Range(10f, 100f)]
    public float minSpawnRadius = 20f;

    [Range(20f, 150f)]
    public float maxSpawnRadius = 60f;

    [Header("═══════ CONFIGURACIÓN ═══════")]
    [Range(1, 50)]
    public int maxAliveEnemies = 20;

    [Range(0.5f, 10f)]
    public float spawnInterval = 3f;

    [Header("═══════ TERRENO ═══════")]
    public float raycastHeight = 100f;
    public float raycastDistance = 200f;
    public LayerMask groundMask;

    [Header("═══════ SUPERFICIES PROHIBIDAS ═══════")]
    public string[] forbiddenTags;

    [Header("═══════ REINTENTOS ═══════")]
    [Range(5, 30)]
    public int maxPositionTries = 15;

    [Range(2f, 10f)]
    public float nearbySearchRadius = 5f;

    // ═══════ ESTADO ═══════

    float timer;
    readonly List<EnemyController> alive = new List<EnemyController>();

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (player == null) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            TrySpawn();
            timer = spawnInterval;
        }
    }

    // ═══════ SPAWNING ═══════

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

            Vector3 candidate = player.position + new Vector3(randCircle.x, 0f, randCircle.y);
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
        if (forbiddenTags == null || forbiddenTags.Length == 0)
            return false;

        foreach (string tag in forbiddenTags)
        {
            if (!string.IsNullOrEmpty(tag) && col.CompareTag(tag))
                return true;
        }

        return false;
    }

    // ═══════ API PÚBLICA ═══════

    public void NotifyEnemyDespawned(EnemyController enemy)
    {
        alive.Remove(enemy);
    }
}