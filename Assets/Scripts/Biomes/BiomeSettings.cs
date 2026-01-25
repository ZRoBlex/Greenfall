using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BiomeRange
{
    public BiomeType biome;
    [Range(0f, 1f)]
    public float min;
    [Range(0f, 1f)]
    public float max;
    public BiomeDefinition definition;   // 👈 NUEVO
}

[CreateAssetMenu(menuName = "World/Biome Settings")]
public class BiomeSettings : ScriptableObject
{
    public float noiseScale = 0.0035f;
    public List<BiomeRange> biomes = new List<BiomeRange>();
}
