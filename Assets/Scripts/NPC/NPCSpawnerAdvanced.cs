using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSpawnerAdvanced : MonoBehaviour
{
    public Transform player;
    public BiomeMap biomeMap;

    [Header("Population")]
    public int maxNPCs = 20;
    public float spawnRadius = 30f;
    public float minDistanceFromPlayer = 10f;
    public float despawnDistance = 70f;

    public float checkInterval = 2f;

    [Header("NPC Types")]
    public List<NPCBiomeEntry> npcTypes = new List<NPCBiomeEntry>();

    List<GameObject> activeNPCs = new List<GameObject>();
    Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();

    [System.Serializable]
    public class NPCBiomeEntry
    {
        public GameObject prefab;
        public List<BiomeType> allowedBiomes;
        public int minGroup = 1;
        public int maxGroup = 3;
        [Range(0, 100)] public float weight = 50f;
    }

    void Awake()
    {
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Start()
    {
        foreach (var e in npcTypes)
            pool[e.prefab] = new Queue<GameObject>();

        StartCoroutine(SpawnerLoop());
    }

    IEnumerator SpawnerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            DespawnInvalid();
            MaintainPopulation();
        }
    }

    void MaintainPopulation()
    {
        while (activeNPCs.Count < maxNPCs)
            SpawnGroup();
    }

    void SpawnGroup()
    {
        var entry = GetRandomEntry();
        if (entry == null) return;

        Vector3 center = GetRandomPosition();
        var biome = biomeMap.GetBiomeDefinition(center);

        if (biome == null || !entry.allowedBiomes.Contains(biome.biomeType))
            return;

        int count = Random.Range(entry.minGroup, entry.maxGroup + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 2f;
            offset.y = 0;

            GameObject npc = GetFromPool(entry.prefab);
            npc.transform.position = center + offset;
            npc.SetActive(true);

            // 🔹 Guardar info del spawn
            var info = npc.GetComponent<NPCSpawnInfo>();
            if (info == null)
                info = npc.AddComponent<NPCSpawnInfo>();

            info.prefabSource = entry.prefab;
            info.biomeSpawnedIn = biome.biomeType;

            activeNPCs.Add(npc);

            NPCHealth h = npc.GetComponent<NPCHealth>();
            if (h != null)
                h.OnDeath = () => DespawnNPC(npc);
        }
    }

    void DespawnInvalid()
    {
        var playerBiome = biomeMap.GetBiomeDefinition(player.position);

        for (int i = activeNPCs.Count - 1; i >= 0; i--)
        {
            GameObject npc = activeNPCs[i];
            float dist = Vector3.Distance(player.position, npc.transform.position);

            bool wrongBiome = false;

            var info = npc.GetComponent<NPCSpawnInfo>();
            if (info != null && playerBiome != null)
            {
                if (!IsBiomeAllowedForNPC(info.prefabSource, playerBiome.biomeType))
                    wrongBiome = true;
            }

            if (dist > despawnDistance || wrongBiome)
            {
                activeNPCs.RemoveAt(i);
                ReturnToPool(npc);
            }
        }
    }

    Vector3 GetRandomPosition()
    {
        Vector2 circle = Random.insideUnitCircle.normalized *
                         Random.Range(minDistanceFromPlayer, spawnRadius);

        return player.position + new Vector3(circle.x, 0, circle.y);
    }

    NPCBiomeEntry GetRandomEntry()
    {
        float total = 0;
        foreach (var e in npcTypes)
            total += e.weight;

        float roll = Random.Range(0, total);
        float sum = 0;

        foreach (var e in npcTypes)
        {
            sum += e.weight;
            if (roll <= sum)
                return e;
        }

        return npcTypes[0];
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (pool[prefab].Count > 0)
            return pool[prefab].Dequeue();

        return Instantiate(prefab);
    }

    void ReturnToPool(GameObject npc)
    {
        npc.SetActive(false);

        var info = npc.GetComponent<NPCSpawnInfo>();
        if (info != null && pool.ContainsKey(info.prefabSource))
        {
            pool[info.prefabSource].Enqueue(npc);
        }
    }

    void DespawnNPC(GameObject npc)
    {
        activeNPCs.Remove(npc);
        ReturnToPool(npc);
    }

    bool IsBiomeAllowedForNPC(GameObject prefab, BiomeType biome)
    {
        foreach (var e in npcTypes)
        {
            if (e.prefab == prefab)
                return e.allowedBiomes.Contains(biome);
        }
        return false;
    }
}
