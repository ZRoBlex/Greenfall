using UnityEngine;
using System.Collections.Generic;

public class TreeGenerator : MonoBehaviour
{
    [Header("Tree Prefab")]
    public GameObject treePrefab;

    [Header("Area")]
    public Vector2 areaSize = new Vector2(100f, 100f);
    public int treeCount = 200;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayHeight = 50f;
    public float maxRayDistance = 100f;

    [Header("Spacing")]
    public float minSpacing = 2.5f;

    [Header("Slope Limit")]
    public float maxSlopeAngle = 35f;

    List<Vector3> spawnedPositions = new List<Vector3>();

    public void Generate()
    {
        Clear();

        if (treePrefab == null)
        {
            Debug.LogError("TreeGenerator: No tree prefab assigned.");
            return;
        }

        int attempts = 0;
        int maxAttempts = treeCount * 10;

        while (spawnedPositions.Count < treeCount && attempts < maxAttempts)
        {
            attempts++;

            // 1️⃣ punto random en el área
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                rayHeight,
                Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f)
            );

            // 2️⃣ raycast hacia abajo
            if (Physics.Raycast(
                randomPos,
                Vector3.down,
                out RaycastHit hit,
                maxRayDistance,
                groundLayer
            ))
            {
                // 3️⃣ validar pendiente
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > maxSlopeAngle)
                    continue;

                Vector3 spawnPos = hit.point;

                // 4️⃣ validar separación mínima
                if (!IsFarEnough(spawnPos))
                    continue;

                // 5️⃣ instanciar árbol
                GameObject tree = Instantiate(
                    treePrefab,
                    spawnPos,
                    Quaternion.identity,
                    transform
                );

                // 6️⃣ alinear con el suelo (opcional pero bonito)
                tree.transform.up = hit.normal;

                spawnedPositions.Add(spawnPos);
            }
        }

        Debug.Log($"TreeGenerator: Spawned {spawnedPositions.Count} trees.");
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

    public void Clear()
    {
        spawnedPositions.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(areaSize.x, 1f, areaSize.y)
        );
    }
#endif
}
