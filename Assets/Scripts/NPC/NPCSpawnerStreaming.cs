using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSpawnerStreaming : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Spawn Settings")]
    public List<NPCTypeProbability> npcTypes = new List<NPCTypeProbability>();

    public int maxNPCs = 15;

    public float spawnRadius = 25f;
    public float minDistanceFromPlayer = 8f;
    public float despawnDistance = 60f;

    public float respawnDelay = 3f;
    public float checkInterval = 2f;

    private List<GameObject> activeNPCs = new List<GameObject>();

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
            Debug.LogError("[NPCSpawnerStreaming] No se encontró Player con tag Player");
    }

    void Start()
    {
        StartCoroutine(SpawnerLoop());
    }

    IEnumerator SpawnerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            DespawnFarNPCs();
            MaintainPopulation();
        }
    }

    void MaintainPopulation()
    {
        while (activeNPCs.Count < maxNPCs)
        {
            SpawnNPC();
        }
    }

    void SpawnNPC()
    {
        GameObject prefab = GetRandomNPCPrefab();
        Vector3 pos = GetRandomPositionAroundPlayer();

        GameObject npc = Instantiate(prefab, pos, Quaternion.identity);
        activeNPCs.Add(npc);

        NPCHealth health = npc.GetComponent<NPCHealth>();
        if (health != null)
        {
            health.OnDeath += () =>
            {
                activeNPCs.Remove(npc);
                Destroy(npc);
            };
        }
    }

    void DespawnFarNPCs()
    {
        for (int i = activeNPCs.Count - 1; i >= 0; i--)
        {
            if (activeNPCs[i] == null)
            {
                activeNPCs.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(player.position, activeNPCs[i].transform.position);

            if (dist > despawnDistance)
            {
                Destroy(activeNPCs[i]);
                activeNPCs.RemoveAt(i);
            }
        }
    }

    Vector3 GetRandomPositionAroundPlayer()
    {
        Vector3 pos;
        int tries = 0;

        do
        {
            Vector2 circle = Random.insideUnitCircle.normalized *
                             Random.Range(minDistanceFromPlayer, spawnRadius);

            pos = player.position + new Vector3(circle.x, 0f, circle.y);

            tries++;
            if (tries > 25) break;

        } while (!IsValidSpawnPoint(pos));

        return pos;
    }

    bool IsValidSpawnPoint(Vector3 pos)
    {
        float radius = 0.6f;
        return !Physics.CheckSphere(pos, radius);
    }

    GameObject GetRandomNPCPrefab()
    {
        float total = 0f;
        foreach (var e in npcTypes)
            total += e.weight;

        float roll = Random.Range(0f, total);
        float sum = 0f;

        foreach (var e in npcTypes)
        {
            sum += e.weight;
            if (roll <= sum)
                return e.npcPrefab;
        }

        return npcTypes[0].npcPrefab;
    }
}
