using UnityEngine;

[System.Serializable]
public struct ResourceDrop
{
    public string materialId;     // "Wood", "Metal", "Scrap"
    public GameObject pickupPrefab;
    public int minAmount;
    public int maxAmount;
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
