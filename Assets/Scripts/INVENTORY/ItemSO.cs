using UnityEngine;

public enum GeneralItemType
{
    Seed,
    Food,
    Upgrade,
    QuestItem,
    Tool,
    Misc
}

[CreateAssetMenu(menuName = "World/Items/General Item")]
public class ItemSO : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;
    [TextArea] public string description;

    [Header("Visual")]
    public Sprite icon;
    public GameObject worldPrefab; // para soltarlo en el mundo

    [Header("Type")]
    public GeneralItemType itemType;

    [Header("Stacking")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("Optional links")]
    public SeedItem seedData; // 👈 si este item ES una semilla
}
