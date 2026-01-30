using UnityEngine;
using System.Collections.Generic;

public class InventoryArea : MonoBehaviour
{
    public static InventoryArea Current;

    public RectTransform areaRect;
    public float padding = 8f;

    List<InventoryItemUI> items = new();

    void Awake()
    {
        Current = this;
    }

    public bool TryPlace(InventoryItemUI item)
    {
        Rect itemRect = GetRect(item.rect);
        Rect area = GetRect(areaRect);

        // 1️⃣ Debe estar dentro del área
        if (!area.Overlaps(itemRect))
            return false;

        // 2️⃣ No debe superponerse con otros items
        foreach (var other in items)
        {
            if (other == item) continue;
            if (GetRect(other.rect).Overlaps(itemRect))
                return false;
        }

        // ✔ Aceptado
        item.transform.SetParent(transform);
        items.Add(item);
        return true;
    }

    Rect GetRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(
            corners[0],
            corners[2] - corners[0]
        );
    }
}
