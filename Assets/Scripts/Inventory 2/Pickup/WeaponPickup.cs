// ============================================================
// WeaponPickup.cs — FIX: ResetForDrop()
// ============================================================
// BUG CORREGIDO:
//   Al recoger un arma, _hasBeenPickedUp se pone true.
//   Al soltarla (DropCurrent), nadie resetea esa flag.
//   Al intentar recogerla de nuevo, TryPickup() devuelve false
//   y el sistema reporta "inventario lleno" cuando en realidad
//   el problema era esta flag.
//
//   WeaponInventory.PrepareAsDropped() ahora llama ResetForDrop()
//   para que el arma se pueda volver a recoger.
// ============================================================

using UnityEngine;
using Greenfall.Inventory;

public class WeaponPickup : MonoBehaviour, IPickable
{
    [Header("Datos del Arma")]
    [SerializeField] private Weapon _weapon;

    [Header("Label de Interacción")]
    [SerializeField] private string _pickupVerb = "Recoger";

    [Header("Efectos")]
    [SerializeField] private GameObject _pickupVFXPrefab;
    [SerializeField] private AudioClip  _pickupSound;

    private Rigidbody   _rb;
    private Collider[]  _colliders;
    private bool        _hasBeenPickedUp;

    private void Awake()
    {
        if (_weapon == null)
            _weapon = GetComponent<Weapon>() ?? GetComponentInChildren<Weapon>();

        _rb        = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>();

        if (_weapon == null)
            Debug.LogError($"[WeaponPickup] {name}: No se encontró componente Weapon.");
    }

    // ── IPickable ─────────────────────────────────────────────

    public Transform WorldTransform => transform;

    public string GetPickupLabel()
    {
        string n = _weapon != null ? _weapon.weaponName : "Arma";
        return $"{_pickupVerb} {n}";
    }

    public bool TryPickup()
    {
        if (_hasBeenPickedUp) return false;
        if (_weapon == null)  return false;

        if (UnifiedInventory.Instance == null)
        {
            Debug.LogError("[WeaponPickup] No hay UnifiedInventory.");
            return false;
        }

        var entry = InventoryEntry.FromWeapon(_weapon);
        bool added = UnifiedInventory.Instance.TryAdd(entry);
        if (!added) return false;

        _hasBeenPickedUp = true;

        PlayPickupSound();

        if (_pickupVFXPrefab != null)
            Destroy(Instantiate(_pickupVFXPrefab, transform.position, Quaternion.identity), 3f);

        if (_rb != null) _rb.isKinematic = true;
        foreach (var col in _colliders) col.enabled = false;

        gameObject.SetActive(false);
        return true;
    }

    // ── FIX: llamado por WeaponInventory al dropear el arma ───

    /// <summary>
    /// Resetea el estado del pickup para que el arma pueda
    /// recogerse de nuevo después de ser dropeada.
    /// </summary>
    public void ResetForDrop()
    {
        _hasBeenPickedUp = false;
    }

    // ── Audio ─────────────────────────────────────────────────

    private void PlayPickupSound()
    {
        if (_pickupSound == null) return;
        var tmp = new GameObject("PickupSound_Temp");
        tmp.transform.position = transform.position;
        var src = tmp.AddComponent<AudioSource>();
        src.clip         = _pickupSound;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(tmp, _pickupSound.length + 0.1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
#endif
}
