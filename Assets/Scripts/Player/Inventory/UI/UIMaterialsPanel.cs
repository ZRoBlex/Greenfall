using UnityEngine;
using System.Collections.Generic;

public class UIMaterialsPanel : MonoBehaviour
{
    [System.Serializable]
    public class MaterialUIEntry
    {
        public string materialId;       // "Wood", "Stone", etc.
        public Sprite icon;
        public UIMaterialSlot slot;
    }

    public List<MaterialUIEntry> materials = new List<MaterialUIEntry>();

    private Dictionary<string, UIMaterialSlot> slotById =
        new Dictionary<string, UIMaterialSlot>();

    void Awake()
    {
        foreach (var entry in materials)
        {
            if (entry.slot == null) continue;

            entry.slot.SetIcon(entry.icon);
            slotById[entry.materialId] = entry.slot;
        }
    }

    // 🔄 Llamado desde PlayerStats
    public void SetMaterialAmount(string id, float amount, float max)
    {
        if (slotById.TryGetValue(id, out var slot))
        {
            slot.SetAmount(amount, max);
        }
    }
}
