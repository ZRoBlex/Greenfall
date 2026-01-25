using System.Collections.Generic;
using UnityEngine;

public class BiomeMap : MonoBehaviour
{
    [Header("Seed")]
    public int seed = 12345;

    [Header("Biome Settings")]
    public BiomeSettings settings;

    [Header("Debug Preview")]
    public bool drawPreview = true;
    public float previewSize = 200f;
    public float previewStep = 8f;

    public List<BiomeDefinition> allBiomeDefinitions = new List<BiomeDefinition>();


    float offsetX;
    float offsetZ;

    void OnValidate()
    {
        RecalculateOffsets();
    }

    void Awake()
    {
        RecalculateOffsets();
    }

    //public void RecalculateOffsets()
    //{
    //    offsetX = seed * 1000.123f;
    //    offsetZ = seed * 2000.456f;
    //}
    public void RecalculateOffsets()
    {
        System.Random rng = new System.Random(seed);

        // offsets grandes PERO seguros (no billones)
        offsetX = rng.Next(-100000, 100000);
        offsetZ = rng.Next(-100000, 100000);
    }



    public float GetNoiseValue(Vector3 worldPos)
    {
        if (settings == null)
            return 0f;

        float nx = (worldPos.x + offsetX) * settings.noiseScale;
        float nz = (worldPos.z + offsetZ) * settings.noiseScale;

        return Mathf.PerlinNoise(nx, nz);
    }

    public BiomeType GetBiome(Vector3 worldPos)
    {
        if (settings == null || settings.biomes.Count == 0)
            return BiomeType.Plains;

        float noise = GetNoiseValue(worldPos);

        foreach (var range in settings.biomes)
        {
            if (noise >= range.min && noise < range.max)
                return range.biome;
        }

        return settings.biomes[settings.biomes.Count - 1].biome;
    }

    public BiomeDefinition GetBiomeDefinition(Vector3 worldPos)
    {
        float noise = GetNoiseValue(worldPos);

        foreach (var range in settings.biomes)
        {
            if (noise >= range.min && noise < range.max)
                return range.definition;
        }

        return null;
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawPreview || settings == null)
            return;

        for (float x = -previewSize * 0.5f; x <= previewSize * 0.5f; x += previewStep)
        {
            for (float z = -previewSize * 0.5f; z <= previewSize * 0.5f; z += previewStep)
            {
                Vector3 pos = transform.position + new Vector3(x, 0f, z);
                var biome = GetBiome(pos);

                Gizmos.color = GetBiomeColor(biome);
                Gizmos.DrawCube(pos + Vector3.up * 0.05f, Vector3.one * (previewStep * 0.9f));
            }
        }
    }

    Color GetBiomeColor(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert: return new Color(1f, 0.85f, 0.2f);
            case BiomeType.Plains: return Color.green;
            case BiomeType.Forest: return new Color(0f, 0.5f, 0f);
            case BiomeType.Mountains: return Color.gray;
            case BiomeType.Snow: return Color.white;
            case BiomeType.Farmland: return Color.red;
            case BiomeType.City: return Color.blue;
            default: return Color.magenta;
        }
    }
#endif
}
