using UnityEngine;
using System.Collections.Generic;

public class ResourceSpawner : MonoBehaviour
{
    public ResourceData[] resourceTypes;

    [Header("Spawn Area")]
    public Vector2 areaSize = new Vector2(80, 80);
    public LayerMask groundMask;
    public float rayHeight = 150f;

    [Header("Spawn Rules")]
    public int initialCount = 20;
    public float minDistanceBetween = 4f;

    readonly List<ResourceNode> spawned = new();

    void Start()
    {
        SpawnAll();
    }

    void SpawnAll()
    {
        int spawnedCount = 0;
        int safety = 0;

        while (spawnedCount < initialCount && safety < initialCount * 20)
        {
            if (TrySpawnOne())
                spawnedCount++;

            safety++;
        }

        if (spawnedCount < initialCount)
        {
            Debug.LogWarning(
                $"⚠️ Solo se spawnearon {spawnedCount}/{initialCount} recursos"
            );
        }
    }

    bool TrySpawnOne()
    {
        Vector3 pos;

        if (!TryGetValidPosition(out pos))
            return false;

        var data = resourceTypes[Random.Range(0, resourceTypes.Length)];

        GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, transform);

        var node = obj.GetComponent<ResourceNode>();
        node.data = data;

        spawned.Add(node);

        return true;
    }

    bool TryGetValidPosition(out Vector3 pos)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 random = GetRandomPoint();
            random.y += rayHeight;

            if (Physics.Raycast(
                random,
                Vector3.down,
                out RaycastHit hit,
                rayHeight * 2f,
                groundMask))
            {
                Vector3 p = hit.point;

                foreach (var n in spawned)
                {
                    if (Vector3.Distance(n.transform.position, p) < minDistanceBetween)
                        goto retry;
                }

                pos = p;
                return true;
            }

        retry:;
        }

        pos = Vector3.zero;
        return false;
    }

    Vector3 GetRandomPoint()
    {
        Vector2 half = areaSize * 0.5f;

        float x = Random.Range(-half.x, half.x);
        float z = Random.Range(-half.y, half.y);

        return transform.position + new Vector3(x, 0f, z);
    }
}
