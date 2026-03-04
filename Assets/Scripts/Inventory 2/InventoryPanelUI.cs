// ============================================================
// InventoryPanelUI.cs  — VERSIÓN FINAL
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Greenfall.Inventory;

public class InventoryPanelUI : MonoBehaviour
{
    [Header("Estructura")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private HotbarUI   _hotbarUI;
    [SerializeField] private Image      _ghostIcon;

    [Header("Slots del panel")]
    [Tooltip("Slots de HOTBAR dentro del panel (mismos índices que la hotbar visible).")]
    [SerializeField] private InventorySlotUI[] _hotbarSlotsInPanel;
    [Tooltip("Slots de BOLSA dentro del panel.")]
    [SerializeField] private InventorySlotUI[] _bagSlots;

    [Header("Input")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;

    [Header("Cursor")]
    [SerializeField] private bool _freeCursorOnOpen = true;

    [Header("Fade (opcional — CanvasGroup en el panel)")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeSpeed = 10f;

    private bool  _isOpen;
    private float _targetAlpha;

    // ── Awake ────────────────────────────────────────────────

    private void Awake()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            _targetAlpha = 0f;
        }

        if (_ghostIcon != null) _ghostIcon.enabled = false;
    }

    private void Start()
    {
        if (UnifiedInventory.Instance == null)
        {
            Debug.LogError("[InventoryPanelUI] No hay UnifiedInventory en escena.");
            return;
        }

        InitSlots();
    }

    private void InitSlots()
    {
        int hotbarSize = UnifiedInventory.Instance.HotbarSize;
        int bagSize    = UnifiedInventory.Instance.BagSize;

        for (int i = 0; i < _hotbarSlotsInPanel.Length && i < hotbarSize; i++)
        {
            _hotbarSlotsInPanel[i].Initialize(
                UnifiedInventory.Instance.HotbarIndex(i), _ghostIcon);
            _hotbarSlotsInPanel[i].SetDragEnabled(false);
        }

        for (int i = 0; i < _bagSlots.Length && i < bagSize; i++)
        {
            _bagSlots[i].Initialize(
                UnifiedInventory.Instance.BagIndex(i), _ghostIcon);
            _bagSlots[i].SetDragEnabled(false);
        }
    }

    // ── Update ───────────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            Toggle();

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();

        // Fade usa unscaledDeltaTime para funcionar con timeScale = 0
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = Mathf.Lerp(
                _canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.unscaledDeltaTime);

            // Ocultar panel cuando el fade de cierre termina
            if (!_isOpen && _canvasGroup.alpha < 0.01f && _panelRoot != null && _panelRoot.activeSelf)
            {
                _panelRoot.SetActive(false);
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }

    // ── Abrir / Cerrar ───────────────────────────────────────

    public void Toggle() => (_isOpen ? (System.Action)Close : Open)();

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (_panelRoot != null) _panelRoot.SetActive(true);

        if (_canvasGroup != null)
        {
            _targetAlpha                = 1f;
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
        }

        SetAllDragEnabled(true);
        _hotbarUI?.SetDragEnabled(true);

        // PAUSA TOTAL
        Time.timeScale = 0f;

        if (_freeCursorOnOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_canvasGroup != null)
        {
            _targetAlpha              = 0f;
            _canvasGroup.interactable = false;
            // blocksRaycasts se apaga en Update cuando el fade termina
        }
        else if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }

        SetAllDragEnabled(false);
        _hotbarUI?.SetDragEnabled(false);

        // REANUDAR JUEGO
        Time.timeScale = 1f;

        if (_freeCursorOnOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    private void SetAllDragEnabled(bool e)
    {
        foreach (var s in _hotbarSlotsInPanel) s.SetDragEnabled(e);
        foreach (var s in _bagSlots)           s.SetDragEnabled(e);
    }

    public bool IsOpen => _isOpen;

    private void OnDestroy()
    {
        if (_isOpen) Time.timeScale = 1f;
    }
}
