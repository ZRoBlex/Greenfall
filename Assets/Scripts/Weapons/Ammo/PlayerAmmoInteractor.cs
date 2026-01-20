using UnityEngine;
using TMPro;

public class PlayerAmmoInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public float interactDistance = 4f;
    public string ammoTag = "AmmoBox";   // 👈 TAG que deben tener las cajas

    [Header("UI")]
    public TextMeshProUGUI interactText;

    private AmmoBox hoveredBox;

    void Update()
    {
        hoveredBox = null;
        interactText.text = "";

        Ray ray = new Ray(transform.position, transform.forward);

        // Raycast sin LayerMask
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            // Primero validar por TAG
            if (hit.collider.CompareTag(ammoTag))
            {
                AmmoBox box = hit.collider.GetComponentInParent<AmmoBox>();
                if (box != null)
                {
                    hoveredBox = box;
                    interactText.text = "INTERACT";
                }
            }
        }

        // Interacción (temporal con E)
        if (hoveredBox != null && Input.GetKeyDown(KeyCode.E))
        {
            hoveredBox.Interact();
            interactText.text = "";
            hoveredBox = null;
        }
    }
}
