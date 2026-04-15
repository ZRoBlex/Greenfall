// ============================================================
//  GF_InventoryUI.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Genera toda la UI del inventario desde código.
//  No necesitas crear ningún prefab de UI.
//
//  SETUP:
//  1. Agrega GF_InventoryUI al mismo GameObject que GF_Inventory
//  2. Asigna el GF_InventoryConfig en el Inspector
//  3. (Opcional) Asigna weaponAdapter si quieres drop/reload desde la UI
//  4. En Play Mode verás la hotbar y podrás abrir el inventario con Tab
//
//  PARA PREVISUALIZAR EN EDITOR:
//  Menú Greenfall → Inventory Editor → botón "Generar UI en Escena"
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GF_InventoryUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS (asignar en Inspector)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Config")]
    public GF_InventoryConfig config;
    public GF_WeaponAdapter   weaponAdapter;

    [Header("Canvas (opcional — crea uno si está vacío)")]
    [Tooltip("Canvas donde se genera la UI. Si está vacío, crea uno nuevo.")]
    public Canvas targetCanvas;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Slots de UI
    private GF_SlotWidget[] _hotbarSlots;
    private GF_SlotWidget[] _bagSlots;

    // Paneles
    private GameObject _hotbarPanel;
    private GameObject _bagPanel;
    private CanvasGroup _bagCG;

    // Ghost drag icon
    private Image _ghostIcon;

    // Estado
    private bool _bagOpen;
    private float _bagTargetAlpha;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[GF_InventoryUI] Sin GF_InventoryConfig. Asigna uno.", this);
            enabled = false;
            return;
        }
        BuildUI();
    }

    private void Start()
    {
        if (GF_Inventory.Instance == null) return;

        GF_Inventory.Instance.OnSlotChanged        += HandleSlotChanged;
        GF_Inventory.Instance.OnActiveSlotChanged  += HandleActiveChanged;
        GF_Inventory.Instance.OnGameStateChanged   += HandleGameState;

        // Forzar refresco inicial de todos los slots
        for (int i = 0; i < GF_Inventory.Instance.TotalSlots; i++)
            HandleSlotChanged(i);

        HandleActiveChanged(GF_Inventory.Instance.ActiveIndex,
                            GF_Inventory.Instance.ActiveIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE — Input
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleInput();
        AnimateBagPanel();
    }

    private void HandleInput()
    {
        if (GF_Inventory.Instance == null) return;

        KeyCode toggle = config.toggleInventoryKey;
        KeyCode drop   = config.dropKey;
        KeyCode reload = config.reloadKey;

        // Teclas 1-9 de hotbar
        for (int i = 0; i < config.hotbarSlotCount && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                GF_Inventory.Instance.SetActive(i);

        // Scroll wheel
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (scroll > 0) GF_Inventory.Instance.SelectPrev();
            else            GF_Inventory.Instance.SelectNext();
        }

        // Tab: abrir/cerrar inventario
        if (Input.GetKeyDown(toggle)) ToggleBag();

        // G: drop
        if (Input.GetKeyDown(drop) && !_bagOpen)
        {
            var slot = GF_Inventory.Instance.ActiveSlot;
            if (slot != null && !slot.IsEmpty)
            {
                if (slot.definition.type == GF_ItemType.Weapon)
                    weaponAdapter?.DropCurrentWeapon();
                else
                    GF_Inventory.Instance.DropActive();
            }
        }

        // R: recargar
        if (Input.GetKeyDown(reload) && !_bagOpen)
            weaponAdapter?.ReloadCurrentWeapon();

        // Esc: cerrar inventario
        if (_bagOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseBag();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ABRIR / CERRAR INVENTARIO
    // ─────────────────────────────────────────────────────────────────────

    public void ToggleBag()
    {
        if (_bagOpen) CloseBag();
        else          OpenBag();
    }

    public void OpenBag()
    {
        if (_bagOpen) return;
        _bagOpen        = true;
        _bagTargetAlpha = 1f;

        if (_bagPanel != null)   _bagPanel.SetActive(true);
        if (_bagCG != null)      { _bagCG.interactable = true; _bagCG.blocksRaycasts = true; }

        // Habilitar drag en todos los slots
        SetAllDragEnabled(true);

        // Pausar juego, liberar cursor
        GF_Inventory.Instance?.SetGameState(GF_GameState.InventoryOpen);
        Time.timeScale      = 0f;
        Cursor.lockState    = CursorLockMode.None;
        Cursor.visible      = true;
    }

    public void CloseBag()
    {
        if (!_bagOpen) return;
        _bagOpen        = false;
        _bagTargetAlpha = 0f;

        if (_bagCG != null) { _bagCG.interactable = false; _bagCG.blocksRaycasts = false; }

        SetAllDragEnabled(false);

        GF_Inventory.Instance?.SetGameState(GF_GameState.Playing);
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void AnimateBagPanel()
    {
        if (_bagCG == null) return;

        _bagCG.alpha = Mathf.Lerp(_bagCG.alpha, _bagTargetAlpha,
                                   config.fadeSpeed * Time.unscaledDeltaTime);

        // Ocultar cuando fade termina
        if (!_bagOpen && _bagCG.alpha < 0.01f && _bagPanel != null && _bagPanel.activeSelf)
            _bagPanel.SetActive(false);
    }

    private void SetAllDragEnabled(bool on)
    {
        if (_hotbarSlots != null) foreach (var s in _hotbarSlots) s.SetDragEnabled(on);
        if (_bagSlots    != null) foreach (var s in _bagSlots)    s.SetDragEnabled(on);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HANDLERS
    // ─────────────────────────────────────────────────────────────────────

    private void HandleSlotChanged(int globalIndex)
    {
        if (GF_Inventory.Instance == null) return;

        if (GF_Inventory.Instance.IsHotbarSlot(globalIndex))
        {
            int local = globalIndex;
            if (_hotbarSlots != null && local < _hotbarSlots.Length)
                _hotbarSlots[local].Refresh();
        }
        else
        {
            int local = globalIndex - GF_Inventory.Instance.HotbarSize;
            if (_bagSlots != null && local >= 0 && local < _bagSlots.Length)
                _bagSlots[local].Refresh();
        }
    }

    private void HandleActiveChanged(int newIndex, int prevIndex)
    {
        if (_hotbarSlots == null) return;

        for (int i = 0; i < _hotbarSlots.Length; i++)
            _hotbarSlots[i].SetSelected(i == newIndex);
    }

    private void HandleGameState(GF_GameState state)
    {
        // Si alguien externo pausa el juego mientras el inventario está abierto, cerrarlo
        if (state == GF_GameState.Paused && _bagOpen)
            CloseBag();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CONSTRUCCIÓN DE UI DESDE CÓDIGO
    // ─────────────────────────────────────────────────────────────────────

    public void BuildUI()
    {
        EnsureCanvas();
        EnsureEventSystem();

        // Ghost icon (compartido por todos los slots para drag)
        _ghostIcon = CreateGhostIcon();

        // Hotbar
        _hotbarPanel = CreateHotbarPanel();
        _hotbarSlots = CreateSlotGrid(_hotbarPanel.transform, config.hotbarSlotCount, 1,
                                       0, GF_Inventory.Instance?.HotbarSize ?? config.hotbarSlotCount);

        // Panel de bolsa
        _bagPanel = CreateBagPanel();
        _bagCG    = _bagPanel.AddComponent<CanvasGroup>();
        _bagCG.alpha = 0f; _bagCG.interactable = false; _bagCG.blocksRaycasts = false;

        var bagContent = CreateBagContent(_bagPanel);
        _bagSlots = CreateSlotGrid(bagContent.transform, config.bagColumns, config.bagRows,
                                    GF_Inventory.Instance?.HotbarSize ?? config.hotbarSlotCount,
                                    (GF_Inventory.Instance?.TotalSlots ?? (config.hotbarSlotCount + config.bagColumns * config.bagRows)) - (GF_Inventory.Instance?.HotbarSize ?? config.hotbarSlotCount));

        _bagPanel.SetActive(false);

        // Registrar el ghost en todos los slots
        foreach (var s in _hotbarSlots) s.RegisterGhost(_ghostIcon, GetComponent<Canvas>() ?? targetCanvas);
        if (_bagSlots != null) foreach (var s in _bagSlots) s.RegisterGhost(_ghostIcon, GetComponent<Canvas>() ?? targetCanvas);
    }

    // ── Canvas ────────────────────────────────────────────────────────────

    private void EnsureCanvas()
    {
        if (targetCanvas != null) return;

        var existing = FindFirstObjectByType<Canvas>();
        if (existing != null) { targetCanvas = existing; return; }

        var cgo  = new GameObject("GF_Canvas");
        var c    = cgo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        cgo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cgo.AddComponent<GraphicRaycaster>();
        targetCanvas = c;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eso = new GameObject("EventSystem");
            eso.AddComponent<EventSystem>();
            eso.AddComponent<StandaloneInputModule>();
        }
    }

    // ── Ghost Icon ────────────────────────────────────────────────────────

    private Image CreateGhostIcon()
    {
        var go    = CreateUIObject("GF_GhostIcon", targetCanvas.transform);
        var img   = go.AddComponent<Image>();
        var rt    = go.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.one * config.slotSize * 0.85f;
        img.raycastTarget = false;
        img.enabled       = false;
        img.color         = new Color(1f, 1f, 1f, 0.75f);
        go.transform.SetAsLastSibling();
        return img;
    }

    // ── Hotbar Panel ──────────────────────────────────────────────────────

    private GameObject CreateHotbarPanel()
    {
        var panel = CreateUIObject("GF_Hotbar", targetCanvas.transform);
        var rt    = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);

        float totalW = config.hotbarSlotCount * config.slotSize
                     + (config.hotbarSlotCount - 1) * config.slotSpacing
                     + 8f;
        rt.sizeDelta = new Vector2(totalW, config.slotSize + 8f);
        rt.anchoredPosition = config.hotbarOffset;

        // Fondo translúcido
        var bg    = panel.AddComponent<Image>();
        bg.color  = new Color(config.panelBgColor.r, config.panelBgColor.g,
                               config.panelBgColor.b, config.panelBgColor.a * 0.7f);

        // Layout horizontal
        var hlg       = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing   = config.slotSpacing;
        hlg.padding   = new RectOffset(4, 4, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        return panel;
    }

    // ── Bag Panel ─────────────────────────────────────────────────────────

    private GameObject CreateBagPanel()
    {
        var panel = CreateUIObject("GF_BagPanel", targetCanvas.transform);
        var rt    = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * 0.5f;

        float w = config.bagColumns * config.slotSize + (config.bagColumns - 1) * config.slotSpacing + config.bagPanelPadding.x * 2f;
        float h = config.bagRows    * config.slotSize + (config.bagRows    - 1) * config.slotSpacing + config.bagPanelPadding.y * 2f + 40f; // 40px for title

        rt.sizeDelta      = new Vector2(w, h);
        rt.anchoredPosition = config.bagPanelOffset;

        // Fondo
        var bg   = panel.AddComponent<Image>();
        bg.color = config.panelBgColor;

        // Borde (outline using a slightly larger transparent image behind, or simple color)
        // Simple: usar un Outline component
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = config.panelBorderColor;
        outline.effectDistance = Vector2.one * config.panelBorderWidth;

        return panel;
    }

    private GameObject CreateBagContent(GameObject bagPanel)
    {
        // Título
        var titleGO = CreateUIObject("BagTitle", bagPanel.transform);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -8f);
        titleRT.sizeDelta        = new Vector2(0f, 24f);

        var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "INVENTARIO";
        titleTmp.fontSize  = 14;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color     = Color.white;
        if (config.font != null) titleTmp.font = config.font;

        // Contenedor del grid
        var content = CreateUIObject("BagContent", bagPanel.transform);
        var rt      = content.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(config.bagPanelPadding.x, config.bagPanelPadding.y);
        rt.offsetMax = new Vector2(-config.bagPanelPadding.x, -(config.bagPanelPadding.y + 32f));

        var glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize  = Vector2.one * config.slotSize;
        glg.spacing   = Vector2.one * config.slotSpacing;
        glg.startCorner      = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis        = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment   = TextAnchor.UpperLeft;
        glg.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount  = config.bagColumns;

        return content;
    }

    // ── Grids de Slots ────────────────────────────────────────────────────

    private GF_SlotWidget[] CreateSlotGrid(Transform parent, int cols, int rows,
                                            int globalIndexStart, int count)
    {
        var widgets = new List<GF_SlotWidget>();

        for (int i = 0; i < count; i++)
        {
            var slotGO = CreateUIObject($"Slot_{globalIndexStart + i}", parent);
            var rt     = slotGO.GetComponent<RectTransform>();
            rt.sizeDelta = Vector2.one * config.slotSize;

            var widget = slotGO.AddComponent<GF_SlotWidget>();
            widget.Initialize(globalIndexStart + i, config);
            widgets.Add(widget);
        }

        return widgets.ToArray();
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_bagOpen)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        if (GF_Inventory.Instance == null) return;
        GF_Inventory.Instance.OnSlotChanged       -= HandleSlotChanged;
        GF_Inventory.Instance.OnActiveSlotChanged -= HandleActiveChanged;
        GF_Inventory.Instance.OnGameStateChanged  -= HandleGameState;
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  GF_SlotWidget — Visual de un slot individual
//  (mismo archivo para simplicidad, podría separarse)
// ══════════════════════════════════════════════════════════════════════════

public class GF_SlotWidget : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler
{
    private int              _index;
    private GF_InventoryConfig _cfg;

    // Visuals
    private Image           _bg;
    private Image           _icon;
    private Image           _border;
    private TextMeshProUGUI _amountTxt;

    // Animación
    private bool   _selected;
    private Vector2 _iconBasePos;
    private Coroutine _effectCR;

    // Drag estático (compartido)
    private static Image          _ghost;
    private static Canvas         _rootCanvas;
    private static int            _dragFromIndex = -1;
    private static GF_SlotWidget  _dragFromSlot;
    private bool _isDragging;
    private bool _dragEnabled;

    // ── Init ──────────────────────────────────────────────────────────────

    public void Initialize(int globalIndex, GF_InventoryConfig cfg)
    {
        _index = globalIndex;
        _cfg   = cfg;
        BuildVisuals();
        Refresh();

        if (GF_Inventory.Instance != null)
        {
            GF_Inventory.Instance.OnSlotChanged       += i => { if (i == _index) Refresh(); };
            GF_Inventory.Instance.OnActiveSlotChanged += (n,_) => SetSelected(n == _index && GF_Inventory.Instance.IsHotbarSlot(_index));
        }
    }

    public void RegisterGhost(Image ghost, Canvas canvas)
    {
        _ghost      = ghost;
        _rootCanvas = canvas;
    }

    // ── Build Visuals ─────────────────────────────────────────────────────

    private void BuildVisuals()
    {
        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.one * _cfg.slotSize;

        // Fondo
        _bg = gameObject.AddComponent<Image>();
        _bg.color = _cfg.emptySlotBg;
        _bg.raycastTarget = true;

        // Borde
        var borderGO  = new GameObject("Border");
        borderGO.transform.SetParent(transform, false);
        var borderRT  = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero; borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = Vector2.zero; borderRT.offsetMax = Vector2.zero;
        _border = borderGO.AddComponent<Image>();
        _border.color = _cfg.emptySlotBorder;
        _border.raycastTarget = false;
        var borderOutline = borderGO.AddComponent<Outline>();
        borderOutline.effectColor    = _cfg.emptySlotBorder;
        borderOutline.effectDistance = Vector2.one * 1f;
        Destroy(borderOutline); // Solo queremos la image para el color de borde

        // Icono
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(transform, false);
        var iconRT  = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
        float pad = _cfg.iconPadding;
        iconRT.offsetMin = Vector2.one * pad; iconRT.offsetMax = -Vector2.one * pad;
        _icon = iconGO.AddComponent<Image>();
        _icon.raycastTarget = false;
        _icon.enabled       = false;
        _iconBasePos = iconRT.anchoredPosition;

        // Cantidad
        var amtGO = new GameObject("Amount");
        amtGO.transform.SetParent(transform, false);
        var amtRT = amtGO.AddComponent<RectTransform>();
        amtRT.anchorMin = new Vector2(0f, 0f); amtRT.anchorMax = new Vector2(1f, 0.4f);
        amtRT.offsetMin = Vector2.zero; amtRT.offsetMax = Vector2.zero;
        _amountTxt = amtGO.AddComponent<TextMeshProUGUI>();
        _amountTxt.alignment = TextAlignmentOptions.BottomRight;
        _amountTxt.fontSize  = _cfg.amountTextSize;
        _amountTxt.color     = _cfg.amountTextColor;
        if (_cfg.font != null) _amountTxt.font = _cfg.font;
        _amountTxt.gameObject.SetActive(false);
    }

    // ── Refresh ───────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (GF_Inventory.Instance == null) return;

        var slot = GF_Inventory.Instance.GetSlot(_index);

        if (slot.IsEmpty)
        {
            _icon.enabled = false;
            _amountTxt.gameObject.SetActive(false);
            _bg.color = _selected
                ? _cfg.emptySlotBg + _cfg.selectedBgOverlay
                : _cfg.emptySlotBg;
            _border.color = _selected ? _cfg.selectedBorder : _cfg.emptySlotBorder;
            StopEffect();
        }
        else
        {
            var def = slot.definition;

            _icon.sprite  = def.icon;
            _icon.color   = def.icon != null ? Color.white : Color.clear;
            _icon.enabled = true;

            bool showAmt = def.isStackable && slot.amount > 1;
            _amountTxt.gameObject.SetActive(showAmt);
            if (showAmt) _amountTxt.text = slot.amount.ToString();

            _bg.color     = _selected ? def.slotBgColor + _cfg.selectedBgOverlay : def.slotBgColor;
            _border.color = _selected ? _cfg.selectedBorder : def.slotBorderColor;

            if (!_selected) StartEffect(def);
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────

    public void SetSelected(bool on)
    {
        _selected = on;
        Refresh();
    }

    public void SetDragEnabled(bool on) => _dragEnabled = on;

    // ── Update (lift animation) ────────────────────────────────────────────

    private void Update()
    {
        if (_icon == null || !_icon.enabled) return;

        var rt = _icon.GetComponent<RectTransform>();
        Vector2 target = _iconBasePos + (_selected ? Vector2.up * _cfg.selectedLiftPx : Vector2.zero);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, target,
                                            _cfg.liftSpeed * Time.unscaledDeltaTime);
    }

    // ── Effects ───────────────────────────────────────────────────────────

    private void StartEffect(GF_ItemDefinition def)
    {
        StopEffect();
        if (def.slotEffect == GF_SlotEffect.None) return;
        _effectCR = def.slotEffect switch
        {
            GF_SlotEffect.Glow    => StartCoroutine(GlowCR(def)),
            GF_SlotEffect.Pulse   => StartCoroutine(PulseCR(def)),
            GF_SlotEffect.Rainbow => StartCoroutine(RainbowCR(def)),
            _                     => null
        };
    }

    private void StopEffect()
    {
        if (_effectCR != null) { StopCoroutine(_effectCR); _effectCR = null; }
    }

    private IEnumerator GlowCR(GF_ItemDefinition def)
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.unscaledTime * 2f) + 1f) * 0.5f;
            var c = def.slotBorderColor;
            c.a = Mathf.Lerp(0.3f, 1f, t) * def.effectIntensity;
            _border.color = c;
            yield return null;
        }
    }

    private IEnumerator PulseCR(GF_ItemDefinition def)
    {
        var rt = _icon.GetComponent<RectTransform>();
        while (true)
        {
            float t = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
            float s = Mathf.Lerp(1f, 1f + def.effectIntensity * 0.12f, t);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
    }

    private IEnumerator RainbowCR(GF_ItemDefinition def)
    {
        float h = 0f;
        while (true)
        {
            h = (h + Time.unscaledDeltaTime * 0.4f) % 1f;
            var c = Color.HSVToRGB(h, 0.9f, 1f);
            c.a = def.effectIntensity;
            _border.color = c;
            yield return null;
        }
    }

    // ── Drag & Drop ───────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (!_dragEnabled) { e.pointerDrag = null; return; }
        var slot = GF_Inventory.Instance?.GetSlot(_index);
        if (slot == null || slot.IsEmpty) { e.pointerDrag = null; return; }

        _dragFromIndex = _index;
        _dragFromSlot  = this;
        _isDragging    = true;

        if (_ghost != null && slot.definition.icon != null)
        {
            _ghost.sprite  = slot.definition.icon;
            _ghost.enabled = true;
            _ghost.transform.SetAsLastSibling();
        }
        _icon.color = new Color(1f, 1f, 1f, 0.2f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_ghost == null || !_ghost.enabled || _rootCanvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform, e.position,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : e.pressEventCamera,
            out Vector2 local);
        _ghost.rectTransform.anchoredPosition = local;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_ghost != null) _ghost.enabled = false;
        _icon.color    = Color.white;
        _dragFromIndex = -1;
        _dragFromSlot  = null;
        _isDragging    = false;
    }

    public void OnDrop(PointerEventData e)
    {
        if (_dragFromIndex < 0 || _dragFromIndex == _index) return;
        GF_Inventory.Instance?.MoveSlot(_dragFromIndex, _index);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (_isDragging) return;
        if (e.button != PointerEventData.InputButton.Left) return;
        if (GF_Inventory.Instance == null) return;
        if (!GF_Inventory.Instance.IsHotbarSlot(_index)) return;
        GF_Inventory.Instance.SetActive(_index);
    }

    private void OnDestroy()
    {
        StopEffect();
        if (_icon != null) _icon.rectTransform.localScale = Vector3.one;
    }
}