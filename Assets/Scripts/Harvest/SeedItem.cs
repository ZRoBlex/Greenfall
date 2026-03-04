using UnityEngine;

[CreateAssetMenu(menuName = "World/Farming/Seed")]
public class SeedItem : ScriptableObject
{
    public string seedId;
    public GameObject plantPrefab;   // qu� planta crece de esta semilla

    [Header("Growth Settings")]
    public float growTime = 60f;      // tiempo total en segundos
    public int maxHarvests = 1;       // cu�ntas veces se puede cosechar

    [Header("Drops")]
    public GameObject cropPrefab;     // lo que da al cosechar
    public int minCropAmount = 1;
    public int maxCropAmount = 3;

    public int minSeedReturn = 0;
    public int maxSeedReturn = 2;

    [Header("World Pickup")]
    public GameObject seedPickupPrefab;

    public Sprite icon; // ícono para el inventario

}
