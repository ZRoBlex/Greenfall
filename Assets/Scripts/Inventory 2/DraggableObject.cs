using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image image;
    [HideInInspector] public Transform parentAfterDrag;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Begin Drag");
        parentAfterDrag = transform.parent; // guardar el padre original para volver si no se suelta en un slot válido
        transform.SetParent(transform.root); // mover al root para que no se recorte por máscaras de UI
        transform.SetAsLastSibling(); // asegurarse de estar encima de otros elementos
        image.raycastTarget = false; // para que el raycast pase al slot destino
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentAfterDrag);
        image.raycastTarget = true; // para que el raycast pase al slot destino
    }
}
