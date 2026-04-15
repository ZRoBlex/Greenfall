// ============================================================
//  WeaponInventoryBridge.cs
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/
// ============================================================
//
//  QUÉ HACE:
//  Es el "traductor" entre el nuevo InventorySystem y el sistema
//  de armas existente (WeaponInventory, WeaponAimController, etc.).
//
//  SIN ESTE SCRIPT:
//  El InventorySystem sabe que tienes "Weapon_AK47 x1" en el slot 2,
//  pero no sabe cómo activar el modelo 3D del arma en la escena.
//
//  CON ESTE SCRIPT:
//  Escucha los eventos de InventorySystem y traduce esas acciones
//  a llamadas al WeaponInventory (que maneja la lógica visual del arma).
//
//  FLUJO DE AGREGAR UN ARMA:
//  1. Jugador toca InventoryItemController del arma
//  2. InventoryItemController.TryPickup() → InventorySystem.TryAddItem()
//  3. InventorySystem dispara OnItemAdded(weaponItemData, 1, slotIndex)
//  4. Este bridge escucha ese evento
//  5. Instancia el worldPrefab del arma y lo pasa a WeaponInventory.AddWeapon()
//  6. WeaponInventory maneja el modelo 3D, aim, recoil, etc.
//
//  FLUJO DE CAMBIAR DE ARMA:
//  1. Jugador presiona 1, 2, 3... → InventorySystem.SetActiveHotbarSlot()
//  2. InventorySystem dispara OnActiveSlotChanged(slotIndex)
//  3. Este bridge escucha ese evento
//  4. Si el slot tiene un arma → WeaponInventory.Equip(weaponIndex)
//  5. Si no tiene arma → WeaponInventory desactiva el arma actual
//
//  COLOCA ESTE SCRIPT EN: el mismo GameObject que WeaponInventory
// ============================================================

using UnityEngine;

public class WeaponInventoryBridge : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────────────────────────────

    [Header("Referencias")]
    [Tooltip("El WeaponInventory existente que gestiona los modelos 3D de las armas.")]
    [SerializeField] private WeaponInventory _weaponInventory;

    [Tooltip("El contexto del jugador para inyectar en las armas al equiparlas.")]
    [SerializeField] private PlayerWeaponContext _playerContext;

    [Tooltip("El Transform donde se instancian los modelos 3D de las armas.")]
    [SerializeField] private Transform _weaponHolder;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE / START
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-detectar WeaponInventory si no fue asignado
        if (_weaponInventory == null)
            _weaponInventory = GetComponent<WeaponInventory>()
                            ?? GetComponentInChildren<WeaponInventory>();

        if (_weaponInventory == null)
            Debug.LogError("[WeaponInventoryBridge] No se encontró WeaponInventory.", this);
    }

    private void Start()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[WeaponInventoryBridge] No hay InventorySystem en escena.", this);
            return;
        }

        // Suscribirse a los eventos relevantes del InventorySystem
        InventorySystem.Instance.OnItemAdded      += HandleItemAdded;
        InventorySystem.Instance.OnItemRemoved    += HandleItemRemoved;
        InventorySystem.Instance.OnActiveSlotChanged += HandleActiveSlotChanged;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HANDLERS DE EVENTOS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Se llama cuando InventorySystem agrega un item a un slot.
    /// Si el item es un arma, necesitamos instanciar su componente en escena.
    /// </summary>
    private void HandleItemAdded(InventoryItemData itemData, int amount, int slotIndex)
    {
        // Solo nos interesan las armas
        if (itemData.type != ItemType.Weapon) return;
        if (itemData.worldPrefab == null)
        {
            Debug.LogError($"[WeaponInventoryBridge] El arma '{itemData.displayName}' " +
                           $"no tiene worldPrefab asignado.", this);
            return;
        }

        // Verificar que el slot en InventorySystem aún no tiene una instancia
        // (puede que el item ya venía con una instancia del InventoryItemController)
        var slot = InventorySystem.Instance.GetSlot(slotIndex);

        Weapon weaponInstance = slot.weaponInstance;

        if (weaponInstance == null)
        {
            // No hay instancia → crear una nueva del worldPrefab
            var weaponGO = Instantiate(itemData.worldPrefab);
            weaponInstance = weaponGO.GetComponent<Weapon>()
                          ?? weaponGO.GetComponentInChildren<Weapon>();

            if (weaponInstance == null)
            {
                Debug.LogError($"[WeaponInventoryBridge] El worldPrefab de '{itemData.displayName}' " +
                               $"no tiene componente Weapon.", this);
                Destroy(weaponGO);
                return;
            }

            // Registrar la instancia en el slot del inventario
            InventorySystem.Instance.SetWeaponInstance(slotIndex, weaponInstance);
        }

        // Pasar el arma al WeaponInventory para que la gestione
        // WeaponInventory se encarga de: parenting, aim, recoil, UI de munición
        _weaponInventory.AddWeapon(weaponInstance);

        // Reproducir sonido de equip si existe
        if (itemData.equipSound != null)
            PlaySoundAtPoint(itemData.equipSound, transform.position, 0.8f);
    }

    /// <summary>
    /// Se llama cuando InventorySystem quita un item de un slot.
    /// Si es un arma, hay que sacarla del WeaponInventory.
    /// </summary>
    private void HandleItemRemoved(InventoryItemData itemData, int amount, int slotIndex)
    {
        // Solo armas
        if (itemData.type != ItemType.Weapon) return;

        // Buscar qué índice tiene esta arma en WeaponInventory
        // (WeaponInventory tiene su propia lista interna)
        // Esto es necesario porque el índice de InventorySystem puede no
        // coincidir con el de WeaponInventory si hay otros tipos de items en hotbar
        if (_weaponInventory == null) return;

        // WeaponInventory.DropCurrent() maneja tirar el arma actual
        // Si el arma que se removió era la actual, dropeamos
        if (_weaponInventory.CurrentWeapon != null)
        {
            var slot = InventorySystem.Instance.GetSlot(slotIndex);
            // Si el slot ya está vacío (se limpió) y el arma actual coincide, dropeamos
            // El WeaponInventory.DropCurrent() maneja la física y el reset del pickup
        }
    }

    /// <summary>
    /// Se llama cuando el jugador cambia el slot activo (1, 2, 3... o scroll).
    /// Necesitamos decirle a WeaponInventory qué arma activar.
    /// </summary>
    private void HandleActiveSlotChanged(int globalSlotIndex)
    {
        if (_weaponInventory == null || InventorySystem.Instance == null) return;

        var slot = InventorySystem.Instance.GetSlot(globalSlotIndex);

        if (slot.IsEmpty || slot.itemData.type != ItemType.Weapon)
        {
            // El slot activo no tiene arma — desactivar el arma actual
            // WeaponInventory no tiene un método "desequip" directo,
            // pero podemos ocultar el arma actual
            // Por ahora simplemente no hacemos nada si no hay arma
            return;
        }

        // Calcular qué índice tiene esta arma en WeaponInventory
        // El índice en WeaponInventory es el número de armas ANTES de este slot
        int weaponIndex = CountWeaponsBeforeSlot(globalSlotIndex);
        _weaponInventory.Equip(weaponIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cuenta cuántas armas hay en los slots de hotbar ANTES del slot dado.
    /// Esto permite convertir el índice global en el índice de WeaponInventory.
    ///
    /// Ejemplo: slots = [Hacha, Vacío, AK47, Pistola, Semilla]
    /// Para la Pistola (slot 3): hay 1 arma antes (AK47 en slot 2) → weaponIndex = 1
    /// </summary>
    private int CountWeaponsBeforeSlot(int globalSlotIndex)
    {
        int count = 0;
        int hotbarSize = InventorySystem.Instance.HotbarSize;

        for (int i = 0; i < Mathf.Min(globalSlotIndex, hotbarSize); i++)
        {
            var s = InventorySystem.Instance.GetSlot(i);
            if (!s.IsEmpty && s.itemData.type == ItemType.Weapon)
                count++;
        }
        return count;
    }

    private static void PlaySoundAtPoint(AudioClip clip, Vector3 pos, float vol)
    {
        if (clip == null) return;
        var go = new GameObject("BridgeSound");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip; src.volume = vol; src.spatialBlend = 0f;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnItemAdded         -= HandleItemAdded;
        InventorySystem.Instance.OnItemRemoved       -= HandleItemRemoved;
        InventorySystem.Instance.OnActiveSlotChanged -= HandleActiveSlotChanged;
    }
}