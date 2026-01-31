using UnityEngine;

public class BiomeFogUpdater : MonoBehaviour
{
    public BiomeMap biomeMap;
    public FogManager fogManager;

    BiomeDefinition currentBiome;

    void Update()
    {
        var biome = biomeMap.GetBiomeDefinition(transform.position);
        if (biome == null || biome == currentBiome) return;

        currentBiome = biome;
        //fogManager.SetBiomeFog(biome.fogColor, biome.fogDensity);
    }
}
