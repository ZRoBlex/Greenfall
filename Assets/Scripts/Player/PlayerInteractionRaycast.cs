using UnityEngine;
using TMPro;

public class PlayerInteractRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public float interactDistance = 4f;
    public LayerMask interactMask;

    [Header("UI")]
    public TextMeshProUGUI interactionText;

    [Header("References")]
    public Camera cam;

    public IInteractable CurrentInteractable { get; private set; }

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        interactionText.text = "";
    }

    void Update()
    {
        DoRaycast();
    }

    void DoRaycast()
    {
        interactionText.text = "";
        CurrentInteractable = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
            return;

        var interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null)
            return;

        CurrentInteractable = interactable;
        interactionText.text = interactable.GetInteractText();
    }
}
