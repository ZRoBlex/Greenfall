// ============================================================
// SeedPickup.cs  (REEMPLAZA el anterior)
// Carpeta: Scripts/Pickup/
// ------------------------------------------------------------
// Reemplaza tu SeedPickup.cs anterior.
// Mismo concepto, pero ahora conecta con UnifiedInventory
// en lugar de solo hacer un Debug.Log.
//
// PARA CONFIGURAR EN EL PREFAB:
// SeedPickupRoot
// ├── Collider (trigger o no, según cómo quieras el pickup)
// └── SeedPickup  ← ESTE SCRIPT
//     ├── seedData  → ScriptableObject SeedItem
//     └── amount    → cuántas semillas
//
// DOS MODOS DE PICKUP:
//   1. Automático (trigger): el jugador se acerca y las recoge solas
//   2. Manual (PlayerInteractor): el jugador presiona E para recoger
//
// Por defecto está en modo manual (IPickable).
// Si quieres automático, pon _autoPickup = true en el Inspector.
// ============================================================

using UnityEngine;
using Greenfall.Inventory;

public class SeedPickup : MonoBehaviour, IPickable
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("Datos")]
    [Tooltip("ScriptableObject con los datos de la semilla.")]
    public SeedItem seedData;

    [Tooltip("Cuántas semillas contiene este pickup.")]
    public int amount = 1;

    [Header("Modo")]
    [Tooltip("true = se recoge al tocar al jugador. false = el jugador presiona E.")]
    [SerializeField] private bool _autoPickup = false;

    [Header("Efectos")]
    [SerializeField] private GameObject _vfxPrefab;
    [SerializeField] private AudioClip  _pickupSound;

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN (para spawnear por código)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa el pickup con datos específicos.
    /// Útil cuando plantas producen semillas y las spawnean al mundo.
    /// </summary>
    public void Initialize(SeedItem seed, int qty)
    {
        seedData = seed;
        amount   = qty;
    }

    // ─────────────────────────────────────────────────────────
    // IPickable
    // ─────────────────────────────────────────────────────────

    public Transform WorldTransform => transform;

    public string GetPickupLabel()
    {
        if (seedData == null) return "Recoger Semilla";
        return $"Recoger {seedData.seedId} x{amount}";
    }

    public bool TryPickup()
    {
        if (seedData == null)
        {
            Debug.LogWarning($"[SeedPickup] {name}: seedData no asignado.");
            return false;
        }

        if (UnifiedInventory.Instance == null)
        {
            Debug.LogError("[SeedPickup] No hay UnifiedInventory en escena.");
            return false;
        }

        var entry = InventoryEntry.FromSeed(seedData, amount);
        bool added = UnifiedInventory.Instance.TryAdd(entry);

        if (!added) return false;

        // Efectos
        if (_vfxPrefab != null)
            Destroy(Instantiate(_vfxPrefab, transform.position, Quaternion.identity), 3f);

        PlaySound();
        Destroy(gameObject);
        return true;
    }

    // ─────────────────────────────────────────────────────────
    // MODO AUTOMÁTICO (trigger)
    // ─────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!_autoPickup) return;
        if (!other.CompareTag("Player")) return;

        TryPickup();
    }

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    private void PlaySound()
    {
        if (_pickupSound == null) return;
        var tmp = new GameObject("SeedPickupSound");
        tmp.transform.position = transform.position;
        var src = tmp.AddComponent<AudioSource>();
        src.clip = _pickupSound;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(tmp, _pickupSound.length + 0.1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
#endif
}
