using UnityEngine;

/// <summary>
/// Base para CUALQUIER item del juego
/// Sistema modular y extensible
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("═══════ INFO ═══════")]
    public string itemId;
    public string itemName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("═══════ TIPO ═══════")]
    public ItemCategory category;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("═══════ STACK ═══════")]
    public bool isStackable = true;

    [Range(1, 9999)]
    public int maxStack = 99;

    [Header("═══════ PREFABS ═══════")]
    public GameObject worldPrefab; // Para dropear en el mundo
    public GameObject equipPrefab; // Para equipar (si aplica)

    [Header("═══════ VALOR ═══════")]
    [Range(0, 999999)]
    public int baseValue = 10;

    // ═══════ VIRTUAL METHODS ═══════

    public virtual bool CanUse() => false;

    public virtual void Use(GameObject user) { }

    public virtual string GetTooltip()
    {
        return $"<b>{itemName}</b>\n{description}";
    }
}

public enum ItemCategory
{
    Weapon,
    Ammo,
    Material,
    Food,
    Tool,
    Consumable,
    Quest,
    Misc
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}