using UnityEngine;

[System.Serializable]
public struct ResourceDrop
{
    public string materialId;
    public GameObject pickupPrefab;

    public int minAmount;
    public int maxAmount;

    [Range(0f, 1f)]
    public float dropChance;   // 👈 probabilidad 0–1

    public float weight;       // 👈 para loot table (opcional por ahora)
}


[CreateAssetMenu(menuName = "Resources/Resource Data")]
public class ResourceData : ScriptableObject
{
    [Header("Identity")]
    public string resourceId; // "Tree", "Rock", etc.

    [Header("Visual")]
    public GameObject prefab;

    [Header("Health")]
    public float maxHealth = 50f;

    [Header("Drops")]
    public ResourceDrop[] drops;

    [Header("Respawn")]
    public bool canRespawn = true;
    public float respawnTime = 60f;
}
