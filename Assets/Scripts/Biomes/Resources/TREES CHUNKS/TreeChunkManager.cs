using UnityEngine;
using System.Collections.Generic;

public class TreeChunkManager : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Chunk Settings")]
    public int chunkSize = 32;            // tamaño de cada chunk en unidades
    public int renderDistance = 2;        // chunks alrededor del jugador

    [Header("Trees")]
    public List<WorldPropSO> treeSOs;
    public int treesPerChunk = 50;

    [Header("Randomness")]
    public float minHeight = 0.8f;
    public float maxHeight = 1.3f;

    [Header("Parent")]
    public Transform treeParent; // padre de todos los chunks

    private Dictionary<Vector2Int, TreeChunk> chunks = new();

    private System.Random prng;

    void Start()
    {
        prng = new System.Random();
        if (treeParent == null)
            treeParent = transform;

        UpdateChunks();
    }

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        // activar / crear chunks cercanos
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int c = playerChunk + new Vector2Int(x, z);
                if (!chunks.ContainsKey(c))
                {
                    TreeChunk chunk = new TreeChunk();
                    chunk.Generate(c, chunkSize, treesPerChunk, treeSOs, prng, minHeight, maxHeight, treeParent);
                    chunks[c] = chunk;
                }
                chunks[c].SetActive(true);
            }
        }

        // desactivar chunks lejanos
        List<Vector2Int> toDeactivate = new List<Vector2Int>();
        foreach (var kvp in chunks)
        {
            Vector2Int c = kvp.Key;
            if (Mathf.Abs(c.x - playerChunk.x) > renderDistance || Mathf.Abs(c.y - playerChunk.y) > renderDistance)
            {
                kvp.Value.SetActive(false);
            }
        }
    }
}

public class TreeChunk
{
    public GameObject chunkGO;
    public List<GameObject> trees = new();

    public void Generate(Vector2Int chunkPos, int chunkSize, int treesCount, List<WorldPropSO> treeSOs, System.Random prng, float minH, float maxH, Transform parent)
    {
        chunkGO = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        chunkGO.transform.parent = parent;

        for (int i = 0; i < treesCount; i++)
        {
            float rx = (float)prng.NextDouble() * chunkSize;
            float rz = (float)prng.NextDouble() * chunkSize;

            Vector3 pos = new Vector3(chunkPos.x * chunkSize + rx, 0f, chunkPos.y * chunkSize + rz);

            WorldPropSO treeSO = treeSOs[prng.Next(0, treeSOs.Count)];
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
