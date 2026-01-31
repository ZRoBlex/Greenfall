using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // referencia al jugador

    [Header("Spawn Settings")]
    public List<NPCTypeProbability> npcTypes = new List<NPCTypeProbability>();
    public int maxNPCs = 10;
    public float spawnRadius = 20f;
    public float minDistanceFromPlayer = 8f;

    public bool respawnOnDeath = true;
    public float respawnDelay = 5f;

    private List<GameObject> spawnedNPCs = new List<GameObject>();

    [System.Serializable]
    public class NPCTypeProbability
    {
        public GameObject npcPrefab;
        [Range(0f, 100f)]
        public float weight = 50f;
    }

    void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogError("[NPCSpawner] No se encontró Player con tag Player");
    }

    void Start()
    {
        for (int i = 0; i < maxNPCs; i++)
            SpawnNPC();
    }

    void SpawnNPC()
    {
        if (spawnedNPCs.Count >= maxNPCs || player == null)
            return;

        GameObject prefab = GetRandomNPCPrefab();
        Vector3 spawnPos = GetRandomPositionAroundPlayer();

        GameObject npcGO = Instantiate(prefab, spawnPos, Quaternion.identity);
        spawnedNPCs.Add(npcGO);

        NPCHealth health = npcGO.GetComponent<NPCHealth>();
        if (health != null && respawnOnDeath)
        {
            health.OnDeath += () =>
            {
                StartCoroutine(RespawnNPC(respawnDelay, npcGO));
            };
        }
    }

    Vector3 GetRandomPositionAroundPlayer()
    {
        Vector3 pos;
        int tries = 0;

        do
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(minDistanceFromPlayer, spawnRadius);
            pos = player.position + new Vector3(circle.x, 0f, circle.y);

            tries++;
            if (tries > 20) break;

        } while (!IsValidSpawnPoint(pos));

        return pos;
    }

    bool IsValidSpawnPoint(Vector3 pos)
    {
        // Evita spawnear dentro de obstáculos
        float radius = 0.5f;
        return !Physics.CheckSphere(pos, radius);
    }

    GameObject GetRandomNPCPrefab()
    {
        float totalWeight = 0f;
        foreach (var entry in npcTypes)
            totalWeight += entry.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in npcTypes)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry.npcPrefab;
        }

        return npcTypes[0].npcPrefab;
    }

    IEnumerator RespawnNPC(float delay, GameObject deadNPC)
    {
        spawnedNPCs.Remove(deadNPC);
        yield return new WaitForSeconds(delay);
        SpawnNPC();
    }
}
