// ============================================================
// InventorySlotUI.cs
// Carpeta: Scripts/Inventory/UI/
// ------------------------------------------------------------
// Representa UN slot visual en la UI.
// Funciona para hotbar Y para la bolsa.
//
// DRAG & DROP:
// Implementa las 3 interfaces de Unity para drag:
//   IBeginDragHandler → guarda qué se está arrastrando
//   IDragHandler      → mueve el ícono de drag fantasma
//   IEndDragHandler   → intenta mover el item al slot destino
//
// REGLA DE BLOQUEO:
// _isDragEnabled lo controla InventoryPanelUI.
// Cuando el panel está cerrado, _isDragEnabled = false en hotbar slots.
// Cuando el panel está abierto, todos los slots tienen _isDragEnabled = true.
//
// CÓMO FUNCIONA EL "GHOST ICON" (ícono fantasma durante drag):
// Hay un único Image compartido en el Canvas (InventoryPanelUI lo provee).
// Durante el drag, se mueve con el mouse y la transparencia indica
// que es un "ghost". Al soltar, desaparece y el item se mueve en los datos.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Greenfall.Inventory;

public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    // ─────────────────────────────────────────────────────────
    // REFERENCIAS (asignar en Inspector)
    // ─────────────────────────────────────────────────────────

    [Header("Visuales")]
    [Tooltip("Imagen del ícono del item. Desactivar si está vacío.")]
    [SerializeField] private Image _icon;

    [Tooltip("Imagen de fondo del slot (resaltar si está seleccionado).")]
    [SerializeField] private Image _background;

    [Tooltip("Texto de cantidad (para seeds/stackables). Ocultar si amount = 1.")]
    [SerializeField] private TextMeshProUGUI _amountText;

    [Header("Lift (animación hotbar seleccionado)")]
    [SerializeField] private float _liftPixels = 15f;
    [SerializeField] private float _liftSpeed  = 10f;

    [Header("Colores de estado")]
    [SerializeField] private Color _normalColor   = Color.white;
    [SerializeField] private Color _selectedColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color _hoverColor    = new Color(0.8f, 0.9f, 1f);

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────

    private int  _globalIndex;      // Índice en UnifiedInventory
    private bool _isSelected;
    private bool _isDragEnabled;
    private bool _initialized;

    private Vector2 _basePosIcon;
    private Vector2 _basePosBg;

    // Ref al ghost icon compartido (lo provee InventoryPanelUI)
    private static Image        _ghostIcon;
    private static int          _dragSourceIndex = -1;
    private static InventorySlotUI _dragSourceSlot;

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Guardar posiciones base UNA sola vez
        if (_icon != null)
            _basePosIcon = _icon.rectTransform.anchoredPosition;
        if (_background != null)
            _basePosBg = _background.rectTransform.anchoredPosition;
        _initialized = true;
    }

    /// <summary>
    /// Llamado por InventoryPanelUI al construir la UI.
    /// </summary>
    /// <param name="globalIndex">Índice en UnifiedInventory</param>
    /// <param name="ghostIcon">Image compartida para mostrar durante drag</param>
    public void Initialize(int globalIndex, Image ghostIcon)
    {
        _globalIndex = globalIndex;
        _ghostIcon   = ghostIcon;

        // Suscribirse al evento de cambio de este slot
        if (UnifiedInventory.Instance != null)
        {
            UnifiedInventory.Instance.OnSlotChanged      += OnSlotChanged;
            UnifiedInventory.Instance.OnActiveSlotChanged += OnActiveChanged;
        }

        Refresh();
    }

    // ─────────────────────────────────────────────────────────
    // ACTUALIZAR VISUAL DESDE DATOS
    // ─────────────────────────────────────────────────────────

    /// <summary>Sincroniza el visual con los datos del slot en UnifiedInventory.</summary>
    public void Refresh()
    {
        if (UnifiedInventory.Instance == null) return;

        InventoryEntry entry = UnifiedInventory.Instance.GetSlot(_globalIndex);
        bool isEmpty = entry.IsEmpty;

        // ── Ícono ──
        if (_icon != null)
        {
            _icon.enabled = !isEmpty;
            if (!isEmpty) _icon.sprite = entry.icon;
        }

        // ── Cantidad ──
        if (_amountText != null)
        {
            bool showAmount = !isEmpty && entry.amount > 1;
            _amountText.gameObject.SetActive(showAmount);
            if (showAmount) _amountText.text = entry.amount.ToString();
        }
    }

    // ─────────────────────────────────────────────────────────
    // ANIMACIÓN LIFT (slot seleccionado en hotbar)
    // ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_initialized) return;
        AnimateLift();
    }

    private void AnimateLift()
    {
        if (_icon == null || !_icon.enabled) return;

        Vector2 targetIcon = _basePosIcon + (_isSelected ? Vector2.up * _liftPixels : Vector2.zero);
        _icon.rectTransform.anchoredPosition = Vector2.Lerp(
            _icon.rectTransform.anchoredPosition,
            targetIcon,
            Time.unscaledDeltaTime * _liftSpeed);

        if (_background != null)
        {
            Vector2 targetBg = _basePosBg + (_isSelected ? Vector2.up * _liftPixels : Vector2.zero);
            _background.rectTransform.anchoredPosition = Vector2.Lerp(
                _background.rectTransform.anchoredPosition,
                targetBg,
                Time.unscaledDeltaTime * _liftSpeed);
        }
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (_background != null)
            _background.color = selected ? _selectedColor : _normalColor;
    }

    // ─────────────────────────────────────────────────────────
    // CONTROL DE DRAG
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Activa o desactiva la posibilidad de arrastrar este slot.
    /// InventoryPanelUI llama esto al abrir/cerrar.
    /// </summary>
    public void SetDragEnabled(bool enabled) => _isDragEnabled = enabled;

    // ─────────────────────────────────────────────────────────
    // DRAG & DROP — Interfaces de Unity
    // ─────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_isDragEnabled) { eventData.pointerDrag = null; return; }

        InventoryEntry entry = UnifiedInventory.Instance?.GetSlot(_globalIndex);
        if (entry == null || entry.IsEmpty) { eventData.pointerDrag = null; return; }

        _dragSourceIndex = _globalIndex;
        _dragSourceSlot  = this;

        // Mostrar ghost icon siguiendo al mouse
        if (_ghostIcon != null)
        {
            _ghostIcon.sprite  = entry.icon;
            _ghostIcon.enabled = true;
            _ghostIcon.color   = new Color(1f, 1f, 1f, 0.7f);
            // Mover al canvas raíz
            _ghostIcon.transform.SetAsLastSibling();
        }

        // Ocultar ícono original mientras se arrastra
        if (_icon != null) _icon.color = new Color(1f, 1f, 1f, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostIcon == null || !_ghostIcon.enabled) return;

        // Mover el ghost con el puntero en coordenadas del Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _ghostIcon.canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPos);

        _ghostIcon.rectTransform.anchoredPosition = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Ocultar ghost
        if (_ghostIcon != null) _ghostIcon.enabled = false;

        // Restaurar ícono
        if (_icon != null) _icon.color = Color.white;

        _dragSourceIndex = -1;
        _dragSourceSlot  = null;
    }

    // IDropHandler: este slot recibe el item que soltaron encima
    public void OnDrop(PointerEventData eventData)
    {
        if (_dragSourceIndex < 0 || _dragSourceIndex == _globalIndex) return;

        // Mover datos en el inventario
        UnifiedInventory.Instance?.Move(_dragSourceIndex, _globalIndex);

        // Refrescar los dos slots involucrados visualmente
        _dragSourceSlot?.Refresh();
        Refresh();
    }

    // Click izquierdo en slot de hotbar → equipar arma
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!UnifiedInventory.Instance.IsHotbar(_globalIndex)) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        UnifiedInventory.Instance.SetActiveHotbar(_globalIndex);
    }

    // ─────────────────────────────────────────────────────────
    // LISTENERS DE EVENTOS
    // ─────────────────────────────────────────────────────────

    private void OnSlotChanged(int changedIndex)
    {
        if (changedIndex == _globalIndex) Refresh();
    }

    private void OnActiveChanged(int activeGlobal)
    {
        SetSelected(activeGlobal == _globalIndex);
    }

    private void OnDestroy()
    {
        if (UnifiedInventory.Instance == null) return;
        UnifiedInventory.Instance.OnSlotChanged       -= OnSlotChanged;
        UnifiedInventory.Instance.OnActiveSlotChanged -= OnActiveChanged;
    }
}
