// ============================================================
// HotbarUI.cs
// Carpeta: Scripts/Inventory/UI/
// ------------------------------------------------------------
// La HOTBAR siempre visible en la parte inferior de la pantalla.
// Contiene los slots 0..hotbarSize-1 del UnifiedInventory.
//
// RESPONSABILIDADES:
// - Mostrar los InventorySlotUI de la hotbar
// - Leer input de cambio de slot (teclas 1-5, scroll)
// - Cuando el inventario se abre, habilitar drag en sus slots
// - Cuando el inventario se cierra, deshabilitar drag
//
// CONFIGURACIÓN EN UNITY:
// 1. Crear un panel de UI (Canvas → Panel "Hotbar")
// 2. Dentro, crear N hijos → cada uno con InventorySlotUI
// 3. Asignar esos hijos al array _slots de este script
// 4. Asignar el ghost icon desde InventoryPanelUI
// ============================================================

using UnityEngine;
using Greenfall.Inventory;

public class HotbarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("Slots de Hotbar")]
    [Tooltip("Los InventorySlotUI de la hotbar, en orden de índice.")]
    [SerializeField] private InventorySlotUI[] _slots;

    [Header("Input")]
    [Tooltip("Delay mínimo entre scrolls. Evita cambiar 3 slots de golpe.")]
    [SerializeField] private float _scrollCooldown = 0.12f;

    [Header("Ghost Icon")]
    [Tooltip("Image compartida en el Canvas raíz para el drag ghost. Asignar desde InventoryPanelUI.")]
    [SerializeField] private UnityEngine.UI.Image _ghostIcon;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    private float _lastScrollTime;
    private bool  _dragEnabled; // controlado por InventoryPanelUI

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────

    private void Start()
    {
        if (UnifiedInventory.Instance == null)
        {
            Debug.LogError("[HotbarUI] No hay UnifiedInventory en escena.");
            return;
        }

        int hotbarSize = UnifiedInventory.Instance.HotbarSize;

        for (int i = 0; i < _slots.Length && i < hotbarSize; i++)
        {
            int globalIndex = UnifiedInventory.Instance.HotbarIndex(i);
            _slots[i].Initialize(globalIndex, _ghostIcon);
            _slots[i].SetDragEnabled(false); // Empieza con drag desactivado
        }

        // Suscribirse al cambio de slot activo para la animación de selección
        UnifiedInventory.Instance.OnActiveSlotChanged += OnActiveSlotChanged;

        // Seleccionar el primero por defecto
        if (hotbarSize > 0)
            UnifiedInventory.Instance.SetActiveHotbar(0);
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE — input de cambio de slot
    // ─────────────────────────────────────────────────────────

    private void Update()
    {
        HandleNumberKeys();
        HandleScroll();
    }

    private void HandleNumberKeys()
    {
        // Teclas 1-5 (o cuantos slots haya)
        for (int i = 0; i < _slots.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                UnifiedInventory.Instance.SetActiveHotbar(i);
                return;
            }
        }
    }

    private void HandleScroll()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        _lastScrollTime = Time.time;

        int current = UnifiedInventory.Instance.ActiveHotbarSlot;
        int size    = UnifiedInventory.Instance.HotbarSize;

        // scroll positivo = hacia arriba = slot anterior
        int next = scroll > 0
            ? (current - 1 + size) % size
            : (current + 1) % size;

        UnifiedInventory.Instance.SetActiveHotbar(next);
    }

    // ─────────────────────────────────────────────────────────
    // HABILITAR / DESHABILITAR DRAG
    // Llamado por InventoryPanelUI al abrir/cerrar
    // ─────────────────────────────────────────────────────────

    public void SetDragEnabled(bool enabled)
    {
        _dragEnabled = enabled;
        foreach (var slot in _slots)
            slot.SetDragEnabled(enabled);
    }

    // ─────────────────────────────────────────────────────────
    // LISTENERS
    // ─────────────────────────────────────────────────────────

    private void OnActiveSlotChanged(int activeGlobal)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            bool selected = UnifiedInventory.Instance.HotbarIndex(i) == activeGlobal;
            _slots[i].SetSelected(selected);
        }
    }

    private void OnDestroy()
    {
        if (UnifiedInventory.Instance != null)
            UnifiedInventory.Instance.OnActiveSlotChanged -= OnActiveSlotChanged;
    }
}
