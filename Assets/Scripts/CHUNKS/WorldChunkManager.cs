using System.Collections.Generic;
using UnityEngine;

public class WorldChunkManager : MonoBehaviour
{
    public Transform player;
    public int chunkSize = 64;
    public int renderDistance = 2;
    public int worldSeed = 12345;
    public GameObject chunkPrefab;
    public BiomeMap biomeMap;


    Dictionary<Vector2Int, WorldChunk> loadedChunks = new();

    void Update()
    {
        Vector2Int playerChunk = GetChunkCoord(player.position);

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, z);

                if (!loadedChunks.ContainsKey(coord))
                    LoadChunk(coord);
            }
        }

        UnloadFarChunks(playerChunk);
    }

    Vector2Int GetChunkCoord(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / chunkSize),
            Mathf.FloorToInt(pos.z / chunkSize)
        );
    }

    void LoadChunk(Vector2Int coord)
    {
        Vector3 worldPos = new Vector3(
            coord.x * chunkSize,
            0f,
            coord.y * chunkSize
        );

        GameObject go = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);
        WorldChunk chunk = go.GetComponent<WorldChunk>();

        chunk.coord = coord;
        chunk.chunkSize = chunkSize;
        chunk.biomeMap = biomeMap; // 👈 CLAVE

        chunk.Generate(worldSeed);

        loadedChunks.Add(coord, chunk);
    }


    void UnloadFarChunks(Vector2Int playerChunk)
    {
        List<Vector2Int> toRemove = new();

        foreach (var kv in loadedChunks)
        {
            int dx = Mathf.Abs(kv.Key.x - playerChunk.x);
            int dz = Mathf.Abs(kv.Key.y - playerChunk.y);

            if (dx > renderDistance || dz > renderDistance)
            {
                kv.Value.Clear();
                Destroy(kv.Value.gameObject);
                toRemove.Add(kv.Key);
            }
        }

        foreach (var key in toRemove)
            loadedChunks.Remove(key);
    }

}
