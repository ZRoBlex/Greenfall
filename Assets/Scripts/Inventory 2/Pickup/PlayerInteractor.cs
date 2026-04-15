// ============================================================
//  PlayerInteractor.cs
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Player/
// ============================================================
//
//  QUÉ HACE:
//  Script UNIFICADO de interacción del jugador.
//  Reemplaza COMPLETAMENTE a:
//    - PlayerInteractRaycast.cs (raycast genérico)
//    - PlayerAmmoInteractor.cs (específico para munición)
//
//  QUÉ DETECTA:
//  Cualquier objeto que tenga InventoryItemController en su jerarquía.
//  También detecta cualquier IInteractable (puertas, NPCs, etc.)
//
//  CÓMO FUNCIONA:
//  Cada frame: lanza un raycast desde la cámara.
//  Si golpea algo interactuable:
//    → Muestra el texto de interacción en la UI
//    → Resalta el objeto (outline)
//  Al presionar E (o el botón configurado):
//    → Llama a la acción del objeto (recoger, abrir, etc.)
//
//  COMPATIBILIDAD:
//  Funciona con el nuevo Input System (PlayerInputHandler) Y con el
//  Input System antiguo (Input.GetKeyDown). Configurable en Inspector.
//
//  COLOCA ESTE SCRIPT EN: el mismo GameObject que PlayerController
// ============================================================

using UnityEngine;
using TMPro;

/// <summary>
/// Interfaz que cualquier objeto interactuable debe implementar.
/// Permite que PlayerInteractor funcione con puertas, NPCs, etc.
/// además de items recogibles.
/// </summary>
public interface IInteractable
{
    /// <summary>Texto que aparece en la UI. Ej: "Abrir Puerta", "Hablar con Elias".</summary>
    string GetInteractionText();

    /// <summary>Ejecuta la acción de interacción. Llamado al presionar E.</summary>
    bool TryInteract(GameObject interactor);

    /// <summary>Transform del objeto para calcular distancia.</summary>
    Transform WorldTransform { get; }
}

// ─────────────────────────────────────────────────────────────────────────────

public class PlayerInteractor : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN DE DETECCIÓN
    // ─────────────────────────────────────────────────────────────────────

    [Header("Detección")]
    [Tooltip("Distancia máxima del raycast de interacción (metros). " +
             "4 es un buen valor para FPS en primera persona.")]
    [Range(1f, 8f)] [SerializeField] private float _interactDistance = 4f;

    [Tooltip("LayerMask que define qué capas pueden ser interactuadas. " +
             "Excluye siempre la capa del Player para evitar auto-detección.")]
    [SerializeField] private LayerMask _interactMask = ~0;

    // ─────────────────────────────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────────────────────────────

    [Header("Input")]
    [Tooltip("Si tienes PlayerInputHandler, asígnalo aquí para usar el New Input System. " +
             "Si está vacío, usa el Input.GetKeyDown() clásico con la tecla _fallbackKey.")]
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Tooltip("Tecla de interacción (usado solo si _inputHandler es null).")]
    [SerializeField] private KeyCode _fallbackKey = KeyCode.E;

    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────────────────────────────

    [Header("Referencias")]
    [Tooltip("La cámara del jugador. Se autodetecta con Camera.main si no se asigna.")]
    [SerializeField] private Camera _camera;

    // ─────────────────────────────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────────────────────────────

    [Header("UI de Interacción")]
    [Tooltip("Texto TMP donde aparece el texto de interacción. " +
             "Ej: 'Recoger Hacha Básica'. Puede ser null si no tienes UI.")]
    [SerializeField] private TextMeshProUGUI _interactionText;

    [Tooltip("GameObject del panel/fondo de la UI de interacción. " +
             "Se activa/desactiva automáticamente. Puede ser null.")]
    [SerializeField] private GameObject _interactionPanel;

    [Tooltip("Icono/imagen que acompaña el texto. Se actualiza con el ícono del item. " +
             "Puede ser null.")]
    [SerializeField] private UnityEngine.UI.Image _interactionIcon;

    [Tooltip("Texto que aparece cuando el inventario está lleno.")]
    [SerializeField] private string _inventoryFullMessage = "Inventario lleno";

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // El item recogible que el jugador está mirando este frame
    private InventoryItemController _currentItemTarget;

    // El objeto interactuable genérico que el jugador está mirando
    private IInteractable _currentInteractable;

    // El último objeto al que le activamos el highlight
    // (para desactivarlo cuando el jugador deja de mirarlo)
    private InventoryItemController _lastHighlightedItem;

    // Timer para mostrar el mensaje de inventario lleno temporalmente
    private float _fullMessageTimer = 0f;
    private const float FULL_MESSAGE_DURATION = 2f;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        // Suscribirse al evento de inventario lleno para mostrar mensaje
        // (InventorySystem dispara este evento cuando no cabe el item)
    }

    private void Start()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryFull += HandleInventoryFull;

        // Iniciar la UI oculta
        SetUIVisible(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // 1. Detectar qué hay frente al jugador
        DoRaycast();

        // 2. Actualizar la UI según lo detectado
        UpdateUI();

        // 3. Verificar si el jugador presionó el botón de interacción
        if (GetInteractPressed())
            TryInteract();

        // 4. Temporizador del mensaje de inventario lleno
        if (_fullMessageTimer > 0f)
        {
            _fullMessageTimer -= Time.deltaTime;
            if (_fullMessageTimer <= 0f)
            {
                // Volver a mostrar el texto normal (si sigue mirando algo)
                UpdateUI();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  RAYCAST
    // ─────────────────────────────────────────────────────────────────────

    private void DoRaycast()
    {
        // Resetear targets del frame anterior
        _currentItemTarget    = null;
        _currentInteractable  = null;

        if (_camera == null) return;

        // Raycast desde el centro de la cámara hacia adelante
        var ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _interactMask,
                             QueryTriggerInteraction.Collide))
        {
            // No golpeó nada — desactivar highlight del objeto anterior si lo había
            ClearHighlight();
            return;
        }

        // Buscar componentes interactuables en el objeto golpeado y sus padres
        // Primero buscamos InventoryItemController (items recogibles)
        _currentItemTarget = hit.collider.GetComponent<InventoryItemController>()
                          ?? hit.collider.GetComponentInParent<InventoryItemController>();

        // Si no es item recogible, buscar IInteractable genérico (puertas, NPCs, etc.)
        if (_currentItemTarget == null)
        {
            _currentInteractable = hit.collider.GetComponent<IInteractable>()
                                ?? hit.collider.GetComponentInParent<IInteractable>();
        }

        // Manejar highlight del nuevo target
        if (_currentItemTarget != _lastHighlightedItem)
        {
            ClearHighlight();
            _lastHighlightedItem = _currentItemTarget;
            _lastHighlightedItem?.SetHighlight(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INTERACCIÓN
    // ─────────────────────────────────────────────────────────────────────

    private void TryInteract()
    {
        // Intentar recoger item
        if (_currentItemTarget != null)
        {
            bool success = _currentItemTarget.TryPickup();
            if (success)
            {
                // Si el item se recogió, el target deja de existir
                _lastHighlightedItem = null;
                _currentItemTarget   = null;
                SetUIVisible(false);
            }
            // Si falló, HandleInventoryFull ya muestra el mensaje
            return;
        }

        // Intentar interacción genérica (puertas, NPCs, etc.)
        if (_currentInteractable != null)
        {
            _currentInteractable.TryInteract(gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────────────────────────────

    private bool GetInteractPressed()
    {
        // Preferir el New Input System si está disponible
        if (_inputHandler != null && _inputHandler.InteractTrigger)
        {
            _inputHandler.ResetInteractTrigger();
            return true;
        }

        // Fallback al Input clásico
        return Input.GetKeyDown(_fallbackKey);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        // Si hay un mensaje de inventario lleno activo, no sobreescribir
        if (_fullMessageTimer > 0f) return;

        // Si hay item target, mostrar su texto
        if (_currentItemTarget != null)
        {
            SetInteractionText(_currentItemTarget.GetInteractionText());
            UpdateInteractionIcon(_currentItemTarget.ItemData?.icon);
            SetUIVisible(true);
            return;
        }

        // Si hay interactuable genérico, mostrar su texto
        if (_currentInteractable != null)
        {
            SetInteractionText(_currentInteractable.GetInteractionText());
            UpdateInteractionIcon(null);
            SetUIVisible(true);
            return;
        }

        // No hay nada — ocultar UI
        SetUIVisible(false);
    }

    private void HandleInventoryFull(InventoryItemData itemData)
    {
        // Mostrar mensaje de inventario lleno temporalmente
        SetInteractionText(_inventoryFullMessage);
        SetUIVisible(true);
        _fullMessageTimer = FULL_MESSAGE_DURATION;
    }

    private void SetInteractionText(string text)
    {
        if (_interactionText != null)
            _interactionText.text = text;
    }

    private void UpdateInteractionIcon(Sprite icon)
    {
        if (_interactionIcon == null) return;
        if (icon != null)
        {
            _interactionIcon.sprite  = icon;
            _interactionIcon.enabled = true;
        }
        else
        {
            _interactionIcon.enabled = false;
        }
    }

    private void SetUIVisible(bool visible)
    {
        if (_interactionPanel != null)
            _interactionPanel.SetActive(visible);

        if (!visible && _interactionText != null)
            _interactionText.text = "";
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────

    private void ClearHighlight()
    {
        if (_lastHighlightedItem != null)
        {
            _lastHighlightedItem.SetHighlight(false);
            _lastHighlightedItem = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryFull -= HandleInventoryFull;

        ClearHighlight();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_camera == null) return;
        Gizmos.color = _currentItemTarget != null ? Color.green : Color.cyan;
        Gizmos.DrawRay(_camera.transform.position,
                       _camera.transform.forward * _interactDistance);
    }
#endif
}