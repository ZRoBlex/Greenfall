using UnityEngine;
using System.Collections.Generic;

public class TreeChunkManager : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;
    public int chunkSize = 32;
    public int renderDistance = 2;

    private Dictionary<Vector2Int, List<GameObject>> treeChunks = new Dictionary<Vector2Int, List<GameObject>>();

    // ⚡ Agrupa todos los árboles hijos en chunks
    public void InitializeFromParent(Transform treeParent)
    {
        treeChunks.Clear();

        foreach (Transform t in treeParent)
        {
            Vector2Int chunk = new Vector2Int(
                Mathf.FloorToInt(t.position.x / chunkSize),
                Mathf.FloorToInt(t.position.z / chunkSize)
            );

            if (!treeChunks.ContainsKey(chunk))
                treeChunks[chunk] = new List<GameObject>();

            treeChunks[chunk].Add(t.gameObject);
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        foreach (var kvp in treeChunks)
        {
            bool active = Mathf.Abs(kvp.Key.x - playerChunk.x) <= renderDistance &&
                          Mathf.Abs(kvp.Key.y - playerChunk.y) <= renderDistance;

            foreach (var tree in kvp.Value)
                if (tree != null)
                    tree.SetActive(active);
        }
    }
}
