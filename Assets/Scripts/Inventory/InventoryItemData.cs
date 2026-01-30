using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class InventoryItemData : ScriptableObject
{
    public string itemId;
    public Sprite icon;

    [Header("Size in inventory (pixels)")]
    public Vector2 size = new Vector2(64, 64);

    public ItemCategory category;
}
