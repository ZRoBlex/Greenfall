using UnityEngine;

[System.Serializable]
public class LootItem
{
    public GameObject prefab;

    [Range(0f, 100f)]
    public float dropChance = 50f;

    public int minAmount = 1;
    public int maxAmount = 1;
}
