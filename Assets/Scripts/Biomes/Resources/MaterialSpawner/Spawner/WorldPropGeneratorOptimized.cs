using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPropGeneratorOptimized : MonoBehaviour
{
    [Header("Seed Settings")]
    public bool useSeed = true;
    public int seed = 12345;
    public bool randomizeSeedOnGenerate = false;

    [Header("Area & Biomes")]
    public Vector2 areaSize = new Vector2(200f, 200f);
    public BiomeMap biomeMap;

    [Header("Ground")]
    public LayerMask groundLayer;
    public float rayHeight = 50f;
    public float maxRayDistance = 100f;
    public float maxSlopeAngle = 35f;

    [Header("Forbidden Zones")]
    public List<string> forbiddenTags = new List<string>() { "Building", "NoSpawn" };
    public float forbiddenCheckRadius = 1.2f;

    [Header("Props Settings")]
    public List<WorldPropSO> treeSOs = new();
    public List<WorldPropSO> rockSOs = new();
    public int maxPropsPerChunk = 50;

    [Header("Chunk Settings")]
    public Transform player;
    public int chunkSize = 32;
    public int renderDistance = 2;
    public float treeMinHeight = 0.8f;
    public float treeMaxHeight = 1.3f;

    [Header("Pool Settings")]
    public int poolMultiplier = 2; // Número de prefabs por chunk


    private System.Random prng;
    private Dictionary<Vector2Int, PropChunk> chunks = new();
    private Dictionary<GameObject, Queue<GameObject>> propPools = new();

    void Start()
    {
        InitSeed();
        InitializePools();
        GenerateInitialChunks();
    }

    void Update()
    {
        UpdateChunks();
    }

    #region Initialization
    void InitSeed()
    {
        if (useSeed)
        {
            if (randomizeSeedOnGenerate) seed = Random.Range(int.MinValue, int.MaxValue);
            prng = new System.Random(seed);
        }
        else prng = new System.Random();
    }

    void InitializePools()
    {
        // Crear pools para todos los SOs
        foreach (var prop in treeSOs) CreatePool(prop);
        foreach (var prop in rockSOs) CreatePool(prop);
    }

    void CreatePool(WorldPropSO prop)
    {
        if (prop == null || prop.prefab == null || propPools.ContainsKey(prop.prefab)) return;

        Queue<GameObject> pool = new();
        int total = maxPropsPerChunk * poolMultiplier;
        for (int i = 0; i < total; i++)
        {
            GameObject go = Instantiate(prop.prefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
        propPools[prop.prefab] = pool;
    }
    #endregion

    #region Chunk System
    void GenerateInitialChunks()
    {
        if (player == null) return;

        Vector2Int center = GetChunk(player.position);
        for (int x = -renderDistance; x <= renderDistance; x++)
            for (int z = -renderDistance; z <= renderDistance; z++)
                GenerateChunk(center + new Vector2Int(x, z));
    }

    void UpdateChunks()
    {
        if (player == null) return;

        Vector2Int playerChunk = GetChunk(player.position);

        // Activar chunks cercanos y generar los que falten
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int c = playerChunk + new Vector2Int(x, z);
                if (!chunks.ContainsKey(c)) GenerateChunk(c);
                chunks[c].SetActive(true);
            }
        }

        // Desactivar chunks lejanos
        foreach (var kvp in chunks)
        {
            Vector2Int c = kvp.Key;
            if (Mathf.Abs(c.x - playerChunk.x) > renderDistance || Mathf.Abs(c.y - playerChunk.y) > renderDistance)
                kvp.Value.SetActive(false);
        }
    }

    Vector2Int GetChunk(Vector3 pos) => new(
        Mathf.FloorToInt(pos.x / chunkSize),
        Mathf.FloorToInt(pos.z / chunkSize)
    );

    void GenerateChunk(Vector2Int chunkPos)
    {
        PropChunk chunk = new(chunkPos, chunkSize, transform);
        chunks[chunkPos] = chunk;
        StartCoroutine(chunk.SpawnPropsCoroutine(this, prng));
    }
    #endregion

    #region Spawning Helpers
    public GameObject GetPooledProp(WorldPropSO prop)
    {
        if (!propPools.ContainsKey(prop.prefab)) return null;
        if (propPools[prop.prefab].Count == 0) return null;

        GameObject go = propPools[prop.prefab].Dequeue();
        go.SetActive(true);
        return go;
    }

    public void ReturnToPool(GameObject go)
    {
        go.SetActive(false);
        foreach (var kvp in propPools)
            if (kvp.Key == go.gameObject) kvp.Value.Enqueue(go);
    }

    public bool IsPositionValid(Vector3 pos, float minSpacing)
    {
        if (IsInForbiddenZone(pos)) return false;
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
    public bool IsPropAllowedInBiome(WorldPropSO prop, BiomeType biome)
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
    #endregion


    #region Nested PropChunk Class
    public class PropChunk
    {
        public GameObject parentGO;
        public List<GameObject> props = new();
        private Vector2Int chunkPos;
        private int chunkSize;

        public PropChunk(Vector2Int pos, int size, Transform parent)
        {
            chunkPos = pos;
            chunkSize = size;
            parentGO = new GameObject($"Chunk_{pos.x}_{pos.y}");
            parentGO.transform.parent = parent;
        }

        public IEnumerator SpawnPropsCoroutine(WorldPropGeneratorOptimized generator, System.Random prng)
        {
            // Generar árboles y rocas
            for (int i = 0; i < generator.maxPropsPerChunk; i++)
            {
                Vector3 pos = new Vector3(
                    chunkPos.x * chunkSize + (float)prng.NextDouble() * chunkSize,
                    generator.rayHeight,
                    chunkPos.y * chunkSize + (float)prng.NextDouble() * chunkSize
                );

                if (!Physics.Raycast(pos, Vector3.down, out RaycastHit hit, generator.maxRayDistance, generator.groundLayer))
                    continue;

                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > generator.maxSlopeAngle) continue;

                pos.y = hit.point.y;

                // Elegir prop basado en biome
                BiomeDefinition biome = generator.biomeMap.GetBiomeDefinition(pos);
                if (biome == null) continue;

                // Mezclar árboles y rocas en una sola lista
                List<WorldPropSO> candidates = new();
                foreach (var t in generator.treeSOs)
                    if (generator.IsPropAllowedInBiome(t, biome.biomeType)) candidates.Add(t);
                foreach (var r in generator.rockSOs)
                    if (generator.IsPropAllowedInBiome(r, biome.biomeType)) candidates.Add(r);

                if (candidates.Count == 0) continue;

                WorldPropSO selected = candidates[prng.Next(0, candidates.Count)];
                GameObject go = generator.GetPooledProp(selected);
                if (go == null) continue;

                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, (float)prng.NextDouble() * 360f, 0f);
                float scale = Mathf.Lerp(selected.minScale, selected.maxScale, (float)prng.NextDouble());
                go.transform.localScale = Vector3.one * scale;
                go.transform.parent = parentGO.transform;

                props.Add(go);

                if (i % 5 == 0) yield return null; // cede frame cada 5 props
            }
        }

        public void SetActive(bool active)
        {
            parentGO.SetActive(active);
        }
    }
    #endregion
}
