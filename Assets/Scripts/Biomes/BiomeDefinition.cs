using UnityEngine;
using System.Collections.Generic;

public enum PropType
{
    Tree,
    Rock,
    Flower,
    Bush,
    Custom
}

[System.Serializable]
public class BiomePropEntry
{
    public WorldPropSO prop;

    [HideInInspector] public int targetCount;
    [HideInInspector] public int spawnedCount;
    [Header("Density Per Chunk")]
    public int minPerChunk = 1;
    public int maxPerChunk = 4;
}



[CreateAssetMenu(menuName = "World/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    public BiomeType biomeType;
    //public Color debugColor;
    public Color debugColor = Color.green; // para visualización

    [Header("Props in this biome")]
    public List<BiomePropEntry> props = new();

    [Header("Farming")]
    public bool allowPlanting = true;

    [Header("Fog Settings")]
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;

}
