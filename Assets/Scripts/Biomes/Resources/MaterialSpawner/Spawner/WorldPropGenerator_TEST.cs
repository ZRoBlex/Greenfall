using UnityEngine;
using System.Collections.Generic;

public class WorldPropGenerator_TEST : MonoBehaviour
{
    [HideInInspector] public int chunkSize;
    [HideInInspector] public Vector2Int chunkCoord;

    [Header("Seed")]
    public int seed = 12345;
    public bool useSeed = true;

    [Header("Spawn Settings")]
    public int spawnAttemptsPerChunk = 120;
    public float rayHeight = 50f;
    public float maxRayDistance = 100f;
    public LayerMask groundLayer;
    public float maxSlopeAngle = 35f;

    [Header("Biome System")]
    public BiomeMap biomeMap;

    [Header("Forbidden")]
    public float forbiddenRadius = 1.2f;
    public List<string> forbiddenTags = new() { "Building", "NoSpawn" };

    //Dictionary<BiomePropEntry, int> remainingToSpawn = new();
    Dictionary<WorldPropSO, int> remainingToSpawn = new();

    System.Random prng;
    List<Vector3> spawnedPositions = new();

    // ======================= PUBLIC =======================

    public void Generate()
    {
        InitSeed();
        Clear();

        var biome = GetChunkBiome();
        if (biome == null) return;

        foreach (var entry in biome.props)
        {
            if (entry.prop == null) continue;

            int clusterCount = prng.Next(entry.minPerChunk, entry.maxPerChunk + 1);

            for (int i = 0; i < clusterCount; i++)
                SpawnCluster(entry.prop);
        }
    }





    public void Clear()
    {
        spawnedPositions.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    // ======================= CORE =======================

    void TrySpawn()
    {
        float localX = NextFloat(0, chunkSize);
        float localZ = NextFloat(0, chunkSize);

        Vector3 rayOrigin = transform.position + new Vector3(localX, rayHeight, localZ);

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
            return;

        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
            return;

        if (IsInForbiddenZone(hit.point))
            return;

        var biome = biomeMap.GetBiomeDefinition(hit.point);
        if (biome == null || biome.props.Count == 0)
            return;

        var entry = biome.props[prng.Next(0, biome.props.Count)];
        var prop = entry.prop;

        if (prop == null || prop.prefab == null)
            return;

        //if (Random.value > prop.spawnChance)
        //    return;
        if (NextFloat(0f, 1f) > prop.spawnChance)
            return;


        if (!IsFarEnough(hit.point, prop.minSpacing))
            return;

        if (!remainingToSpawn.TryGetValue(prop, out int left))
            return;

        if (left <= 0)
        {
            remainingToSpawn.Remove(prop);
            return;
        }



        Quaternion rot = Quaternion.Euler(0, NextFloat(0, 360), 0);
        GameObject obj = Instantiate(prop.prefab, hit.point, rot, transform);

        if (prop.alignToGround)
            obj.transform.up = hit.normal;

        var node = obj.GetComponent<ResourceNode>();
        if (node && prop.resourceData)
            node.data = prop.resourceData;

        spawnedPositions.Add(hit.point);

        remainingToSpawn[prop] = left - 1;

    }

    // ======================= HELPERS =======================

    void InitSeed()
    {
        int finalSeed = seed;

        if (useSeed)
        {
            finalSeed ^= chunkCoord.x * 73856093;
            finalSeed ^= chunkCoord.y * 19349663;
        }

        prng = new System.Random(finalSeed);
        Random.InitState(finalSeed);
    }

    bool IsFarEnough(Vector3 pos, float spacing)
    {
        foreach (var p in spawnedPositions)
            if (Vector3.Distance(p, pos) < spacing)
                return false;
        return true;
    }

    bool IsInForbiddenZone(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, forbiddenRadius);
        foreach (var col in hits)
            foreach (var tag in forbiddenTags)
                if (col.CompareTag(tag))
                    return true;
        return false;
    }

    float NextFloat(float min, float max)
    {
        return (float)(prng.NextDouble() * (max - min) + min);
    }

    void CalculateChunkTargets()
    {
        remainingToSpawn.Clear();

        var biome = GetChunkBiome();
        if (biome == null) return;

        foreach (var entry in biome.props)
        {
            if (entry.prop == null) continue;

            int amount = prng.Next(entry.minPerChunk, entry.maxPerChunk + 1);

            if (amount > 0)
                remainingToSpawn[entry.prop] = amount;
        }
    }


    BiomeDefinition GetChunkBiome()
    {
        Vector3 center = transform.position + new Vector3(chunkSize * 0.5f, 0, chunkSize * 0.5f);
        return biomeMap.GetBiomeDefinition(center);
    }

    void SpawnCluster(WorldPropSO prop)
    {
        int clusterSize = prng.Next(3, 8);
        float clusterRadius = prng.Next(4, 9);

        if (!FindClusterCenter(out Vector3 center))
            return;

        for (int i = 0; i < clusterSize; i++)
        {
            Vector2 offset = Random.insideUnitCircle * clusterRadius;
            Vector3 origin = center + new Vector3(offset.x, rayHeight, offset.y);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
                continue;

            if (IsInForbiddenZone(hit.point))
                continue;

            if (!IsFarEnough(hit.point, prop.minSpacing))
                continue;

            Quaternion rot = Quaternion.Euler(0, NextFloat(0, 360), 0);
            GameObject obj = Instantiate(prop.prefab, hit.point, rot, transform);

            if (prop.alignToGround)
                obj.transform.up = hit.normal;

            spawnedPositions.Add(hit.point);
        }
    }
    bool FindClusterCenter(out Vector3 center)
    {
        for (int i = 0; i < 10; i++)
        {
            float x = NextFloat(0, chunkSize);
            float z = NextFloat(0, chunkSize);
            Vector3 origin = transform.position + new Vector3(x, rayHeight, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayer))
            {
                center = hit.point;
                return true;
            }
        }

        center = Vector3.zero;
        return false;
    }


}
