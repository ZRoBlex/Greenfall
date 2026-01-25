using UnityEngine;

[CreateAssetMenu(menuName = "Pickups/Pickup Definition")]
public class PickupDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // "Wood", "Stone", "Metal"

    [Header("Visual / Prefab")]
    public GameObject prefab;         // el prefab físico del pickup

    [Header("Stacking")]
    public int maxStack = 50;

    [Header("Magnet")]
    public float magnetSpeed = 12f;
    public float collectDistance = 0.6f;

    [Header("Loot / Rareza")]
    public float baseDropWeight = 1f; // para loot tables luego

    [Header("UI")]
    public Sprite icon;
}
