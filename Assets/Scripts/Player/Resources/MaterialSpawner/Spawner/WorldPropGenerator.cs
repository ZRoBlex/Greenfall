using UnityEngine;
using System.Collections.Generic;

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

    // 🌲 Árboles
    [Header("Trees")]
    public List<GameObject> treePrefabs = new List<GameObject>();
    public int treeCount = 150;
    public float minTreeSpacing = 3f;

    // 🪨 Rocas
    [Header("Rocks")]
    public List<GameObject> rockPrefabs = new List<GameObject>();
    public int rockCount = 100;
    public float minRockSpacing = 2f;

    [Header("Biome System")]
    public BiomeMap biomeMap;



    List<Vector3> spawnedPositions = new List<Vector3>();
    System.Random prng;

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

        ResetBiomePropCounters();   // ✅ NUEVO

        GenerateByBiome();

        Debug.Log($"WorldPropGenerator: Generated {spawnedPositions.Count} props total.");
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

        if (treePrefabs.Count == 0)
        {
            Debug.LogWarning("WorldPropGenerator: No tree prefabs assigned.");
            return;
        }

        SpawnFromList(treePrefabs, treeCount, minTreeSpacing);
        Debug.Log("WorldPropGenerator: Trees generated.");
    }

    void GenerateRocks(bool keepExisting)
    {
        if (rockPrefabs.Count == 0)
        {
            Debug.LogWarning("WorldPropGenerator: No rock prefabs assigned.");
            return;
        }

        SpawnFromList(rockPrefabs, rockCount, minRockSpacing);
        Debug.Log("WorldPropGenerator: Rocks generated.");
    }

    void SpawnFromList(List<GameObject> prefabs, int count, float minSpacing)
    {
        int attempts = 0;
        int maxAttempts = count * 20;

        while (count > 0 && attempts < maxAttempts)
        {
            attempts++;

            float rx = NextFloat(-0.5f, 0.5f) * areaSize.x;
            float rz = NextFloat(-0.5f, 0.5f) * areaSize.y;

            Vector3 randomPos = transform.position + new Vector3(
                rx,
                rayHeight,
                rz
            );

            if (Physics.Raycast(
                randomPos,
                Vector3.down,
                out RaycastHit hit,
                maxRayDistance,
                groundLayer
            ))
            {
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > maxSlopeAngle)
                    continue;

                Vector3 spawnPos = hit.point;

                if (!IsFarEnough(spawnPos, minSpacing))
                    continue;

                if (IsInForbiddenZone(spawnPos))
                    continue;

                // 🔮 Noise sample (no depende del tamaño del área)
                float noiseValue = Mathf.PerlinNoise(
                    (spawnPos.x + noiseOffset.x) * noiseScale,
                    (spawnPos.z + noiseOffset.y) * noiseScale
                );

                // (por ahora solo debug / futuro biomas)
                // if (noiseValue < 0.3f) continue;

                GameObject prefab =
                    prefabs[prng.Next(0, prefabs.Count)];

                GameObject obj = Instantiate(
                    prefab,
                    spawnPos,
                    Quaternion.Euler(0, NextFloat(0f, 360f), 0),
                    transform
                );

                obj.transform.up = hit.normal;

                spawnedPositions.Add(spawnPos);
                count--;

                BiomeType biome = biomeMap != null
    ? biomeMap.GetBiome(spawnPos)
    : BiomeType.Plains;

                // DEBUG (por ahora no bloquea nada)
                if (biome == BiomeType.Desert && prefabs == treePrefabs)
                    continue; // ejemplo: no árboles en desierto

            }
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
        {
            foreach (var tag in forbiddenTags)
            {
                if (col.CompareTag(tag))
                    return true;
            }
        }

        return false;
    }

    float NextFloat(float min, float max)
    {
        return (float)(prng.NextDouble() * (max - min) + min);
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

            Vector3 randomPos = transform.position + new Vector3(
                rx,
                rayHeight,
                rz
            );

            if (!Physics.Raycast(
                randomPos,
                Vector3.down,
                out RaycastHit hit,
                maxRayDistance,
                groundLayer
            ))
                continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle)
                continue;

            Vector3 spawnPos = hit.point;

            if (IsInForbiddenZone(spawnPos))
                continue;

            var biomeDef = biomeMap.GetBiomeDefinition(spawnPos);
            if (biomeDef == null)
                continue;

            TrySpawnFromBiome(biomeDef, spawnPos, hit.normal);

            // 🛑 corte temprano
            if (AllBiomeTargetsReached())
                break;
        }
    }

    void TrySpawnFromBiome(BiomeDefinition biome, Vector3 pos, Vector3 normal)
    {
        foreach (var entry in biome.props)
        {
            // 🚫 ya llenó su cuota
            if (entry.spawnedCount >= entry.targetCount)
                continue;

            // 🎲 probabilidad
            if (Random.value > entry.spawnChance)
                continue;

            // 📏 espaciado
            if (!IsFarEnough(pos, entry.minSpacing))
                continue;

            if (entry.prefabs.Count == 0)
                continue;

            GameObject prefab =
                entry.prefabs[prng.Next(0, entry.prefabs.Count)];

            Quaternion rot = Quaternion.Euler(0, NextFloat(0f, 360f), 0);

            GameObject obj = Instantiate(prefab, pos, rot, transform);

            if (entry.alignToGround)
                obj.transform.up = normal;

            spawnedPositions.Add(pos);

            entry.spawnedCount++;   // ✅ CONSUME targetCount

            break; // ⛔ solo 1 prop por punto
        }
    }



#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(areaSize.x, 1f, areaSize.y)
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, forbiddenCheckRadius);
    }
#endif


    public void RandomizeSeed()
    {
        seed = Random.Range(int.MinValue, int.MaxValue);
        Debug.Log($"WorldPropGenerator: New random seed = {seed}");
    }

    void ResetBiomePropCounters()
    {
        if (biomeMap == null)
            return;

        foreach (var def in biomeMap.allBiomeDefinitions)
        {
            foreach (var prop in def.props)
            {
                prop.spawnedCount = 0;
            }
        }
    }


    bool AllBiomeTargetsReached()
    {
        if (biomeMap == null)
            return true;

        foreach (var def in biomeMap.allBiomeDefinitions)
        {
            foreach (var prop in def.props)
            {
                if (prop.spawnedCount < prop.targetCount)
                    return false;
            }
        }

        return true;
    }



}
