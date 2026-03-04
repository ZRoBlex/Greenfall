// ============================================================
// HotbarUI.cs  — VERSIÓN LEGACY INPUT
// ============================================================

using UnityEngine;
using Greenfall.Inventory;

public class HotbarUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private InventorySlotUI[] _slots;

    [Header("Input")]
    [SerializeField] private float _scrollCooldown = 0.12f;

    [Header("Ghost Icon")]
    [SerializeField] private UnityEngine.UI.Image _ghostIcon;

    private float _lastScrollTime;

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
            _slots[i].Initialize(UnifiedInventory.Instance.HotbarIndex(i), _ghostIcon);
            _slots[i].SetDragEnabled(false);
        }

        UnifiedInventory.Instance.OnActiveSlotChanged += OnActiveSlotChanged;

        if (hotbarSize > 0)
            UnifiedInventory.Instance.SetActiveHotbar(0);
    }

    private void Update()
    {
        HandleNumberKeys();
        HandleScroll();
        HandleReload();
    }

    private void HandleNumberKeys()
    {
        for (int i = 0; i < _slots.Length && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                UnifiedInventory.Instance.SetActiveHotbar(i);
    }

    private void HandleScroll()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        _lastScrollTime = Time.time;
        int cur  = UnifiedInventory.Instance.ActiveHotbarSlot;
        int size = UnifiedInventory.Instance.HotbarSize;
        int next = scroll > 0
            ? (cur - 1 + size) % size
            : (cur + 1) % size;

        UnifiedInventory.Instance.SetActiveHotbar(next);
    }

    private void HandleReload()
    {
        // Reload con R (legacy)
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Buscar WeaponInventory en escena y llamar reload
            var wi = FindFirstObjectByType<WeaponInventory>();
            wi?.CurrentWeapon?.ReloadFromInventory();
        }
    }

    public void SetDragEnabled(bool enabled)
    {
        foreach (var slot in _slots)
            slot.SetDragEnabled(enabled);
    }

    private void OnActiveSlotChanged(int activeGlobal)
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i].SetSelected(UnifiedInventory.Instance.HotbarIndex(i) == activeGlobal);
    }

    private void OnDestroy()
    {
        if (UnifiedInventory.Instance != null)
            UnifiedInventory.Instance.OnActiveSlotChanged -= OnActiveSlotChanged;
    }
}
