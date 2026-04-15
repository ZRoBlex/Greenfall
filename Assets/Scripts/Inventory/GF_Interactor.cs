// ============================================================
//  GF_Interactor.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Reemplaza: PlayerInteractRaycast.cs, PlayerAmmoInteractor.cs,
//             WeaponInteractor.cs
//
//  Coloca este script en el mismo GameObject que FirstPersonController.
//  Detecta GF_WorldPickup (items recogibles) e IInteractable (puertas, etc.)
//  Compatible con PlayerInputHandler (New Input System) Y con KeyCode.
// ============================================================

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GF_Interactor : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────────────────────────────

    [Header("Raycast")]
    [Range(1f, 8f)] public float interactDistance = 4f;
    public LayerMask interactMask = ~0;

    [Header("Input")]
    [Tooltip("PlayerInputHandler del jugador. Si está asignado, usa New Input System.")]
    public PlayerInputHandler inputHandler;
    [Tooltip("Tecla de respaldo si inputHandler es null.")]
    public KeyCode fallbackKey = KeyCode.E;

    [Header("Referencias")]
    public Camera playerCamera;

    [Header("UI — Texto de Interacción")]
    [Tooltip("Texto TMP donde aparece '[ E ] Recoger Hacha'. Puede ser null.")]
    public TextMeshProUGUI interactionText;

    [Tooltip("Panel que contiene el texto. Se activa/desactiva automáticamente.")]
    public GameObject interactionPanel;

    [Tooltip("Imagen opcional para el ícono del item.")]
    public Image interactionIcon;

    [Header("Config")]
    public GF_InventoryConfig config;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    private GF_WorldPickup  _currentPickup;
    private IInteractable   _currentInteractable;
    private GF_WorldPickup  _lastHighlighted;

    private float _fullMessageTimer;
    private const float FULL_MSG_DURATION = 2f;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE / START
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        if (GF_Inventory.Instance != null)
            GF_Inventory.Instance.OnInventoryFull += HandleInventoryFull;

        SetUIVisible(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        DoRaycast();
        UpdateUI();

        if (GetInteractPressed()) TryInteract();

        if (_fullMessageTimer > 0f)
        {
            _fullMessageTimer -= Time.deltaTime;
            if (_fullMessageTimer <= 0f) UpdateUI(); // Restaurar texto normal
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  RAYCAST
    // ─────────────────────────────────────────────────────────────────────

    private void DoRaycast()
    {
        _currentPickup      = null;
        _currentInteractable = null;

        if (playerCamera == null) return;

        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask,
                             QueryTriggerInteraction.Collide))
        {
            ClearHighlight();
            return;
        }

        // Buscar GF_WorldPickup
        _currentPickup = hit.collider.GetComponent<GF_WorldPickup>()
                      ?? hit.collider.GetComponentInParent<GF_WorldPickup>();

        // Si no es un pickup, buscar IInteractable genérico
        if (_currentPickup == null)
        {
            _currentInteractable = hit.collider.GetComponent<IInteractable>()
                                ?? hit.collider.GetComponentInParent<IInteractable>();
        }

        // Gestionar highlight
        if (_currentPickup != _lastHighlighted)
        {
            ClearHighlight();
            _lastHighlighted = _currentPickup;
            _lastHighlighted?.SetHighlight(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INTERACCIÓN
    // ─────────────────────────────────────────────────────────────────────

    private void TryInteract()
    {
        if (_currentPickup != null)
        {
            bool ok = _currentPickup.TryPickup();
            if (ok)
            {
                _lastHighlighted = null;
                _currentPickup   = null;
                SetUIVisible(false);
            }
            return;
        }

        _currentInteractable?.TryInteract(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────────────────────────────

    private bool GetInteractPressed()
    {
        // New Input System
        if (inputHandler != null && inputHandler.InteractTrigger)
        {
            inputHandler.ResetInteractTrigger();
            return true;
        }
        // Legacy fallback
        return Input.GetKeyDown(fallbackKey);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (_fullMessageTimer > 0f) return; // No sobreescribir mensaje de lleno

        if (_currentPickup != null)
        {
            SetInteractionText(_currentPickup.GetInteractionText());
            UpdateIcon(_currentPickup.definition?.icon);
            SetUIVisible(true);
            return;
        }

        if (_currentInteractable != null)
        {
            SetInteractionText(_currentInteractable.GetInteractionText());
            UpdateIcon(null);
            SetUIVisible(true);
            return;
        }

        SetUIVisible(false);
    }

    private void HandleInventoryFull(GF_ItemDefinition def)
    {
        string msg = config != null
            ? config.inventoryFullMessage
            : "Inventario lleno";

        SetInteractionText(msg);
        SetUIVisible(true);
        _fullMessageTimer = FULL_MSG_DURATION;
    }

    private void SetInteractionText(string text)
    {
        if (interactionText != null) interactionText.text = text;
    }

    private void UpdateIcon(Sprite icon)
    {
        if (interactionIcon == null) return;
        interactionIcon.enabled = icon != null;
        if (icon != null) interactionIcon.sprite = icon;
    }

    private void SetUIVisible(bool visible)
    {
        if (interactionPanel != null) interactionPanel.SetActive(visible);
        if (!visible && interactionText != null) interactionText.text = "";
    }

    private void ClearHighlight()
    {
        _lastHighlighted?.SetHighlight(false);
        _lastHighlighted = null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (GF_Inventory.Instance != null)
            GF_Inventory.Instance.OnInventoryFull -= HandleInventoryFull;
        ClearHighlight();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = _currentPickup != null ? Color.green : Color.cyan;
        Gizmos.DrawRay(playerCamera.transform.position,
                       playerCamera.transform.forward * interactDistance);
    }
#endif
}