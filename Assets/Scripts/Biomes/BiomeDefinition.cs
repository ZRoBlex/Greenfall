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
    public string name;
    public PropType type;

    public List<GameObject> prefabs = new List<GameObject>();

    [Range(0f, 1f)]
    public float spawnChance = 1f;

    public float minSpacing = 3f;

    [Header("Density")]
    public float densityPerKm2 = 120f;

    public bool alignToGround = true;

    [System.NonSerialized]
    public int spawnedCount = 0;

    [System.NonSerialized]
    public int targetCount = 0;   // 👈 ahora se calcula
}


[CreateAssetMenu(menuName = "World/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    public BiomeType biomeType;

    [Header("Allowed Props in this Biome")]
    public List<BiomePropEntry> props = new List<BiomePropEntry>();
}
