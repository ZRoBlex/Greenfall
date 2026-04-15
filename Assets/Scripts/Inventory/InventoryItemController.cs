// ============================================================
//  InventoryItemController.cs
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/
// ============================================================
//
//  QUÉ ES ESTO:
//  El componente que va en TODOS los objetos recogibles del mundo.
//  Reemplaza WeaponPickup para armas y maneja cualquier tipo de item.
//
//  CÓMO USARLO:
//  1. Crea o selecciona el prefab del objeto en el mundo (hacha, semilla, etc.)
//  2. Agrega este componente
//  3. Asigna el InventoryItemData correspondiente
//  4. Opcionalmente ajusta los overrides de cantidad y sonido
//
//  FLUJO CUANDO EL JUGADOR RECOGE:
//  PlayerInteractor.cs detecta este componente con raycast
//  → llama TryPickup()
//  → se agrega al InventorySystem
//  → se toca el VFX y el sonido de recoger
//  → el GameObject se desactiva (o destruye)
//
//  OVERRIDES POR INSTANCIA:
//  Aunque todos los "hacha de madera" usan el mismo InventoryItemData,
//  puedes hacer que UNA instancia específica en el mundo tenga
//  propiedades distintas (ej: esta caja tiene 50 balas en vez de 30).
// ============================================================

using UnityEngine;

public class InventoryItemController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  DATOS DEL ITEM
    // ─────────────────────────────────────────────────────────────────────

    [Header("Datos del Item")]
    [Tooltip("La definición del item. Crea uno con: " +
             "Click derecho → Create → Greenfall → Inventory → Item Data. " +
             "Este SO contiene el ícono, tipo, sonido, VFX, etc.")]
    [SerializeField] private InventoryItemData _itemData;

    [Header("Overrides (dejan en blanco para usar el valor del ItemData)")]
    [Tooltip("Cuántas unidades hay en esta instancia específica en el mundo. " +
             "0 = usa el valor por defecto (1 para no-stackeables, 1+ para stackeables). " +
             "Ejemplo: esta caja de munición tiene 45 balas, no 30.")]
    [SerializeField] private int _amountOverride = 0;

    [Tooltip("Si se asigna, sobreescribe el sonido de recoger del ItemData. " +
             "Útil para tener variaciones de sonido en la misma caja de munición.")]
    [SerializeField] private AudioClip _pickupSoundOverride;

    // ─────────────────────────────────────────────────────────────────────
    //  INTERACCIÓN
    // ─────────────────────────────────────────────────────────────────────

    [Header("Interacción")]
    [Tooltip("Texto que aparece en la UI cuando el jugador mira este objeto. " +
             "Vacío = se genera automáticamente: 'Recoger [NombreItem]'.")]
    [SerializeField] private string _interactionTextOverride = "";

    [Tooltip("Si true, el objeto parpadea o tiene algún efecto de outline " +
             "cuando el jugador lo está mirando. " +
             "Requiere un OutlineController en este GO o en sus hijos.")]
    [SerializeField] private bool _highlightOnFocus = true;

    // ─────────────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN ESPECIAL DE ARMAS
    // ─────────────────────────────────────────────────────────────────────

    [Header("Solo para Weapons")]
    [Tooltip("Para armas: si true, las balas en el magazine de esta instancia " +
             "específica son aleatorias al spawnearse (simula arma usada). " +
             "Si false, el magazine viene lleno (arma nueva).")]
    [SerializeField] private bool _randomAmmoOnSpawn = false;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Cuántas unidades hay realmente (considerando overrides)
    private int _actualAmount;

    // True después del primer pickup (para poder recogerse de nuevo tras ser dropeado)
    private bool _hasBeenPickedUp = false;

    // Cache del componente Weapon si este prefab es un arma
    private Weapon _cachedWeapon;

    // Cache del outline para efectos de hover
    private MonoBehaviour _outlineComponent;

    // ─────────────────────────────────────────────────────────────────────
    //  PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Los datos del item que este objeto representa.</summary>
    public InventoryItemData ItemData => _itemData;

    /// <summary>Transform de este objeto (usado por PlayerInteractor para distancia).</summary>
    public Transform WorldTransform => transform;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Calcular la cantidad real de este item en escena
        _actualAmount = _amountOverride > 0
            ? _amountOverride
            : (_itemData != null ? 1 : 0);

        // Cache del componente Weapon si aplica
        if (_itemData != null && _itemData.type == ItemType.Weapon)
            _cachedWeapon = GetComponent<Weapon>() ?? GetComponentInChildren<Weapon>();

        // Cache del outline (si existe)
        // Buscamos por nombre de tipo para no tener dependencia dura
        _outlineComponent = GetComponent("OutlineObject") as MonoBehaviour
                        ?? GetComponentInChildren<MonoBehaviour>(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INTERFAZ DE INTERACCIÓN
    //  PlayerInteractor llama estos métodos
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Texto que aparece en la UI de interacción cuando el jugador mira este objeto.
    /// </summary>
    public string GetInteractionText()
    {
        if (!string.IsNullOrEmpty(_interactionTextOverride))
            return _interactionTextOverride;

        if (_itemData == null) return "Interactuar";

        // Generar texto automáticamente según el tipo
        string verb = _itemData.type switch
        {
            ItemType.Weapon     => "Recoger",
            ItemType.Ammo       => "Recoger",
            ItemType.Tool       => "Recoger",
            ItemType.Seed       => "Recoger",
            ItemType.Consumable => "Usar",
            ItemType.Material   => "Recoger",
            ItemType.QuestItem  => "Examinar",
            _                   => "Recoger"
        };

        // Si es stackeable, mostrar cantidad
        string amount = _itemData.isStackable && _actualAmount > 1
            ? $" x{_actualAmount}"
            : "";

        return $"{verb} {_itemData.displayName}{amount}";
    }

    /// <summary>
    /// Intenta hacer que el jugador recoja este item.
    /// Llamado por PlayerInteractor cuando el jugador presiona E.
    ///
    /// Returns: true si se recogió exitosamente.
    /// </summary>
    public bool TryPickup()
    {
        // Seguridad: no permitir doble pickup en el mismo frame
        if (_hasBeenPickedUp) return false;
        if (_itemData == null)
        {
            Debug.LogError($"[InventoryItemController] {name}: ItemData es null.", this);
            return false;
        }
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[InventoryItemController] No hay InventorySystem en escena.");
            return false;
        }

        // Para armas, incluimos la instancia en escena para que el bridge la pueda gestionar
        Weapon weaponToRegister = (_itemData.type == ItemType.Weapon) ? _cachedWeapon : null;

        // Intentar agregar al inventario
        bool added = InventorySystem.Instance.TryAddItem(_itemData, _actualAmount, weaponToRegister);

        if (!added)
        {
            // Inventario lleno — el evento ya se disparó desde InventorySystem
            // Aquí podemos agregar feedback adicional si queremos (shake de UI, etc.)
            return false;
        }

        // Registro exitoso — ejecutar efectos
        _hasBeenPickedUp = true;
        OnPickupSuccess();
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  EFECTOS DE RECOGER
    // ─────────────────────────────────────────────────────────────────────

    private void OnPickupSuccess()
    {
        // 1. VFX de partículas
        if (PickupVFXSystem.Instance != null)
            PickupVFXSystem.Instance.Play(_itemData.pickupVFX, transform.position);

        // 2. Sonido de recoger
        AudioClip soundToPlay = _pickupSoundOverride != null
            ? _pickupSoundOverride
            : _itemData.pickupSound;

        if (soundToPlay != null)
            PlaySoundAtPoint(soundToPlay, transform.position, _itemData.pickupVolume);

        // 3. Si es un arma, WeaponInventoryBridge se encarga de activarla
        //    No necesitamos hacer nada especial aquí.

        // 4. Desactivar este GameObject
        //    Lo desactivamos en lugar de destruirlo para poder reutilizarlo
        //    (el pool de items o el sistema de respawn puede reactivarlo)
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HIGHLIGHT (cuando el jugador lo está mirando)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Activa el efecto de outline/highlight en este objeto.
    /// Llamado por PlayerInteractor cuando el raycast entra en este objeto.
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (!_highlightOnFocus) return;

        // Intentar activar/desactivar el OutlineObject si existe
        // Buscamos por nombre de tipo para evitar dependencia dura en este script
        var outlines = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var comp in outlines)
        {
            if (comp.GetType().Name == "OutlineObject" ||
                comp.GetType().Name == "HoverOutline")
            {
                comp.enabled = highlighted;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PARA SISTEMAS EXTERNOS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resetea el estado de este item para que pueda ser recogido de nuevo.
    /// Se llama cuando el item es dropeado al mundo después de haber sido recogido.
    /// </summary>
    public void ResetForPickup()
    {
        _hasBeenPickedUp = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Establece la cantidad de unidades que este item drop tiene.
    /// Llamado por InventorySystem.DropItemFromSlot().
    /// </summary>
    public void SetDroppedAmount(int amount)
    {
        _actualAmount    = amount;
        _hasBeenPickedUp = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ─────────────────────────────────────────────────────────────────────

    private static void PlaySoundAtPoint(AudioClip clip, Vector3 pos, float volume)
    {
        var go  = new GameObject("PickupSound");
        go.transform.position = pos;
        var src  = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.spatialBlend = 0.5f; // Mezcla entre 2D y 3D
        src.volume       = volume;
        src.Play();
        Object.Destroy(go, clip.length + 0.2f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GIZMOS DE DEBUG
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_itemData == null) return;

        // Mostrar el tipo y nombre del item encima del objeto en scene view
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"[{_itemData.type}] {_itemData.displayName}");

        // Esfera de color según rareza
        Gizmos.color = _itemData.GetRarityColor();
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
#endif
}