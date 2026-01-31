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

    [Header("Ground")]
    public LayerMask groundLayer; // Floor
    public float spawnHeightOffset = 0.5f;


    public float checkInterval = 2f;

    [Header("Group Spacing")]
    public float minDistanceBetweenGroups = 8f;
    List<Vector3> groupCenters = new List<Vector3>();


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
        int safety = 0;

        while (activeNPCs.Count < maxNPCs && safety < 10)
        {
            SpawnGroup();
            safety++;
        }
    }


    void SpawnGroup()
    {
        var entry = GetRandomEntry();
        if (entry == null) return;

        Vector3 center = GetValidGroupCenter();
        if (center == Vector3.zero)
            return;

        var biome = biomeMap.GetBiomeDefinition(center);

        if (biome == null || !entry.allowedBiomes.Contains(biome.biomeType))
            return;

        groupCenters.Add(center);

        int count = Random.Range(entry.minGroup, entry.maxGroup + 1);

        //for (int i = 0; i < count; i++)
        //{
        //    Vector3 offset = Random.insideUnitSphere * 2.5f;
        //    offset.y = 0;

        //    GameObject npc = GetFromPool(entry.prefab);
        //    npc.transform.position = center + offset;
        //    npc.SetActive(true);

        //    var info = npc.GetComponent<NPCSpawnInfo>();
        //    if (info == null)
        //        info = npc.AddComponent<NPCSpawnInfo>();

        //    info.prefabSource = entry.prefab;
        //    info.biomeSpawnedIn = biome.biomeType;

        //    activeNPCs.Add(npc);

        //    NPCHealth h = npc.GetComponent<NPCHealth>();
        //    if (h != null)
        //        h.OnDeath = () => DespawnNPC(npc);
        //}
        GameObject leader = null;
        NPCGroupLeader leaderScript = null;

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 3f;
            offset.y = 0;

            GameObject npc = GetFromPool(entry.prefab);
            npc.transform.position = center + offset;
            npc.SetActive(true);

            if (i == 0)
            {
                leader = npc;
                leaderScript = npc.AddComponent<NPCGroupLeader>();
            }
            else
            {
                var member = npc.AddComponent<NPCGroupMember>();
                member.SetLeader(leader.transform);
                leaderScript.members.Add(member);
            }

            activeNPCs.Add(npc);
        }



    }

    Vector3 GetValidGroupCenter()
    {
        for (int tries = 0; tries < 20; tries++)
        {
            Vector3 pos = GetRandomPosition();

            // buscar suelo
            Ray ray = new Ray(pos + Vector3.up * 50f, Vector3.down);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 100f, groundLayer))
                continue;

            Vector3 groundPos = hit.point + Vector3.up * spawnHeightOffset;

            bool tooClose = false;
            foreach (var c in groupCenters)
            {
                if (Vector3.Distance(groundPos, c) < minDistanceBetweenGroups)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return groundPos;
        }

        return Vector3.zero;
    }



    void DespawnInvalid()
    {
        groupCenters.Clear();
        foreach (var npc in activeNPCs)
        {
            groupCenters.Add(npc.transform.position);
        }

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
