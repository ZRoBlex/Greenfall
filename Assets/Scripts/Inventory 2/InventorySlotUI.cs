// ============================================================
// InventorySlotUI.cs  — VERSIÓN CORREGIDA
// Carpeta: Scripts/Inventory/UI/
// ------------------------------------------------------------
// FIXES:
// 1. Drag & drop: _background forzado con raycastTarget = true.
//    Sin esto, OnDrop nunca se llama en el slot destino.
// 2. Click vs Drag: flag _wasDragging evita que OnPointerClick
//    dispare al terminar un drag.
// 3. Tamaño del ícono: campo _iconSize. (0,0) = no cambiar.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Greenfall.Inventory;

public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler
{
    [Header("Visuales")]
    [SerializeField] private Image _icon;
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _amountText;

    [Header("Tamaño del Ícono")]
    [Tooltip("Tamaño en px del ícono. (0,0) = usa el tamaño actual del Inspector.")]
    [SerializeField] private Vector2 _iconSize = new Vector2(48f, 48f);

    [Header("Lift — hotbar seleccionado")]
    [SerializeField] private float _liftPixels = 15f;
    [SerializeField] private float _liftSpeed  = 10f;

    [Header("Colores")]
    [SerializeField] private Color _normalColor   = Color.white;
    [SerializeField] private Color _selectedColor = new Color(1f, 0.85f, 0.3f);

    // Estado
    private int  _globalIndex = -1;
    private bool _isSelected;
    private bool _isDragEnabled;
    private bool _initialized;
    private bool _wasDragging;

    private Vector2 _basePosIcon;
    private Vector2 _basePosBg;

    // Estáticos compartidos entre todos los slots
    private static Image           _ghostIcon;
    private static Canvas          _rootCanvas;
    private static int             _dragSourceIndex = -1;
    private static InventorySlotUI _dragSourceSlot;

    // ── Awake: forzar configuración crítica ──────────────────

    private void Awake()
    {
        // CRÍTICO: sin esto, OnDrop nunca se dispara en este slot
        if (_background != null)
        {
            _background.raycastTarget = true;
            _basePosBg = _background.rectTransform.anchoredPosition;
        }

        if (_icon != null)
        {
            // El ícono NO bloquea raycasts (el background ya lo hace)
            _icon.raycastTarget = false;
            _basePosIcon = _icon.rectTransform.anchoredPosition;

            // Aplicar tamaño si fue configurado
            if (_iconSize.sqrMagnitude > 0.01f)
                _icon.rectTransform.sizeDelta = _iconSize;
        }

        _initialized = true;
    }

    // ── Initialize ───────────────────────────────────────────

    public void Initialize(int globalIndex, Image ghostIcon)
    {
        _globalIndex = globalIndex;

        if (ghostIcon != null)
        {
            _ghostIcon            = ghostIcon;
            _rootCanvas           = ghostIcon.canvas;
            _ghostIcon.raycastTarget = false; // el ghost no bloquea drops
            _ghostIcon.enabled    = false;

            if (_iconSize.sqrMagnitude > 0.01f)
                _ghostIcon.rectTransform.sizeDelta = _iconSize;
        }

        if (UnifiedInventory.Instance != null)
        {
            UnifiedInventory.Instance.OnSlotChanged      += HandleSlotChanged;
            UnifiedInventory.Instance.OnActiveSlotChanged += HandleActiveChanged;
        }

        Refresh();
    }

    // ── Refresh: datos → visual ──────────────────────────────

    public void Refresh()
    {
        if (UnifiedInventory.Instance == null || _globalIndex < 0) return;

        InventoryEntry entry = UnifiedInventory.Instance.GetSlot(_globalIndex);
        bool empty = entry == null || entry.IsEmpty;

        if (_icon != null)
        {
            _icon.enabled = !empty;
            if (!empty)
            {
                _icon.sprite = entry.icon;
                _icon.color  = Color.white;
            }
        }

        if (_amountText != null)
        {
            bool show = !empty && entry.amount > 1;
            _amountText.gameObject.SetActive(show);
            if (show) _amountText.text = entry.amount.ToString();
        }
    }

    // ── Update: lift animation ───────────────────────────────

    private void Update()
    {
        if (!_initialized || _icon == null || !_icon.enabled) return;

        Vector2 tIcon = _basePosIcon + (_isSelected ? Vector2.up * _liftPixels : Vector2.zero);
        _icon.rectTransform.anchoredPosition = Vector2.Lerp(
            _icon.rectTransform.anchoredPosition, tIcon,
            Time.unscaledDeltaTime * _liftSpeed);

        if (_background != null)
        {
            Vector2 tBg = _basePosBg + (_isSelected ? Vector2.up * _liftPixels : Vector2.zero);
            _background.rectTransform.anchoredPosition = Vector2.Lerp(
                _background.rectTransform.anchoredPosition, tBg,
                Time.unscaledDeltaTime * _liftSpeed);
        }
    }

    public void SetSelected(bool s)
    {
        _isSelected = s;
        if (_background != null) _background.color = s ? _selectedColor : _normalColor;
    }

    public void SetDragEnabled(bool e) => _isDragEnabled = e;

    // ── Drag & Drop ──────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (!_isDragEnabled) { e.pointerDrag = null; return; }

        var entry = UnifiedInventory.Instance?.GetSlot(_globalIndex);
        if (entry == null || entry.IsEmpty) { e.pointerDrag = null; return; }

        _dragSourceIndex = _globalIndex;
        _dragSourceSlot  = this;
        _wasDragging     = true;

        if (_ghostIcon != null && entry.icon != null)
        {
            _ghostIcon.sprite  = entry.icon;
            _ghostIcon.enabled = true;
            _ghostIcon.color   = new Color(1f, 1f, 1f, 0.75f);
            _ghostIcon.transform.SetAsLastSibling();
        }

        if (_icon != null) _icon.color = new Color(1f, 1f, 1f, 0.2f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_ghostIcon == null || !_ghostIcon.enabled || _rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            e.position,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : e.pressEventCamera,
            out Vector2 localPos);

        _ghostIcon.rectTransform.anchoredPosition = localPos;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_ghostIcon != null) _ghostIcon.enabled = false;
        if (_icon != null)      _icon.color = Color.white;
        _dragSourceIndex = -1;
        _dragSourceSlot  = null;
    }

    public void OnDrop(PointerEventData e)
    {
        // OnDrop se llama ANTES de OnEndDrag → _dragSourceIndex es válido aquí
        if (_dragSourceIndex < 0 || _dragSourceIndex == _globalIndex) return;

        UnifiedInventory.Instance?.Move(_dragSourceIndex, _globalIndex);

        _dragSourceSlot?.Refresh();
        Refresh();
    }

    // ── Click: solo si no hubo drag ──────────────────────────

    public void OnPointerClick(PointerEventData e)
    {
        if (_wasDragging) { _wasDragging = false; return; }
        if (e.button != PointerEventData.InputButton.Left) return;
        if (UnifiedInventory.Instance == null) return;
        if (!UnifiedInventory.Instance.IsHotbar(_globalIndex)) return;

        UnifiedInventory.Instance.SetActiveHotbar(_globalIndex);
    }

    // ── Listeners ────────────────────────────────────────────

    private void HandleSlotChanged(int i)  { if (i == _globalIndex) Refresh(); }
    private void HandleActiveChanged(int a) { SetSelected(a == _globalIndex); }

    private void OnDestroy()
    {
        if (UnifiedInventory.Instance == null) return;
        UnifiedInventory.Instance.OnSlotChanged       -= HandleSlotChanged;
        UnifiedInventory.Instance.OnActiveSlotChanged -= HandleActiveChanged;
    }
}
