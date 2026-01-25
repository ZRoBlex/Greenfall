using UnityEngine;
using System.Collections.Generic;

public class FloorLootSpawner : MonoBehaviour
{
    [Header("Seed Settings")]
    public bool useSeed = true;
    public int seed = 12345;
    public bool randomizeSeedOnGenerate = false;

    System.Random prng;

    [Header("Area")]
    public Vector2 areaSize = new Vector2(100f, 100f);
    public int maxLootCount = 50;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayHeight = 50f;
    public float maxRayDistance = 100f;

    [Header("Placement")]
    public float minSpacing = 2f;
    public float maxSlopeAngle = 35f;
    public float verticalOffset = 0.15f;

    [Header("Noise Settings")]
    public float noiseScale = 0.05f;
    public float noiseThreshold = 0.5f;

    [Header("Forbidden Tags")]
    public List<string> forbiddenTags = new List<string>();

    [Header("Weapon Prefabs")]
    public List<GameObject> weaponPrefabs = new List<GameObject>();

    List<Vector3> spawnedPositions = new List<Vector3>();
    List<GameObject> spawnedLoot = new List<GameObject>();

    // =====================================================

    void InitSeed()
    {
        if (useSeed)
        {
            if (randomizeSeedOnGenerate)
                seed = Random.Range(int.MinValue, int.MaxValue);

            prng = new System.Random(seed);
        }
        else
        {
            prng = new System.Random();
        }
    }

    float NextFloat01()
    {
        return (float)prng.NextDouble();
    }

    // =====================================================

    public void Generate()
    {
        Clear();
        InitSeed();

        if (weaponPrefabs.Count == 0)
        {
            Debug.LogWarning("FloorLootSpawner: No weapon prefabs assigned.");
            return;
        }

        int attempts = 0;
        int maxAttempts = maxLootCount * 15;

        while (spawnedLoot.Count < maxLootCount && attempts < maxAttempts)
        {
            attempts++;

            // 1️⃣ punto random en área
            float rx = Mathf.Lerp(-areaSize.x * 0.5f, areaSize.x * 0.5f, NextFloat01());
            float rz = Mathf.Lerp(-areaSize.y * 0.5f, areaSize.y * 0.5f, NextFloat01());

            Vector3 randomPos = transform.position + new Vector3(rx, rayHeight, rz);

            // 2️⃣ perlin noise
            float nx = (transform.position.x + rx) * noiseScale + seed * 0.001f;
            float nz = (transform.position.z + rz) * noiseScale + seed * 0.001f;
            float noise = Mathf.PerlinNoise(nx, nz);

            if (noise < noiseThreshold)
                continue;

            // 3️⃣ raycast suelo
            if (!Physics.Raycast(
                randomPos,
                Vector3.down,
                out RaycastHit hit,
                maxRayDistance,
                groundLayer))
                continue;

            // 4️⃣ pendiente
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle)
                continue;

            // 5️⃣ tags prohibidos
            if (forbiddenTags.Contains(hit.collider.tag))
                continue;

            Vector3 spawnPos = hit.point + Vector3.up * verticalOffset;

            // 6️⃣ spacing
            if (!IsFarEnough(spawnPos))
                continue;

            // 7️⃣ elegir arma random
            int index = prng.Next(0, weaponPrefabs.Count);
            GameObject prefab = weaponPrefabs[index];

            GameObject loot = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

            spawnedLoot.Add(loot);
            spawnedPositions.Add(spawnPos);
        }

        Debug.Log($"FloorLootSpawner: Spawned {spawnedLoot.Count} weapons.");
    }

    bool IsFarEnough(Vector3 pos)
    {
        foreach (var p in spawnedPositions)
        {
            if (Vector3.Distance(p, pos) < minSpacing)
                return false;
        }
        return true;
    }

    // =====================================================

    public void Clear()
    {
        spawnedPositions.Clear();

        for (int i = spawnedLoot.Count - 1; i >= 0; i--)
        {
            if (spawnedLoot[i] != null)
                DestroyImmediate(spawnedLoot[i]);
        }

        spawnedLoot.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(areaSize.x, 1f, areaSize.y)
        );
    }
#endif
}
