// ============================================================
//  GF_WeaponAdapter.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Reemplaza: WeaponInventory.cs + AmmoInventory.cs
//
//  SIN MODIFICAR: Weapon.cs, WeaponAimController.cs, WeaponSwayBinder.cs
//
//  RESPONSABILIDADES:
//  1. Cuando GF_Inventory agrega un arma → instanciarla bajo WeaponHolder
//  2. Cuando el slot activo cambia → activar/desactivar el arma correcta
//  3. Cuando el estado es InventoryOpen/Paused → deshabilitar input de armas
//  4. Manejar recarga leyendo ammo de GF_Inventory directamente
//  5. Manejar drop de armas (física, reset del pickup)
//  6. Exponer CurrentWeapon para WeaponSwayBinder
//
//  MODIFICACIÓN MÍNIMA REQUERIDA EN WeaponSwayBinder.cs:
//  Cambiar: [SerializeField] WeaponInventory inventory;
//  Por:     [SerializeField] GF_WeaponAdapter adapter;
//  Y:       inventory.CurrentWeapon → adapter.CurrentWeapon
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class GF_WeaponAdapter : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS (asignar en Inspector)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Contexto del Jugador")]
    [Tooltip("El mismo PlayerWeaponContext que usabas en WeaponInventory.")]
    public PlayerWeaponContext playerContext;

    [Header("Arma por Defecto")]
    [Tooltip("ItemDefinition del arma con que empieza el jugador. " +
             "Su worldPrefab se instancia automáticamente al inicio.")]
    public GF_ItemDefinition defaultWeaponDefinition;

    [Header("Drop")]
    public float  dropForce       = 5f;
    public float  dropAngularForce = 3f;
    public Vector3 dropRotMin     = new Vector3(-60f, 0f, -60f);
    public Vector3 dropRotMax     = new Vector3( 60f, 360f, 60f);

    [Header("UI Munición (opcional)")]
    [Tooltip("AmmoUIController existente — se actualiza al cambiar de arma.")]
    public AmmoUIController ammoUI;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Mapa: slot global de GF_Inventory → Weapon instanciado en escena
    private Dictionary<int, Weapon> _weaponsBySlot = new Dictionary<int, Weapon>();

    // Arma actualmente equipada (slot activo)
    private Weapon _currentWeapon;
    private int    _currentSlotIndex = -1;

    // Acceso público para WeaponSwayBinder y sistemas externos
    public Weapon CurrentWeapon => _currentWeapon;

    // Referencia al holder de armas (del PlayerWeaponContext)
    private Transform WeaponHolder => playerContext?.weaponHolder;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE / START
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // No hacemos nada aquí — Start() corre después de que GF_Inventory
        // se inicializó, así el arma default se agrega sin race condition.
    }

    private void Start()
    {
        if (GF_Inventory.Instance == null)
        {
            Debug.LogError("[GF_WeaponAdapter] No hay GF_Inventory en escena.", this);
            return;
        }

        // Suscribirse ANTES de agregar el arma default
        GF_Inventory.Instance.OnItemAdded         += HandleItemAdded;
        GF_Inventory.Instance.OnItemRemoved       += HandleItemRemoved;
        GF_Inventory.Instance.OnActiveSlotChanged += HandleActiveSlotChanged;
        GF_Inventory.Instance.OnGameStateChanged  += HandleGameStateChanged;

        // Agregar arma default después de suscribirse → OnItemAdded disparará
        // y procesará el arma correctamente
        if (defaultWeaponDefinition != null)
        {
            GF_Inventory.Instance.TryAdd(defaultWeaponDefinition, 1);
            // El slot activo ya es 0 por defecto → HandleActiveSlotChanged se
            // disparará desde el TryAdd → equip automático
        }
        else
        {
            // Sin arma default, disparar el evento del slot activo
            // para que la UI se actualice correctamente
            GF_Inventory.Instance.ForceRefresh(GF_Inventory.Instance.ActiveIndex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HANDLERS DE EVENTOS
    // ─────────────────────────────────────────────────────────────────────

    private void HandleItemAdded(GF_ItemDefinition def, int amount, int slotIndex)
    {
        if (def.type != GF_ItemType.Weapon) return;

        // Instanciar el arma en escena
        Weapon weapon = InstantiateWeapon(def, slotIndex);
        if (weapon == null) return;

        _weaponsBySlot[slotIndex] = weapon;
        GF_Inventory.Instance.SetWeaponInstance(slotIndex, weapon);

        // Si el slot donde quedó es el activo, equipar inmediatamente
        if (slotIndex == GF_Inventory.Instance.ActiveIndex)
            EquipWeapon(slotIndex);
    }

    private void HandleItemRemoved(GF_ItemDefinition def, int amount, int slotIndex)
    {
        if (def.type != GF_ItemType.Weapon) return;

        // Si era el arma activa, desactivarla
        if (_currentSlotIndex == slotIndex)
        {
            DeactivateCurrentWeapon();
            _currentSlotIndex = -1;
            _currentWeapon    = null;
        }

        // Limpiar del diccionario
        _weaponsBySlot.Remove(slotIndex);
    }

    private void HandleActiveSlotChanged(int newIndex, int prevIndex)
    {
        // Si el slot anterior tenía un arma, desactivarla
        if (_currentWeapon != null)
            DeactivateCurrentWeapon();

        // Si el nuevo slot tiene un arma, equiparla
        var newSlot = GF_Inventory.Instance.GetSlot(newIndex);
        if (!newSlot.IsEmpty && newSlot.definition.type == GF_ItemType.Weapon)
            EquipWeapon(newIndex);
        else
        {
            // Slot sin arma → asegurarse de que no queda arma activa
            _currentWeapon    = null;
            _currentSlotIndex = -1;

            // Actualizar la UI de munición para reflejar "sin arma"
            ammoUI?.SetCurrentWeapon(null);
        }
    }

    private void HandleGameStateChanged(GF_GameState state)
    {
        if (_currentWeapon == null) return;

        bool weaponsActive = state == GF_GameState.Playing;

        // Habilitar/deshabilitar el script de disparo del arma activa
        _currentWeapon.enabled = weaponsActive;

        // Forzar salida del modo aim si se pausa
        if (!weaponsActive)
        {
            var aim = _currentWeapon.GetComponent<WeaponAimController>();
            aim?.ForceStopAim();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  EQUIP / DEACTIVATE
    // ─────────────────────────────────────────────────────────────────────

    private void EquipWeapon(int slotIndex)
    {
        if (!_weaponsBySlot.TryGetValue(slotIndex, out Weapon weapon)) return;
        if (weapon == null) return;

        _currentWeapon    = weapon;
        _currentSlotIndex = slotIndex;

        // Aplicar offset de inventario
        weapon.transform.localPosition = weapon.GetInventoryPositionOffset();
        weapon.ApplyInventoryRotation(weapon.GetInventoryRotationOffset());

        // Inyectar contexto en el AimController
        var aim = weapon.GetComponent<WeaponAimController>();
        if (aim != null && playerContext != null)
            aim.InjectContext(playerContext);

        // Refresh del sway
        var sway = weapon.GetComponent<SwayController>();
        sway?.RefreshBaseRotation();

        // Activar el GO
        weapon.gameObject.SetActive(true);

        // Habilitar Weapon solo si el juego está activo
        weapon.enabled = GF_Inventory.Instance.GameState == GF_GameState.Playing;

        // Actualizar UI de munición
        ammoUI?.SetCurrentWeapon(weapon);
    }

    private void DeactivateCurrentWeapon()
    {
        if (_currentWeapon == null) return;

        // Forzar salida del aim antes de desactivar
        var aim = _currentWeapon.GetComponent<WeaponAimController>();
        aim?.ForceStopAim();

        _currentWeapon.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INSTANCIAR ARMA
    // ─────────────────────────────────────────────────────────────────────

    private Weapon InstantiateWeapon(GF_ItemDefinition def, int slotIndex)
    {
        if (def.worldPrefab == null)
        {
            Debug.LogError($"[GF_WeaponAdapter] '{def.displayName}' no tiene worldPrefab.", this);
            return null;
        }

        // Verificar si el GF_WorldPickup ya tiene un Weapon activo
        // (el jugador recogió el prefab del mundo — reusar esa instancia)
        var existingSlot = GF_Inventory.Instance.GetSlot(slotIndex);
        if (existingSlot.weaponInstance != null)
        {
            // Reusar la instancia que vino del pickup
            Weapon existing = existingSlot.weaponInstance;
            PrepareWeaponForHolster(existing);
            return existing;
        }

        // Instanciar nuevo
        var go     = Instantiate(def.worldPrefab);
        var weapon = go.GetComponent<Weapon>() ?? go.GetComponentInChildren<Weapon>();

        if (weapon == null)
        {
            Debug.LogError($"[GF_WeaponAdapter] '{def.displayName}'.worldPrefab no tiene Weapon.", this);
            Destroy(go);
            return null;
        }

        PrepareWeaponForHolster(weapon);
        return weapon;
    }

    private void PrepareWeaponForHolster(Weapon w)
    {
        if (WeaponHolder != null)
            w.transform.SetParent(WeaponHolder, false);

        // Kinematic, sin colisiones mientras está en el inventario
        if (w.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        foreach (var col in w.GetComponentsInChildren<Collider>()) col.enabled = false;

        // Asegurarse de que el GO empiece inactivo
        w.gameObject.SetActive(false);
        w.enabled = false;

        // Desactivar el GF_WorldPickup si existe
        var pickup = w.GetComponent<GF_WorldPickup>();
        if (pickup != null) pickup.enabled = false;

        // Activar audio
        if (w.TryGetComponent<WeaponAudio>(out var audio)) audio.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DROP DE ARMA (llamado desde GF_InventoryUI al presionar G)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tira al mundo el arma del slot activo.
    /// GF_InventoryUI llama esto en lugar de GF_Inventory.DropActive()
    /// para manejar la física del arma correctamente.
    /// </summary>
    public void DropCurrentWeapon()
    {
        int activeIndex = GF_Inventory.Instance.ActiveIndex;
        var slot = GF_Inventory.Instance.GetSlot(activeIndex);

        if (slot.IsEmpty || slot.definition.type != GF_ItemType.Weapon) return;
        if (!slot.definition.canDrop) return;

        Weapon w = _currentWeapon;
        if (w == null) _weaponsBySlot.TryGetValue(activeIndex, out w);
        if (w == null) { GF_Inventory.Instance.ClearSlot(activeIndex); return; }

        // Salir del aim
        var aim = w.GetComponent<WeaponAimController>();
        aim?.ForceStopAim();

        // Desactivar weapon, reactivar como objeto de mundo
        w.enabled = false;
        if (w.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
        foreach (var col in w.GetComponentsInChildren<Collider>()) col.enabled = true;

        // Restaurar pickup
        var pickup = w.GetComponent<GF_WorldPickup>();
        if (pickup != null) { pickup.enabled = true; pickup.ResetPickup(); }

        // Separar del holder y poner en el mundo
        w.transform.SetParent(null);
        w.transform.position = transform.position
            + transform.TransformDirection(new Vector3(0f, 0.3f, 1f));
        w.transform.rotation = Quaternion.Euler(
            Random.Range(dropRotMin.x, dropRotMax.x),
            Random.Range(dropRotMin.y, dropRotMax.y),
            Random.Range(dropRotMin.z, dropRotMax.z));

        if (rb != null)
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * dropAngularForce, ForceMode.Impulse);
        }

        w.gameObject.SetActive(true);

        // Limpiar estado interno
        _weaponsBySlot.Remove(activeIndex);
        _currentWeapon    = null;
        _currentSlotIndex = -1;

        // Limpiar en GF_Inventory (sin disparar la lógica de drop física de nuevo)
        GF_Inventory.Instance.ClearSlot(activeIndex);

        // Actualizar UI
        ammoUI?.SetCurrentWeapon(null);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  RECARGA (llamado desde GF_InventoryUI al presionar R)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Recarga el arma activa usando ammo de GF_Inventory.
    /// No llama Weapon.ReloadFromInventory() — lee directamente del inventario.
    /// </summary>
    public void ReloadCurrentWeapon()
    {
        if (_currentWeapon == null) return;

        var mag   = _currentWeapon.magazine;
        var stats = _currentWeapon.stats;

        if (mag == null || stats?.ammoType == null || mag.IsFull) return;

        // Buscar la GF_ItemDefinition que corresponde a este tipo de ammo
        var ammoDef = FindAmmoDefinition(stats.ammoType);
        if (ammoDef == null)
        {
            Debug.LogWarning($"[GF_WeaponAdapter] No hay ItemDefinition para ammo '{stats.ammoType.ammoName}'.");
            return;
        }

        int needed  = mag.maxBullets - mag.currentBullets;
        int removed = GF_Inventory.Instance.RemoveItems(ammoDef, needed);

        if (removed > 0)
            mag.AddBullets(removed);
    }

    /// <summary>
    /// Busca la GF_ItemDefinition de tipo Ammo que corresponde a un AmmoTypeSO.
    /// </summary>
    private GF_ItemDefinition FindAmmoDefinition(AmmoTypeSO ammoType)
    {
        if (ammoType == null) return null;
        for (int i = 0; i < GF_Inventory.Instance.TotalSlots; i++)
        {
            var slot = GF_Inventory.Instance.GetSlot(i);
            if (!slot.IsEmpty
                && slot.definition.type == GF_ItemType.Ammo
                && slot.definition.ammoType == ammoType)
                return slot.definition;
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PICKUP DE ARMA DESDE EL MUNDO
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por GF_WorldPickup cuando el jugador recoge un arma.
    /// Pasa la instancia del Weapon ya existente en el mundo para reutilizarla.
    /// </summary>
    public bool PickupWeapon(GF_ItemDefinition def, Weapon existingInstance)
    {
        // Si el inventario está lleno, no hacer nada
        bool added = GF_Inventory.Instance.TryAdd(def, 1, existingInstance);

        if (added && existingInstance != null)
            PrepareWeaponForHolster(existingInstance);

        return added;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (GF_Inventory.Instance == null) return;
        GF_Inventory.Instance.OnItemAdded         -= HandleItemAdded;
        GF_Inventory.Instance.OnItemRemoved       -= HandleItemRemoved;
        GF_Inventory.Instance.OnActiveSlotChanged -= HandleActiveSlotChanged;
        GF_Inventory.Instance.OnGameStateChanged  -= HandleGameStateChanged;
    }
}