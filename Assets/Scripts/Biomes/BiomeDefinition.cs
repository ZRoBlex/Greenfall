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
    public string name;                 // solo para debug
    public PropType type;

    public List<GameObject> prefabs = new List<GameObject>();

    [Range(0f, 1f)]
    public float spawnChance = 1f;      // probabilidad por intento

    public float minSpacing = 3f;
    public int targetCount = 50;        // densidad real

    public bool alignToGround = true;
}

[CreateAssetMenu(menuName = "World/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    public BiomeType biomeType;

    [Header("Allowed Props in this Biome")]
    public List<BiomePropEntry> props = new List<BiomePropEntry>();
}
