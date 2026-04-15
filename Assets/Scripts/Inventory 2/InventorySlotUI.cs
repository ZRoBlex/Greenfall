// ============================================================
//  InventorySlotUI.cs  — v2 NUEVO SISTEMA
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/UI/
// ============================================================
//
//  QUÉ HACE:
//  Renderiza visualmente UN slot del inventario.
//  Se actualiza automáticamente cuando InventorySystem dispara OnSlotChanged.
//
//  EFECTOS DE SLOT (SlotEffectType):
//  None    → sin efecto
//  Glow    → borde brillante (coroutine de alpha pulsante)
//  Pulse   → el icono sube y baja suavemente
//  Shine   → reflejo animado sobre el icono
//  Rainbow → el borde cambia de color continuamente
//
//  DRAG & DROP:
//  Implementa IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler.
//  Funciona moviendo los slots en InventorySystem (fuente de verdad).
//
//  CÓMO CONFIGURAR EN INSPECTOR:
//  1. El prefab del slot debe tener:
//     - Image (background) como componente principal o hijo
//     - Image (_icon) para el ícono del item
//     - TextMeshPro (_amountText) para la cantidad
//     - Image opcional (_borderImage) para el borde coloreado
//     - Image opcional (_effectOverlay) para efectos de slot
//  2. Asignar esas referencias en el Inspector
//  3. Initialize() se llama desde HotbarUI o InventoryPanelUI
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS DE VISUALS (asignar en Inspector)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Visuals — asignar en prefab")]
    [Tooltip("Imagen de fondo del slot. Su color cambia según el item.")]
    [SerializeField] private Image _background;

    [Tooltip("Imagen del ícono del item. Se oculta si el slot está vacío.")]
    [SerializeField] private Image _icon;

    [Tooltip("Texto de cantidad ('x15', 'x3'). Se oculta si es 1 o vacío.")]
    [SerializeField] private TextMeshProUGUI _amountText;

    [Tooltip("Borde del slot. Su color cambia con la rareza del item. " +
             "Puede ser null si no quieres borde de rareza.")]
    [SerializeField] private Image _borderImage;

    [Tooltip("Overlay para efectos especiales (Glow, Shine, Rainbow). " +
             "Puede ser null.")]
    [SerializeField] private Image _effectOverlay;

    [Tooltip("Icono del tipo de item (pequeño en esquina). " +
             "Útil para diferenciar de un vistazo. Puede ser null.")]
    [SerializeField] private Image _typeIndicator;

    // ─────────────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN DE LOOKS
    // ─────────────────────────────────────────────────────────────────────

    [Header("Looks")]
    [Tooltip("Color del fondo cuando el slot está vacío.")]
    [SerializeField] private Color _emptyBackgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);

    [Tooltip("Color del borde cuando el slot está vacío.")]
    [SerializeField] private Color _emptyBorderColor = new Color(0.3f, 0.3f, 0.35f, 0.7f);

    [Tooltip("Color del borde cuando el slot está seleccionado (slot activo de hotbar).")]
    [SerializeField] private Color _selectedBorderColor = new Color(0.9f, 0.8f, 0.2f, 1f);

    [Tooltip("Cuánto se ilumina el slot al seleccionarlo. " +
             "El fondo se multiplica por este color.")]
    [SerializeField] private Color _selectedOverlayColor = new Color(1f, 0.95f, 0.4f, 0.15f);

    [Header("Animación de selección")]
    [Tooltip("Cuántos píxeles sube el ícono cuando el slot está seleccionado.")]
    [SerializeField] private float _selectedLiftPixels = 8f;

    [Tooltip("Velocidad del lift (interpolación). Más alto = más responsive.")]
    [SerializeField] private float _liftSpeed = 12f;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Índice global en InventorySystem (0-based, hotbar + bag)
    private int _globalIndex = -1;

    // ¿Está seleccionado (slot activo)?
    private bool _isSelected;

    // ¿Está habilitado el drag & drop?
    private bool _dragEnabled;

    // ¿Se está haciendo drag desde este slot?
    private bool _isDragging;

    // Posición base del ícono (para la animación de lift)
    private Vector2 _iconBasePos;
    private Vector2 _backgroundBasePos;

    // Referencias estáticas compartidas por todos los slots
    private static Image         _ghostIcon;      // Ícono que sigue al cursor durante drag
    private static Canvas        _rootCanvas;     // Canvas raíz para posicionamiento del ghost
    private static int           _dragSourceIndex = -1;
    private static InventorySlotUI _dragSourceSlot;

    // Coroutine del efecto actual del slot
    private Coroutine _effectCoroutine;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // El fondo DEBE tener raycastTarget = true para recibir drops
        if (_background != null) _background.raycastTarget = true;

        // El ícono NO debe tener raycastTarget (interferiría con el background)
        if (_icon != null)
        {
            _icon.raycastTarget = false;
            _iconBasePos = _icon.rectTransform.anchoredPosition;
        }

        if (_background != null)
            _backgroundBasePos = _background.rectTransform.anchoredPosition;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INITIALIZE — llamado desde HotbarUI o InventoryPanelUI
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa este slot UI con su índice global en InventorySystem.
    /// Debe llamarse una vez al crear la UI.
    ///
    /// ghostIcon: la Image que sigue al cursor durante drag.
    /// Todos los slots comparten el mismo ghostIcon.
    /// </summary>
    public void Initialize(int globalIndex, Image ghostIcon)
    {
        _globalIndex = globalIndex;

        // Configurar el ghost icon compartido
        if (ghostIcon != null && _ghostIcon == null)
        {
            _ghostIcon               = ghostIcon;
            _ghostIcon.raycastTarget = false; // El ghost no bloquea drops
            _ghostIcon.enabled       = false;
            _rootCanvas = ghostIcon.GetComponentInParent<Canvas>();
        }

        // Suscribirse a los eventos del InventorySystem
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnSlotChanged       += HandleSlotChanged;
            InventorySystem.Instance.OnActiveSlotChanged += HandleActiveSlotChanged;
        }

        Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  REFRESH — actualizar visual desde InventorySystem
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lee el slot de InventorySystem y actualiza todos los visuals.
    /// Se llama automáticamente cuando OnSlotChanged se dispara para este índice.
    /// </summary>
    public void Refresh()
    {
        if (_globalIndex < 0 || InventorySystem.Instance == null) return;

        var slot = InventorySystem.Instance.GetSlot(_globalIndex);

        if (slot.IsEmpty)
        {
            // Slot vacío
            if (_icon != null)        { _icon.enabled = false; }
            if (_amountText != null)  { _amountText.gameObject.SetActive(false); }
            if (_background != null)  { _background.color = _emptyBackgroundColor; }
            if (_borderImage != null) { _borderImage.color = _emptyBorderColor; }
            if (_typeIndicator != null) { _typeIndicator.enabled = false; }
            StopEffect();
        }
        else
        {
            // Slot con item
            var itemData = slot.itemData;

            // Ícono
            if (_icon != null)
            {
                _icon.sprite  = itemData.icon;
                _icon.color   = itemData.icon != null ? Color.white : Color.clear;
                _icon.enabled = true;
            }

            // Cantidad (solo si stackeable y > 1)
            if (_amountText != null)
            {
                bool showAmount = itemData.isStackable && slot.amount > 1;
                _amountText.gameObject.SetActive(showAmount);
                if (showAmount) _amountText.text = slot.amount.ToString();
            }

            // Fondo con color del item
            if (_background != null)
                _background.color = itemData.slotBackgroundColor;

            // Borde con color de rareza (si no está seleccionado)
            if (_borderImage != null && !_isSelected)
                _borderImage.color = itemData.slotBorderColor;

            // Indicador de tipo
            if (_typeIndicator != null)
            {
                _typeIndicator.enabled = true;
                // Podrías asignar sprites por tipo aquí si tienes un conjunto de iconos
            }

            // Efecto especial del slot
            ApplySlotEffect(itemData);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  EFECTOS DE SLOT
    // ─────────────────────────────────────────────────────────────────────

    private void ApplySlotEffect(InventoryItemData itemData)
    {
        StopEffect();

        if (itemData.slotEffect == SlotEffectType.None) return;
        if (_effectOverlay == null) return;

        _effectCoroutine = itemData.slotEffect switch
        {
            SlotEffectType.Glow    => StartCoroutine(GlowEffect(itemData)),
            SlotEffectType.Pulse   => StartCoroutine(PulseEffect(itemData)),
            SlotEffectType.Shine   => StartCoroutine(ShineEffect(itemData)),
            SlotEffectType.Rainbow => StartCoroutine(RainbowEffect(itemData.slotEffectIntensity)),
            _                      => null
        };
    }

    private void StopEffect()
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }
        if (_effectOverlay != null)
        {
            _effectOverlay.enabled = false;
            _effectOverlay.color   = Color.clear;
        }
    }

    // Borde que pulsa de alpha — sutil para épicos
    private IEnumerator GlowEffect(InventoryItemData itemData)
    {
        _effectOverlay.enabled = true;
        float intensity = itemData.slotEffectIntensity;
        while (true)
        {
            float t     = (Mathf.Sin(Time.unscaledTime * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0f, intensity * 0.6f, t);
            _effectOverlay.color = new Color(
                itemData.slotBorderColor.r,
                itemData.slotBorderColor.g,
                itemData.slotBorderColor.b,
                alpha);
            yield return null;
        }
    }

    // El ícono sube y baja — para items consumibles o especiales
    private IEnumerator PulseEffect(InventoryItemData itemData)
    {
        if (_icon == null) yield break;
        float intensity = itemData.slotEffectIntensity;
        while (true)
        {
            float t      = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
            float scaleX = Mathf.Lerp(1f, 1f + intensity * 0.1f, t);
            float scaleY = Mathf.Lerp(1f, 1f + intensity * 0.1f, t);
            _icon.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
            yield return null;
        }
    }

    // Destello que cruza el ícono horizontalmente
    private IEnumerator ShineEffect(InventoryItemData itemData)
    {
        _effectOverlay.enabled = true;
        float intensity = itemData.slotEffectIntensity;
        while (true)
        {
            // Esperar antes del siguiente destello
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(2f, 4f));

            // Animación del destello
            float duration = 0.4f;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Aparece rápido, desaparece rápido — forma de campana
                float alpha = Mathf.Sin(t * Mathf.PI) * intensity * 0.8f;
                _effectOverlay.color = new Color(1f, 1f, 1f, alpha);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _effectOverlay.color = Color.clear;
        }
    }

    // Borde que cambia de color continuamente (items legendarios)
    private IEnumerator RainbowEffect(float intensity)
    {
        _effectOverlay.enabled = true;
        float hue = 0f;
        while (true)
        {
            hue = (hue + Time.unscaledDeltaTime * 0.3f) % 1f;
            var color = Color.HSVToRGB(hue, 0.8f, 1f);
            color.a = intensity * 0.5f;
            if (_borderImage != null) _borderImage.color = color;
            if (_effectOverlay != null) _effectOverlay.color = new Color(color.r, color.g, color.b, intensity * 0.2f);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE — animación de lift
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_icon == null || !_icon.enabled) return;

        // Interpolar la posición del ícono hacia arriba si está seleccionado
        Vector2 targetPos = _iconBasePos + (_isSelected ? Vector2.up * _selectedLiftPixels : Vector2.zero);
        _icon.rectTransform.anchoredPosition = Vector2.Lerp(
            _icon.rectTransform.anchoredPosition,
            targetPos,
            Time.unscaledDeltaTime * _liftSpeed);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SELECCIÓN (slot activo de hotbar)
    // ─────────────────────────────────────────────────────────────────────

    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        if (_borderImage != null)
        {
            if (selected)
            {
                _borderImage.color = _selectedBorderColor;
            }
            else
            {
                // Restaurar color de rareza si hay item
                var slot = InventorySystem.Instance?.GetSlot(_globalIndex);
                _borderImage.color = (slot != null && !slot.IsEmpty)
                    ? slot.itemData.slotBorderColor
                    : _emptyBorderColor;
            }
        }

        if (_background != null)
        {
            var slot = InventorySystem.Instance?.GetSlot(_globalIndex);
            if (selected)
            {
                // Sumar overlay de selección al color base
                Color baseColor = (slot != null && !slot.IsEmpty)
                    ? slot.itemData.slotBackgroundColor
                    : _emptyBackgroundColor;
                _background.color = baseColor + _selectedOverlayColor;
            }
            else if (slot != null && !slot.IsEmpty)
            {
                _background.color = slot.itemData.slotBackgroundColor;
            }
            else
            {
                _background.color = _emptyBackgroundColor;
            }
        }
    }

    public void SetDragEnabled(bool enabled) => _dragEnabled = enabled;

    // ─────────────────────────────────────────────────────────────────────
    //  DRAG & DROP
    // ─────────────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (!_dragEnabled) { e.pointerDrag = null; return; }

        var slot = InventorySystem.Instance?.GetSlot(_globalIndex);
        if (slot == null || slot.IsEmpty) { e.pointerDrag = null; return; }

        _dragSourceIndex = _globalIndex;
        _dragSourceSlot  = this;
        _isDragging      = true;

        // Mostrar el ghost icon siguiendo al cursor
        if (_ghostIcon != null && slot.itemData.icon != null)
        {
            _ghostIcon.sprite  = slot.itemData.icon;
            _ghostIcon.enabled = true;
            _ghostIcon.color   = new Color(1f, 1f, 1f, 0.75f);
            _ghostIcon.transform.SetAsLastSibling(); // Encima de todo
        }

        // Oscurecer el ícono original
        if (_icon != null) _icon.color = new Color(1f, 1f, 1f, 0.25f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_ghostIcon == null || !_ghostIcon.enabled || _rootCanvas == null) return;

        // Mover el ghost icon a la posición del cursor
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
        _isDragging      = false;
    }

    public void OnDrop(PointerEventData e)
    {
        // OnDrop se llama en el slot DESTINO cuando sueltas el drag sobre él
        if (_dragSourceIndex < 0 || _dragSourceIndex == _globalIndex) return;

        // Mover en InventorySystem (fuente de verdad)
        InventorySystem.Instance?.MoveSlot(_dragSourceIndex, _globalIndex);

        // La UI se actualiza automáticamente por OnSlotChanged
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLICK
    // ─────────────────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData e)
    {
        // Ignorar si fue drag (OnEndDrag ya lo manejó)
        if (_isDragging) return;
        if (e.button != PointerEventData.InputButton.Left) return;
        if (InventorySystem.Instance == null) return;
        if (!InventorySystem.Instance.IsHotbarSlot(_globalIndex)) return;

        InventorySystem.Instance.SetActiveHotbarSlot(_globalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HOVER (tooltip futuro)
    // ─────────────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        // Aquí se puede mostrar un tooltip con descripción del item
        // Por ahora solo un log de debug
        // var slot = InventorySystem.Instance?.GetSlot(_globalIndex);
        // if (slot != null && !slot.IsEmpty)
        //     InventoryTooltip.Instance?.Show(slot.itemData, transform.position);
    }

    public void OnPointerExit(PointerEventData e)
    {
        // InventoryTooltip.Instance?.Hide();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HANDLERS DE EVENTOS
    // ─────────────────────────────────────────────────────────────────────

    private void HandleSlotChanged(int changedIndex)
    {
        if (changedIndex == _globalIndex) Refresh();
    }

    private void HandleActiveSlotChanged(int activeIndex)
    {
        SetSelected(activeIndex == _globalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        StopEffect();
        if (_icon != null)
            _icon.rectTransform.localScale = Vector3.one;

        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnSlotChanged       -= HandleSlotChanged;
        InventorySystem.Instance.OnActiveSlotChanged -= HandleActiveSlotChanged;
    }
}