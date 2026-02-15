using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Sistema de interacción UNIFICADO ultra optimizado
/// Funciona con CUALQUIER IInteractable
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

    [Header("═══════ FILTROS ═══════")]
    [Tooltip("Tags que el raycast ignorará completamente")]
    public string[] ignoredTags = { "Untagged", "Decoration", "Grass" };

    // ═══════ ESTADO ═══════

    IInteractable currentInteractable;

    InputAction interactAction;

    // ═══════ CACHE ═══════

    Transform cameraTransform;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null)
            cameraTransform = playerCamera.transform;

        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput != null)
        {
            var map = playerInput.actions.FindActionMap(actionMapName, true);
            if (map != null)
            {
                interactAction = map.FindAction(interactActionName, true);
                if (interactAction != null)
                    interactAction.performed += OnInteractPerformed;
            }
        }

        if (interactText != null)
        {
            interactText.text = "";
            interactText.gameObject.SetActive(false);
        }
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

    // ═══════ DETECCIÓN CON FILTRO ═══════

    void DetectInteractable()
    {
        currentInteractable = null;

        if (cameraTransform == null)
            return;

        if (!RaycastFiltered(out RaycastHit hit))
            return;

        // Buscar IInteractable
        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
            return;

        if (!interactable.CanInteract(gameObject))
            return;

        currentInteractable = interactable;
    }

    bool RaycastFiltered(out RaycastHit finalHit)
    {
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;

        float remainingDistance = interactDistance;
        Vector3 currentOrigin = origin;

        // Raycast con penetración de objetos ignorados
        while (remainingDistance > 0f)
        {
            if (!Physics.Raycast(
                currentOrigin,
                direction,
                out RaycastHit hit,
                remainingDistance,
                interactMask,
                QueryTriggerInteraction.Ignore))
            {
                break;
            }

            // Ignorar este tag?
            if (HasIgnoredTag(hit.collider))
            {
                float traveled = hit.distance + 0.01f;
                remainingDistance -= traveled;
                currentOrigin = hit.point + direction * 0.01f;
                continue;
            }

            // Hit válido
            finalHit = hit;
            return true;
        }

        finalHit = default;
        return false;
    }

    bool HasIgnoredTag(Collider col)
    {
        if (ignoredTags == null || ignoredTags.Length == 0)
            return false;

        string colTag = col.tag;

        foreach (string tag in ignoredTags)
        {
            if (colTag == tag)
                return true;
        }

        return false;
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
}