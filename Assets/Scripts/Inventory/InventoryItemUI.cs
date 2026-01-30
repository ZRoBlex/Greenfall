using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public InventoryItemData data;

    public RectTransform rect;
    public Image background;
    public Image icon;

    CanvasGroup cg;
    Transform originalParent;
    Vector2 originalPos;
    Canvas dragCanvas;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        dragCanvas = GameObject.Find("Canvas_Drag")?.GetComponent<Canvas>();

        ApplySizeFromData();
    }

    void ApplySizeFromData()
    {
        if (data == null) return;

        rect.sizeDelta = data.size;

        if (background != null)
            background.rectTransform.sizeDelta = data.size;

        if (icon != null)
            icon.sprite = data.icon;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        originalParent = transform.parent;
        originalPos = rect.anchoredPosition;

        transform.SetParent(dragCanvas.transform);
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        rect.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        cg.blocksRaycasts = true;

        InventoryArea area = InventoryArea.Current;
        if (area == null || !area.TryPlace(this))
        {
            transform.SetParent(originalParent);
            rect.anchoredPosition = originalPos;
        }
    }
}
