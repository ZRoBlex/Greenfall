// ============================================================
//  HotbarUI.cs — v2 NUEVO SISTEMA
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/UI/
// ============================================================
//
//  QUÉ HACE:
//  Maneja la barra de hotbar visible en pantalla.
//  Gestiona input de teclado (1-9), scroll wheel y la tecla R de recarga.
//
//  CONFIGURACIÓN:
//  1. Crea los slots hijos (GameObject con InventorySlotUI)
//  2. Asígnalos al array _slots en el Inspector
//  3. Asigna el _ghostIcon (una Image en el mismo Canvas, encima de todo)
//  4. HotbarUI llama Initialize() en cada slot automáticamente
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────────────────────────────

    [Header("Slots (asignar en Inspector)")]
    [Tooltip("Array de InventorySlotUI. Deben estar en orden de slot 0 a N.")]
    [SerializeField] private InventorySlotUI[] _slots;

    [Header("Ghost Icon para Drag")]
    [Tooltip("Una Image en el Canvas que sigue al cursor durante drag & drop. " +
             "Debe estar en el Canvas raíz, encima de todo (último en jerarquía).")]
    [SerializeField] private Image _ghostIcon;

    [Header("Input")]
    [Tooltip("Cooldown entre cambios por scroll wheel (segundos). " +
             "Evita cambios accidentales muy rápidos.")]
    [Range(0.05f, 0.5f)] [SerializeField] private float _scrollCooldown = 0.12f;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────────────────────────────────

    private float _lastScrollTime;

    // ─────────────────────────────────────────────────────────────────────
    //  START
    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[HotbarUI] No hay InventorySystem en escena.", this);
            return;
        }

        // Inicializar cada slot con su índice global correspondiente
        int hotbarSize = InventorySystem.Instance.HotbarSize;
        for (int i = 0; i < _slots.Length && i < hotbarSize; i++)
        {
            _slots[i].Initialize(
                InventorySystem.Instance.HotbarToGlobal(i),
                _ghostIcon);
            _slots[i].SetDragEnabled(false); // Drag solo cuando el inventario está abierto
        }

        // Suscribirse a cambio de slot activo para actualizar visuals
        InventorySystem.Instance.OnActiveSlotChanged += OnActiveSlotChanged;

        // Seleccionar el primer slot al inicio
        InventorySystem.Instance.SetActiveHotbarSlot(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE — Input
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        HandleNumberKeys();
        HandleScrollWheel();
        HandleReloadKey();
        HandleDropKey();
    }

    // Teclas 1-9 para seleccionar slot directamente
    private void HandleNumberKeys()
    {
        for (int i = 0; i < _slots.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                InventorySystem.Instance.SetActiveHotbarSlot(i);
        }
    }

    // Scroll wheel para ciclar por la hotbar
    private void HandleScrollWheel()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        _lastScrollTime = Time.time;

        // Scroll hacia arriba = slot anterior, hacia abajo = siguiente
        if (scroll > 0) InventorySystem.Instance.SelectPreviousHotbarSlot();
        else            InventorySystem.Instance.SelectNextHotbarSlot();
    }

    // R = recargar arma actual
    private void HandleReloadKey()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;

        var activeSlot = InventorySystem.Instance.ActiveSlot;
        if (activeSlot == null || activeSlot.IsEmpty) return;
        if (activeSlot.weaponInstance == null) return;

        activeSlot.weaponInstance.ReloadFromInventory();
    }

    // G = tirar item del slot activo
    private void HandleDropKey()
    {
        if (!Input.GetKeyDown(KeyCode.G)) return;
        InventorySystem.Instance.DropActiveItem();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HANDLERS DE EVENTOS
    // ─────────────────────────────────────────────────────────────────────

    private void OnActiveSlotChanged(int activeGlobalIndex)
    {
        int hotbarSize = InventorySystem.Instance.HotbarSize;
        for (int i = 0; i < _slots.Length && i < hotbarSize; i++)
            _slots[i].SetSelected(InventorySystem.Instance.HotbarToGlobal(i) == activeGlobalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Habilita o deshabilita el drag & drop en todos los slots de la hotbar.
    /// Se llama desde InventoryPanelUI cuando el panel se abre/cierra.
    /// </summary>
    public void SetDragEnabled(bool enabled)
    {
        foreach (var slot in _slots)
            slot.SetDragEnabled(enabled);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnActiveSlotChanged -= OnActiveSlotChanged;
    }
}