// ============================================================
// InventoryPanelUI.cs
// Carpeta: Scripts/Inventory/UI/
// ------------------------------------------------------------
// El panel GRANDE del inventario que se abre con Tab (o Q).
// Contiene:
//   - Sección de HOTBAR (los mismos slots de HotbarUI, pero en el panel)
//   - Sección de BOLSA (los slots bag[0..bagSize-1])
//
// COMPORTAMIENTO AL ABRIR:
//   1. El panel se hace visible (SetActive(true))
//   2. Se habilita drag en todos los slots (hotbar + bolsa)
//   3. El cursor se libera (opcional, configurar en Inspector)
//   4. El input de movimiento del jugador puede pausarse
//
// COMPORTAMIENTO AL CERRAR:
//   1. El panel se oculta (SetActive(false))
//   2. Se deshabilita drag en los slots de hotbar
//   3. Los slots de bolsa no importan (están ocultos)
//   4. El cursor vuelve a bloquearse
//
// GHOST ICON:
//   Un único Image en el Canvas raíz que se mueve con el mouse.
//   Se asigna a todos los slots para el feedback visual de drag.
//
// ESTRUCTURA DE UI RECOMENDADA:
// Canvas (CanvasScaler: Scale With Screen Size)
// ├── GhostIcon          (Image — fuera de todo panel, tamaño 64x64)
// ├── HotbarPanel        (HotbarUI — siempre visible)
// │   ├── Slot_0
// │   ├── Slot_1  ... etc
// └── InventoryPanel     (InventoryPanelUI — se oculta/muestra)
//     ├── HotbarSection  (muestra los mismos slots que HotbarPanel visualmente)
//     │   ├── Slot_0_copy
//     │   └── ...
//     └── BagSection
//         ├── BagSlot_0
//         └── ...
//
// NOTA: Los slots del panel SON los mismos InventorySlotUI del HotbarUI,
// pero DENTRO del panel también hay copias de los slots de hotbar visibles
// para que el jugador pueda mover cosas de la bolsa a la hotbar y viceversa.
// ALTERNATIVA SIMPLE: Usar UNA sola sección con todos los slots en el panel.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using Greenfall.Inventory;

public class InventoryPanelUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("Estructura del Panel")]
    [Tooltip("El GameObject del panel completo. Se activa/desactiva.")]
    [SerializeField] private GameObject _panelRoot;

    [Tooltip("El HotbarUI (la barra siempre visible). Para controlar su drag.")]
    [SerializeField] private HotbarUI _hotbarUI;

    [Tooltip("Ghost icon en el Canvas raíz. Un único Image para todos los slots.")]
    [SerializeField] private Image _ghostIcon;

    [Header("Slots del Panel")]
    [Tooltip("Los slots de HOTBAR dentro del panel (para mover items de/hacia hotbar).")]
    [SerializeField] private InventorySlotUI[] _hotbarSlotsInPanel;

    [Tooltip("Los slots de BOLSA dentro del panel.")]
    [SerializeField] private InventorySlotUI[] _bagSlots;

    [Header("Input")]
    [Tooltip("Tecla para abrir/cerrar el inventario.")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;

    [Header("Cursor")]
    [Tooltip("¿Liberar el cursor al abrir el inventario?")]
    [SerializeField] private bool _freeCursorOnOpen = true;

    [Header("Animación (opcional)")]
    [Tooltip("CanvasGroup para fade in/out. Opcional.")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeSpeed = 8f;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    private bool _isOpen;
    private float _targetAlpha;

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Ocultar el panel al inicio
        if (_panelRoot != null) _panelRoot.SetActive(false);
        if (_canvasGroup != null) { _canvasGroup.alpha = 0f; _targetAlpha = 0f; }

        // El ghost icon empieza oculto
        if (_ghostIcon != null) _ghostIcon.enabled = false;
    }

    private void Start()
    {
        if (UnifiedInventory.Instance == null)
        {
            Debug.LogError("[InventoryPanelUI] No hay UnifiedInventory en escena.");
            return;
        }

        InitializeSlots();
    }

    private void InitializeSlots()
    {
        int hotbarSize = UnifiedInventory.Instance.HotbarSize;
        int bagSize    = UnifiedInventory.Instance.BagSize;

        // Slots de hotbar dentro del panel
        for (int i = 0; i < _hotbarSlotsInPanel.Length && i < hotbarSize; i++)
        {
            int globalIndex = UnifiedInventory.Instance.HotbarIndex(i);
            _hotbarSlotsInPanel[i].Initialize(globalIndex, _ghostIcon);
            _hotbarSlotsInPanel[i].SetDragEnabled(false);
        }

        // Slots de bolsa
        for (int i = 0; i < _bagSlots.Length && i < bagSize; i++)
        {
            int globalIndex = UnifiedInventory.Instance.BagIndex(i);
            _bagSlots[i].Initialize(globalIndex, _ghostIcon);
            _bagSlots[i].SetDragEnabled(false);
        }
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE — Input de toggle
    // ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            Toggle();

        // Fade in/out del panel (si tiene CanvasGroup)
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = Mathf.Lerp(
                _canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.unscaledDeltaTime);
        }
    }

    // ─────────────────────────────────────────────────────────
    // ABRIR / CERRAR
    // ─────────────────────────────────────────────────────────

    public void Toggle() => (_isOpen ? (System.Action)Close : Open)();

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        // Mostrar panel
        if (_panelRoot != null) _panelRoot.SetActive(true);
        _targetAlpha = 1f;

        // Habilitar drag en TODOS los slots
        SetAllDragEnabled(true);

        // Cursor
        if (_freeCursorOnOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // Pausar input de movimiento del jugador (opcional)
        // GreenFallInput.BlockMovement = true; // si tienes ese flag
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        // Ocultar panel
        _targetAlpha = 0f;
        // Usamos un delay por el fade, o directo si no hay fade
        if (_canvasGroup == null)
            if (_panelRoot != null) _panelRoot.SetActive(false);
        else
            StartCoroutine(HidePanelAfterFade());

        // Deshabilitar drag en hotbar (bolsa queda oculta, no importa)
        SetAllDragEnabled(false);
        _hotbarUI?.SetDragEnabled(false);

        // Restaurar cursor
        if (_freeCursorOnOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        // Restaurar input del jugador
        // GreenFallInput.BlockMovement = false;
    }

    private System.Collections.IEnumerator HidePanelAfterFade()
    {
        // Esperar a que el fade termine
        yield return new WaitUntil(() => _canvasGroup.alpha < 0.05f);
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // CONTROL DE DRAG
    // ─────────────────────────────────────────────────────────

    private void SetAllDragEnabled(bool enabled)
    {
        // Hotbar dentro del panel
        foreach (var slot in _hotbarSlotsInPanel)
            slot.SetDragEnabled(enabled);

        // Bolsa
        foreach (var slot in _bagSlots)
            slot.SetDragEnabled(enabled);

        // Hotbar siempre visible
        _hotbarUI?.SetDragEnabled(enabled);
    }

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    public bool IsOpen => _isOpen;
}
