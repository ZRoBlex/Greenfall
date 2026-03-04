// ============================================================
// WeaponPickup.cs
// Carpeta: Scripts/Pickup/
// ------------------------------------------------------------
// Se agrega al PREFAB del arma tirada en el suelo.
// Cuando el jugador lo recoge:
//   1. Llama UnifiedInventory.TryAdd() con un InventoryEntry de arma
//   2. Si hay espacio: desactiva el Rigidbody, oculta el objeto
//   3. Si NO hay espacio: no hace nada (el arma sigue en el suelo)
//
// NOTA SOBRE EL FLUJO DE ARMAS:
// La instancia de Weapon NO se destruye al recogerla.
// Se desactiva en el mundo y su GameObject se mueve al WeaponHolder
// del jugador (eso lo hace WeaponInventory.AddWeapon internamente).
//
// ESTRUCTURA DEL PREFAB DE ARMA EN EL SUELO:
// WeaponPickupRoot
// ├── Rigidbody           ← para física al caer/ser dropeada
// ├── Collider            ← para detección de proximidad
// ├── WeaponPickup        ← ESTE SCRIPT
// ├── Weapon              ← el script principal del arma
// └── WeaponMesh          ← el modelo visible
//
// CÓMO SPAWNEAR UN ARMA EN EL SUELO:
// Simplemente instancia el prefab en el mundo.
// No necesitas configuración extra.
// ============================================================

using UnityEngine;
using Greenfall.Inventory;

public class WeaponPickup : MonoBehaviour, IPickable
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("Datos del Arma")]
    [Tooltip("El componente Weapon de este prefab. Se autodetecta si está en el mismo GO.")]
    [SerializeField] private Weapon _weapon;

    [Header("Label de Interacción")]
    [Tooltip("Texto base que ve el jugador al acercarse. Se añade el nombre del arma.")]
    [SerializeField] private string _pickupVerb = "Recoger";

    [Header("Efecto al Recoger (opcional)")]
    [Tooltip("Partículas o sonido al ser recogida. Se destruyen solos.")]
    [SerializeField] private GameObject _pickupVFXPrefab;
    [SerializeField] private AudioClip  _pickupSound;

    // ─────────────────────────────────────────────────────────
    // COMPONENTES
    // ─────────────────────────────────────────────────────────

    private Rigidbody  _rb;
    private Collider[] _colliders;
    private AudioSource _audio;
    private bool _hasBeenPickedUp;

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Autodetectar el componente Weapon si no está asignado
        if (_weapon == null)
            _weapon = GetComponent<Weapon>() ?? GetComponentInChildren<Weapon>();

        _rb         = GetComponent<Rigidbody>();
        _colliders  = GetComponentsInChildren<Collider>();
        _audio      = GetComponent<AudioSource>();

        if (_weapon == null)
            Debug.LogError($"[WeaponPickup] {name}: No se encontró componente Weapon.");
    }

    // ─────────────────────────────────────────────────────────
    // IPickable
    // ─────────────────────────────────────────────────────────

    public Transform WorldTransform => transform;

    public string GetPickupLabel()
    {
        string weaponName = _weapon != null ? _weapon.weaponName : "Arma";
        return $"{_pickupVerb} {weaponName}";
    }

    /// <summary>
    /// El jugador intenta recoger esta arma.
    /// Retorna true si se añadió al inventario, false si estaba lleno.
    /// </summary>
    public bool TryPickup()
    {
        if (_hasBeenPickedUp) return false;
        if (_weapon == null)  return false;
        if (UnifiedInventory.Instance == null)
        {
            Debug.LogError("[WeaponPickup] No hay UnifiedInventory en escena.");
            return false;
        }

        // Crear el entry y intentar añadirlo
        var entry = InventoryEntry.FromWeapon(_weapon);
        bool added = UnifiedInventory.Instance.TryAdd(entry);

        if (!added)
        {
            // Inventario lleno → el arma sigue en el suelo
            // Aquí podrías mostrar un mensaje "Inventario lleno"
            return false;
        }

        // ── Recoger el arma: quitarla del mundo ──
        _hasBeenPickedUp = true;

        // Sonido
        PlayPickupSound();

        // Efecto visual
        if (_pickupVFXPrefab != null)
            Destroy(Instantiate(_pickupVFXPrefab, transform.position, Quaternion.identity), 3f);

        // Desactivar físicas y colliders (el Weapon.cs se activa dentro del WeaponInventory)
        if (_rb != null) _rb.isKinematic = true;
        foreach (var col in _colliders) col.enabled = false;

        // Desactivar el pickup en el mundo
        // El GameObject del Weapon se reactivará dentro del WeaponHolder del jugador
        // cuando WeaponInventory.AddWeapon lo procese
        gameObject.SetActive(false);

        return true;
    }

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    private void PlayPickupSound()
    {
        if (_pickupSound == null) return;

        // Crear una fuente de audio temporal que no se destruya con el objeto
        var tempGO = new GameObject("PickupSound_Temp");
        tempGO.transform.position = transform.position;
        var src = tempGO.AddComponent<AudioSource>();
        src.clip    = _pickupSound;
        src.volume  = 1f;
        src.spatialBlend = 0f; // 2D
        src.Play();
        Destroy(tempGO, _pickupSound.length + 0.1f);
    }

    // ─────────────────────────────────────────────────────────
    // VISUAL DEBUG
    // ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
#endif
}
