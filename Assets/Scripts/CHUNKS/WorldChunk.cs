using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    public Vector2Int coord;
    public int chunkSize;

    public BiomeMap biomeMap;
    public WorldPropGenerator_TEST propGenerator;

    public void Generate(int worldSeed)
    {
        int chunkSeed = worldSeed
            ^ (coord.x * 73856093)
            ^ (coord.y * 19349663);

        propGenerator.seed = chunkSeed;
        propGenerator.useSeed = true;

        propGenerator.chunkCoord = coord;
        propGenerator.chunkSize = chunkSize;
        propGenerator.biomeMap = biomeMap; // 👈 AQUÍ SE ARREGLA TODO

        propGenerator.Generate();
    }

    public void Clear()
    {
        propGenerator.Clear();
    }
}
