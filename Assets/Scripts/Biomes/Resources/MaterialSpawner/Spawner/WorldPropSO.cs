using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "World/World Prop")]
public class WorldPropSO : ScriptableObject
{
    [Header("Identity")]
    public string propId; // "OakTree", "PineTree", "SmallRock", etc.

    [Header("Visual / Resource")]
    public GameObject prefab;        // el modelo en el mundo
    public ResourceData resourceData; // drops, vida, respawn, etc.

    [Header("Spawn Rules")]
    public bool alignToGround = true;
    public float minSpacing = 3f;

    [Range(0f, 1f)]
    public float spawnChance = 1f; // probabilidad por intento

    [Header("Biome Control")]
    public bool allowedInPlains = true;
    public bool allowedInForest = true;
    public bool allowedInSnow = true;
    public bool allowedInDesert = true;
    public bool allowedInMountains = true;
    public bool allowedInFarmLand = true; // 👈 nuevo bioma
    public bool allowedInCity = true;     // 👈 nuevo bioma

    [Header("Density")]
    public float densityPerKm2 = 50f; // 👈 lo que ya estás usando por bioma
}
