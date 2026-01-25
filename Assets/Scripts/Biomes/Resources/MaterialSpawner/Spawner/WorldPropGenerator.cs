using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPropGenerator : MonoBehaviour
{
    [Header("Seed Settings")]
    public bool useSeed = true;
    public int seed = 12345;

    [Tooltip("Si está activo, genera una seed nueva cada vez que llamas GenerateAll()")]
    public bool randomizeSeedOnGenerate = false;

    [Header("Noise Settings (future biomes 👀)")]
    public float noiseScale = 0.08f;
    public Vector2 noiseOffset;

    [Header("Area")]
    public Vector2 areaSize = new Vector2(200f, 200f);

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayHeight = 50f;
    public float maxRayDistance = 100f;

    [Header("Slope Limit")]
    public float maxSlopeAngle = 35f;

    [Header("Forbidden Spawn")]
    public List<string> forbiddenTags = new List<string>() { "Building", "NoSpawn" };
    public float forbiddenCheckRadius = 1.2f;

    [Header("Biome System")]
    public BiomeMap biomeMap;

    [Header("Props by SO")]
    public List<WorldPropSO> treeSOs = new List<WorldPropSO>();
    public List<WorldPropSO> rockSOs = new List<WorldPropSO>();

    [Header("Legacy Prefabs (optional)")]
    public List<GameObject> treePrefabs = new List<GameObject>();
    public List<GameObject> rockPrefabs = new List<GameObject>();
    public int treeCount = 150;
    public int rockCount = 100;
    public float minTreeSpacing = 3f;
    public float minRockSpacing = 2f;

    [Header("Chunked Trees")]
    public Transform player;            // referencia al jugador
    public int chunkSize = 32;          // tamaño de cada chunk
    public int renderDistance = 2;      // chunks alrededor del jugador
    public float treeMinHeight = 0.8f;  // altura mínima aleatoria
    public float treeMaxHeight = 1.3f;  // altura máxima aleatoria

    [Header("Chunk Manager")]
    public TreeChunkManager treeChunkManager;


    private Dictionary<Vector2Int, TreeChunk> treeChunks = new();


    Dictionary<BiomeDefinition, float> biomeAreaKm2 = new Dictionary<BiomeDefinition, float>();
    List<Vector3> spawnedPositions = new List<Vector3>();
    System.Random prng;

    void Start()
{
    if (transform.childCount == 0)
        GenerateAll();
    else if (treeChunkManager != null)
        treeChunkManager.InitializeFromParent(transform); // agrupa los árboles existentes
}

    void Update()
    {
        UpdateTreeChunks();
    }

    // ===== PUBLIC API =====
    public void GenerateAll()
    {
        InitSeed();
        Clear();

        if (biomeMap == null)
        {
            Debug.LogError("WorldPropGenerator: No BiomeMap assigned.");
            return;
        }

        CalculateBiomeAreas();
        CalculateBiomeTargets();
        GenerateByBiome();

        Debug.Log($"WorldPropGenerator: Generated {spawnedPositions.Count} props total.");

        // ⚡ Inicializar chunk manager en la siguiente frame
        if (treeChunkManager != null)
        {
            StartCoroutine(InitializeChunksNextFrame());
        }
    }

    private IEnumerator InitializeChunksNextFrame()
    {
        yield return null; // espera 1 frame
        treeChunkManager.InitializeFromParent(transform);
    }

    public void GenerateTreesOnly()
    {
        InitSeed();
        GenerateTrees(true);
    }

    public void GenerateRocksOnly()
    {
        InitSeed();
        GenerateRocks(true);
    }

    public void Clear()
    {
        spawnedPositions.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    // ===== INTERNAL =====
    void InitSeed()
    {
        if (useSeed)
        {
            if (randomizeSeedOnGenerate)
            {
                seed = Random.Range(int.MinValue, int.MaxValue);
            }

            prng = new System.Random(seed);
        }
        else
        {
            prng = new System.Random();
        }
    }

    void GenerateTrees(bool keepExisting)
    {
        if (!keepExisting)
            spawnedPositions.Clear();

        if (treeSOs.Count == 0)
        {
            Debug.LogWarning("WorldPropGenerator: No tree SOs assigned.");
            return;
        }

        SpawnFromSOList(treeSOs, treeCount);
        Debug.Log("WorldPropGenerator: Trees generated.");
    }

    void GenerateRocks(bool keepExisting)
    {
        if (rockSOs.Count == 0)
        {
            Debug.LogWarning("WorldPropGenerator: No rock SOs assigned.");
            return;
        }

        SpawnFromSOList(rockSOs, rockCount);
        Debug.Log("WorldPropGenerator: Rocks generated.");
    }

    void SpawnFromSOList(List<WorldPropSO> props, int count)
    {
        int attempts = 0;
        int maxAttempts = count * 20;

        while (count > 0 && attempts < maxAttempts)
        {
            attempts++;

            float rx = NextFloat(-0.5f, 0.5f) * areaSize.x;
            float rz = NextFloat(-0.5f, 0.5f) * areaSize.y;
            Vector3 randomPos = transform.position + new Vector3(rx, rayHeight, rz);

            if (!Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
                continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            Vector3 spawnPos = hit.point;

            if (!IsFarEnough(spawnPos, 1f)) continue; // opcional, spacing mínimo general
            if (IsInForbiddenZone(spawnPos)) continue;

            WorldPropSO prop = props[prng.Next(0, props.Count)];

            // Probabilidad por SO
            if (Random.value > prop.spawnChance) continue;

            GameObject obj = Instantiate(prop.prefab, spawnPos, Quaternion.Euler(0, NextFloat(0f, 360f), 0), transform);
            if (prop.alignToGround)
                obj.transform.up = hit.normal;

            var node = obj.GetComponent<ResourceNode>();
            if (node != null && prop.resourceData != null)
                node.data = prop.resourceData;

            spawnedPositions.Add(spawnPos);
            count--;
        }
    }

    void GenerateByBiome()
    {
        int attempts = 0;
        int maxAttempts = 100000;

        while (attempts < maxAttempts)
        {
            attempts++;

            float rx = NextFloat(-0.5f, 0.5f) * areaSize.x;
            float rz = NextFloat(-0.5f, 0.5f) * areaSize.y;
            Vector3 randomPos = transform.position + new Vector3(rx, rayHeight, rz);

            if (!Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
                continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            Vector3 spawnPos = hit.point;
            if (IsInForbiddenZone(spawnPos)) continue;

            var biomeDef = biomeMap.GetBiomeDefinition(spawnPos);
            if (biomeDef == null) continue;

            TrySpawnFromBiome(biomeDef, spawnPos, hit.normal);

            if (AllBiomeTargetsReached()) break;
        }
    }

    void TrySpawnFromBiome(BiomeDefinition biome, Vector3 pos, Vector3 normal)
    {
        foreach (var entry in biome.props)
        {
            if (entry.spawnedCount >= entry.targetCount)
                continue;

            WorldPropSO prop = entry.prop;
            if (prop == null || prop.prefab == null) continue;
            if (Random.value > prop.spawnChance) continue;
            if (!IsFarEnough(pos, prop.minSpacing)) continue;
            if (!IsPropAllowedInBiome(prop, biome.biomeType)) continue;

            Quaternion rot = Quaternion.Euler(0, NextFloat(0f, 360f), 0);
            GameObject obj = Instantiate(prop.prefab, pos, rot, transform);
            if (prop.alignToGround)
                obj.transform.up = normal;

            var node = obj.GetComponent<ResourceNode>();
            if (node != null && prop.resourceData != null)
                node.data = prop.resourceData;

            spawnedPositions.Add(pos);
            entry.spawnedCount++;
            break; // solo 1 prop por punto
        }
    }

    bool IsFarEnough(Vector3 pos, float minSpacing)
    {
        foreach (var p in spawnedPositions)
        {
            if (Vector3.Distance(p, pos) < minSpacing)
                return false;
        }
        return true;
    }

    bool IsInForbiddenZone(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, forbiddenCheckRadius);
        foreach (var col in hits)
            foreach (var tag in forbiddenTags)
                if (col.CompareTag(tag)) return true;
        return false;
    }

    float NextFloat(float min, float max)
    {
        return (float)(prng.NextDouble() * (max - min) + min);
    }

    bool IsPropAllowedInBiome(WorldPropSO prop, BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Plains => prop.allowedInPlains,
            BiomeType.Forest => prop.allowedInForest,
            BiomeType.Snow => prop.allowedInSnow,
            BiomeType.Desert => prop.allowedInDesert,
            _ => true
        };
    }

    bool AllBiomeTargetsReached()
    {
        if (biomeMap == null) return true;

        foreach (var def in biomeMap.allBiomeDefinitions)
            foreach (var prop in def.props)
                if (prop.spawnedCount < prop.targetCount)
                    return false;

        return true;
    }

    void CalculateBiomeAreas()
    {
        biomeAreaKm2.Clear();
        float areaWidth = areaSize.x;
        float areaHeight = areaSize.y;
        float sampleStep = 5f;

        int samplesX = Mathf.CeilToInt(areaWidth / sampleStep);
        int samplesZ = Mathf.CeilToInt(areaHeight / sampleStep);

        for (int x = 0; x < samplesX; x++)
        {
            for (int z = 0; z < samplesZ; z++)
            {
                float wx = transform.position.x - areaWidth * 0.5f + x * sampleStep;
                float wz = transform.position.z - areaHeight * 0.5f + z * sampleStep;

                Vector3 pos = new Vector3(wx, 0f, wz);
                var biomeDef = biomeMap.GetBiomeDefinition(pos);
                if (biomeDef == null) continue;

                if (!biomeAreaKm2.ContainsKey(biomeDef))
                    biomeAreaKm2[biomeDef] = 0f;

                biomeAreaKm2[biomeDef] += sampleStep * sampleStep;
            }
        }

        var keys = new List<BiomeDefinition>(biomeAreaKm2.Keys);
        foreach (var k in keys)
            biomeAreaKm2[k] /= 1_000_000f; // m² → km²
    }

    void CalculateBiomeTargets()
    {
        foreach (var def in biomeMap.allBiomeDefinitions)
        {
            float biomeKm2 = biomeAreaKm2.ContainsKey(def) ? biomeAreaKm2[def] : 0f;
            foreach (var entry in def.props)
            {
                entry.spawnedCount = 0;
                entry.targetCount = Mathf.RoundToInt(entry.prop.densityPerKm2 * biomeKm2);
            }
        }
    }

    [System.Serializable]
    public class TreeChunk
    {
        public GameObject chunkGO;
        public List<GameObject> trees = new();

        public void Generate(Vector2Int chunkPos, int chunkSize, int treesCount, List<WorldPropSO> treeSOs, System.Random prng, float minH, float maxH, Transform parent, BiomeMap biomeMap)
        {
            chunkGO = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
            chunkGO.transform.parent = parent;

            for (int i = 0; i < treesCount; i++)
            {
                float rx = (float)prng.NextDouble() * chunkSize;
                float rz = (float)prng.NextDouble() * chunkSize;

                Vector3 pos = new Vector3(chunkPos.x * chunkSize + rx, 0f, chunkPos.y * chunkSize + rz);

                // Chequear altura usando raycast
                if (!Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, parent.GetComponent<WorldPropGenerator>().groundLayer))
                    continue;

                pos.y = hit.point.y;

                // Obtener bioma
                BiomeDefinition biome = biomeMap.GetBiomeDefinition(pos);
                if (biome == null) continue;

                // Filtrar árboles permitidos en ese bioma
                List<WorldPropSO> allowedTrees = new List<WorldPropSO>();
                foreach (var t in treeSOs)
                {
                    if ((biome.biomeType == BiomeType.Forest && t.allowedInForest) ||
                        (biome.biomeType == BiomeType.Plains && t.allowedInPlains) ||
                        (biome.biomeType == BiomeType.Desert && t.allowedInDesert) ||
                        (biome.biomeType == BiomeType.Snow && t.allowedInSnow))
                        allowedTrees.Add(t);
                }

                if (allowedTrees.Count == 0) continue;

                WorldPropSO treeSO = allowedTrees[prng.Next(0, allowedTrees.Count)];
                if (treeSO.prefab == null) continue;

                GameObject tree = GameObject.Instantiate(treeSO.prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), chunkGO.transform);
                float scale = Random.Range(minH, maxH);
                tree.transform.localScale *= scale;

                trees.Add(tree);
            }
        }

        public void SetActive(bool active)
        {
            if (chunkGO != null)
                chunkGO.SetActive(active);
        }
    }

    void UpdateTreeChunks()
    {
        if (player == null || treeSOs.Count == 0 || biomeMap == null)
            return;

        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        // Crear/activar chunks cercanos
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int c = playerChunk + new Vector2Int(x, z);
                if (!treeChunks.ContainsKey(c))
                {
                    TreeChunk chunk = new TreeChunk();
                    chunk.Generate(c, chunkSize, 50, treeSOs, prng, treeMinHeight, treeMaxHeight, transform, biomeMap);
                    treeChunks[c] = chunk;
                }
                treeChunks[c].SetActive(true);
            }
        }

        // Desactivar chunks lejanos
        foreach (var kvp in treeChunks)
        {
            Vector2Int c = kvp.Key;
            if (Mathf.Abs(c.x - playerChunk.x) > renderDistance || Mathf.Abs(c.y - playerChunk.y) > renderDistance)
            {
                kvp.Value.SetActive(false);
            }
        }
    }


    public void RandomizeSeed()
    {
        seed = Random.Range(int.MinValue, int.MaxValue);
        Debug.Log($"WorldPropGenerator: New random seed = {seed}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, 1f, areaSize.y));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, forbiddenCheckRadius);
    }
#endif
}
