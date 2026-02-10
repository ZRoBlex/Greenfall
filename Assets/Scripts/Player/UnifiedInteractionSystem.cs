using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Sistema de interacción UNIFICADO
/// Reemplaza: PlayerInteractRaycast, WeaponInteractor, PlayerAmmoInteractor, ResourceHarvester
/// </summary>
public class UnifiedInteractionSystem : MonoBehaviour
{
    [Header("═══════ RAYCAST ═══════")]
    [Range(1f, 10f)]
    public float interactDistance = 4f;

    public LayerMask interactMask = -1;

    [Header("═══════ REFERENCIAS ═══════")]
    public Camera playerCamera;
    public TextMeshProUGUI interactText;

    [Header("═══════ INPUT ═══════")]
    public PlayerInput playerInput;
    public string actionMapName = "Player";
    public string interactActionName = "Interact";

    // ═══════ ESTADO ═══════

    IInteractable currentInteractable;
    GameObject currentInteractableObject;

    InputAction interactAction;

    // ═══════ CACHE ═══════

    Transform cameraTransform;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        cameraTransform = playerCamera.transform;

        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput != null)
        {
            var map = playerInput.actions.FindActionMap(actionMapName, true);
            interactAction = map.FindAction(interactActionName, true);
            interactAction.performed += OnInteractPerformed;
        }

        if (interactText != null)
            interactText.text = "";
    }

    void OnDestroy()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
    }

    void Update()
    {
        DetectInteractable();
        UpdateUI();
    }

    // ═══════ DETECCIÓN ═══════

    void DetectInteractable()
    {
        currentInteractable = null;
        currentInteractableObject = null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
            return;

        // Buscar IInteractable en el objeto o sus padres
        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
            return;

        if (!interactable.CanInteract(gameObject))
            return;

        currentInteractable = interactable;
        currentInteractableObject = hit.collider.gameObject;
    }

    void UpdateUI()
    {
        if (interactText == null)
            return;

        if (currentInteractable == null)
        {
            interactText.text = "";
            interactText.gameObject.SetActive(false);
            return;
        }

        string message = currentInteractable.GetInteractText();
        string key = GetInteractKeyName();

        interactText.text = $"{message}\n<size=70%>({key})</size>";
        interactText.gameObject.SetActive(true);
    }

    // ═══════ INTERACCIÓN ═══════

    void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (currentInteractable == null)
            return;

        currentInteractable.Interact(gameObject);
    }

    // ═══════ HELPERS ═══════

    string GetInteractKeyName()
    {
        if (interactAction == null)
            return "?";

        var binding = interactAction.bindings[0];
        return InputControlPath.ToHumanReadableString(
            binding.effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    // ═══════ GIZMOS ═══════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDistance);
    }
#endif
}