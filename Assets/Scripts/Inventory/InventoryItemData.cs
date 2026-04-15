// ============================================================
//  InventoryItemData.cs
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/Data/
// ============================================================
//
//  QUÉ ES ESTO:
//  Un ScriptableObject que define UN TIPO de item.
//  No es el item en sí — es la plantilla o "clase" del item.
//  Piensa en él como una ficha de definición:
//    - "Hacha de Madera" es un InventoryItemData
//    - Cuando el jugador recoge un hacha, el inventario guarda
//      una referencia a ese InventoryItemData + cuántas tiene (1)
//
//  CÓMO CREAR UNO:
//  Click derecho en Project → Create → Greenfall → Inventory → Item Data
//  Nómbralo "Item_HachaMadera" o "Item_Semilla_Maiz", etc.
//
//  TIPO DE ITEM (ItemType):
//  Weapon   → tiene WeaponStats, activa el sistema de armas
//  Ammo     → stackeable, tiene AmmoTypeSO, se consume al disparar
//  Seed     → se puede plantar, tiene datos de cultivo
//  Material → recurso de construcción/crafting (madera, metal, etc.)
//  Consumable → se usa y desaparece (comida, medicina)
//  Tool     → herramienta (hacha, pico, hoz) — tiene ToolData
//  QuestItem → no se puede tirar, tiene marcador especial
//  Misc     → cualquier otra cosa
// ============================================================

using UnityEngine;
using System;

// ── Enums públicos del sistema de inventario ───────────────────────────────

/// <summary>
/// Tipo funcional del item. Determina qué sistemas se activan
/// cuando el jugador equipa o usa este item.
/// </summary>
public enum ItemType
{
    Weapon,       // Activa WeaponInventory y permite disparar
    Ammo,         // Stackeable, se consume al recargar armas
    Seed,         // Se puede plantar en tierra fértil
    Material,     // Recurso de crafting/construcción
    Consumable,   // Se usa una vez (comida, medicina, buff)
    Tool,         // Herramienta recolectora (hacha, pico, hoz)
    QuestItem,    // No se puede tirar ni destruir
    Misc          // Cualquier otra cosa
}

/// <summary>
/// Rareza del item. Afecta el color del borde del slot en UI
/// y potencialmente el precio/valor.
/// </summary>
public enum ItemRarity
{
    Common,      // Gris  — items básicos del mundo
    Uncommon,    // Verde — algo mejor de lo normal
    Rare,        // Azul  — notable, especial
    Epic,        // Violeta — muy poderoso o único
    Legendary    // Dorado — único en el mundo
}

/// <summary>
/// Efecto visual que se aplica al slot en la UI cuando este
/// item está en él. Se configura con slotEffectIntensity.
/// </summary>
public enum SlotEffectType
{
    None,        // Sin efecto — slot normal
    Glow,        // Brillo suave en el borde del slot
    Pulse,       // El slot pulsa (escala sube y baja suavemente)
    Shine,       // Un destello se mueve por el icono (como luz)
    Rainbow      // El color del borde cambia continuamente (items legendarios)
}

/// <summary>
/// Forma del efecto de partículas al recoger el item.
/// Todo se genera desde código — no necesitas ningún prefab de VFX.
/// </summary>
public enum PickupVFXShape
{
    Burst,       // Explosión radial de partículas hacia afuera
    Rise,        // Partículas suben suavemente hacia arriba
    Spiral,      // Partículas suben en espiral (giro helicoidal)
    Scatter,     // Partículas salen en direcciones completamente aleatorias
    Implode,     // Partículas van hacia ADENTRO (como si se absorbieran)
    Sparkle      // Partículas estáticas que brillan y se desvanecen
}

// ── Config de VFX (serializable para aparecer en el Inspector) ──────────────

/// <summary>
/// Configuración completa del efecto visual al recoger un item.
/// Todo se genera en runtime desde código — sin prefabs necesarios.
/// Cada campo es ajustable directamente en el Inspector de InventoryItemData.
/// </summary>
[Serializable]
public class PickupVFXConfig
{
    [Tooltip("Forma del efecto. Determina hacia dónde van las partículas.")]
    public PickupVFXShape shape = PickupVFXShape.Burst;

    [Tooltip("Color principal de las partículas (al inicio de su vida).")]
    public Color primaryColor = Color.white;

    [Tooltip("Color secundario (al final de la vida de la partícula). " +
             "Las partículas van interpolando del primary al secondary.")]
    public Color secondaryColor = new Color(1f, 1f, 1f, 0f);

    [Tooltip("Número de partículas emitidas en el efecto. " +
             "Más partículas = efecto más llamativo pero más costoso.")]
    [Range(5, 80)] public int particleCount = 20;

    [Tooltip("Duración total del efecto en segundos. " +
             "El PickupVFXSystem espera este tiempo antes de devolver al pool.")]
    [Range(0.2f, 3f)] public float duration = 0.8f;

    [Tooltip("Radio o fuerza de la emisión. " +
             "Para Burst: qué tan lejos vuelan. Para Rise: velocidad inicial.")]
    [Range(0.1f, 5f)] public float radius = 1.5f;

    [Tooltip("Tamaño de cada partícula al nacer.")]
    [Range(0.02f, 0.5f)] public float particleSize = 0.1f;

    [Tooltip("Escala de variación en el tamaño. 0 = todas iguales, 1 = muy variadas.")]
    [Range(0f, 1f)] public float sizeVariation = 0.3f;

    [Tooltip("Si true, la gravedad afecta las partículas " +
             "(solo tiene sentido para Burst y Scatter).")]
    public bool useGravity = false;

    [Tooltip("Si true, las partículas rotan mientras viven.")]
    public bool rotateParticles = true;

    [Tooltip("Velocidad de rotación de las partículas (°/s).")]
    [Range(0f, 360f)] public float rotationSpeed = 90f;
}

// ── ScriptableObject principal ──────────────────────────────────────────────

/// <summary>
/// Plantilla de definición de un item del juego.
/// Crear: Click derecho en Project → Create → Greenfall → Inventory → Item Data
///
/// Una instancia de este SO define un TYPE de item (ej: "Hacha de Madera").
/// El inventario guarda referencias a este SO + una cantidad para representar
/// "cuántas hachas tiene el jugador".
/// </summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Greenfall/Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────
    //  IDENTIDAD
    // ─────────────────────────────────────────────────────────────────────

    [Header("Identidad")]
    [Tooltip("ID único del item. Se usa para buscarlo por código. " +
             "Ejemplos: 'weapon_axe_basic', 'ammo_9mm', 'seed_corn'. " +
             "NO uses espacios — usa guiones bajos.")]
    public string itemId = "item_unnamed";

    [Tooltip("Nombre que verá el jugador en la UI.")]
    public string displayName = "Item sin nombre";

    [Tooltip("Descripción corta que aparece al pasar el cursor. " +
             "1-2 frases máximo.")]
    [TextArea(1, 3)]
    public string description = "";

    [Tooltip("Icono del item. Se muestra en hotbar, inventario y tooltips.")]
    public Sprite icon;

    // ─────────────────────────────────────────────────────────────────────
    //  CLASIFICACIÓN
    // ─────────────────────────────────────────────────────────────────────

    [Header("Clasificación")]
    [Tooltip("Tipo funcional del item. Determina qué sistemas se activan " +
             "al equiparlo/usarlo.")]
    public ItemType type = ItemType.Misc;

    [Tooltip("Rareza del item. Afecta el color del borde del slot en UI.")]
    public ItemRarity rarity = ItemRarity.Common;

    // ─────────────────────────────────────────────────────────────────────
    //  PREFAB DEL MUNDO
    // ─────────────────────────────────────────────────────────────────────

    [Header("Prefab del Mundo")]
    [Tooltip("Prefab que aparece cuando el item está en el suelo esperando " +
             "ser recogido, o cuando el jugador lo tira. " +
             "Para armas: debe tener el componente Weapon. " +
             "Para semillas/materiales: puede ser solo un modelo 3D.")]
    public GameObject worldPrefab;

    // ─────────────────────────────────────────────────────────────────────
    //  APILAMIENTO (stacking)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Apilamiento")]
    [Tooltip("Si true, múltiples unidades de este item ocupan UN slot. " +
             "Activa esto para: munición, semillas, materiales, comida, etc. " +
             "Desactívalo para: armas, herramientas, items únicos.")]
    public bool isStackable = false;

    [Tooltip("Máximo de unidades en un solo slot de inventario. " +
             "Solo aplica si isStackable = true.")]
    [Min(1)] public int maxStack = 1;

    // ─────────────────────────────────────────────────────────────────────
    //  COMPORTAMIENTO
    // ─────────────────────────────────────────────────────────────────────

    [Header("Comportamiento")]
    [Tooltip("Si true, el jugador puede tirar este item. " +
             "Desactívalo para items de misión (QuestItem) o el arma default.")]
    public bool canBeDropped = true;

    [Tooltip("Si true, el jugador puede equipar este item (moverlo al slot activo " +
             "para usarlo). Actívalo para: armas, herramientas, semillas.")]
    public bool canBeEquipped = true;

    [Tooltip("Si true, este item se DESTRUYE al usarse (no vuelve al inventario). " +
             "Actívalo para: comida, medicina, objetos de un solo uso.")]
    public bool consumeOnUse = false;

    [Tooltip("Peso en kg del item. Puede usarse para límites de carga futuros.")]
    [Min(0f)] public float weight = 0.1f;

    [Tooltip("Valor en moneda del mundo (útil para comercio futuro).")]
    [Min(0)] public int baseValue = 10;

    // ─────────────────────────────────────────────────────────────────────
    //  VISUAL EN INVENTARIO
    // ─────────────────────────────────────────────────────────────────────

    [Header("Visual en Inventario")]
    [Tooltip("Color de fondo del slot cuando este item está en él. " +
             "Úsalo para diferenciar tipos de items de un vistazo.")]
    public Color slotBackgroundColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);

    [Tooltip("Color del borde del slot. " +
             "Normalmente relacionado con la rareza del item.")]
    public Color slotBorderColor = Color.gray;

    [Tooltip("Efecto visual especial en el slot. " +
             "Úsalo para items épicos o legendarios — úsalo con moderación.")]
    public SlotEffectType slotEffect = SlotEffectType.None;

    [Tooltip("Intensidad del efecto del slot. 0 = casi invisible, 1 = máximo.")]
    [Range(0f, 1f)] public float slotEffectIntensity = 0.5f;

    // ─────────────────────────────────────────────────────────────────────
    //  AUDIO
    // ─────────────────────────────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("Sonido que suena cuando el jugador recoge este item. " +
             "AudioSource temporal se crea y destruye automáticamente.")]
    public AudioClip pickupSound;

    [Tooltip("Volumen del sonido de recoger. 0.8 es buen valor por defecto.")]
    [Range(0f, 1f)] public float pickupVolume = 0.8f;

    [Tooltip("Sonido que suena cuando el jugador tira este item al suelo.")]
    public AudioClip dropSound;

    [Tooltip("Sonido que suena cuando el jugador equipa este item " +
             "(solo si canBeEquipped = true).")]
    public AudioClip equipSound;

    // ─────────────────────────────────────────────────────────────────────
    //  VFX AL RECOGER
    // ─────────────────────────────────────────────────────────────────────

    [Header("VFX al Recoger")]
    [Tooltip("Configuración del efecto de partículas al recoger. " +
             "Todo se genera desde código — sin prefabs. " +
             "Ajusta forma, colores, cantidad y tamaño desde aquí.")]
    public PickupVFXConfig pickupVFX = new PickupVFXConfig();

    // ─────────────────────────────────────────────────────────────────────
    //  DATOS ESPECÍFICOS POR TIPO
    //  (Solo llena la sección que corresponde al tipo de tu item)
    // ─────────────────────────────────────────────────────────────────────

    [Header("─── WEAPON (solo si type = Weapon) ─────────────")]
    [Tooltip("Stats del arma. Mismo WeaponStats que ya usas. " +
             "IMPORTANTE: el worldPrefab debe tener el componente Weapon.")]
    public WeaponStats weaponStats;

    // ─────────────────────────────────────────────────────────────────────

    [Header("─── AMMO (solo si type = Ammo) ──────────────────")]
    [Tooltip("Tipo de munición. Referencia al mismo AmmoTypeSO que usa WeaponStats.")]
    public AmmoTypeSO ammoType;

    [Tooltip("Cuántas unidades de munición hay en un item de este tipo. " +
             "Ejemplo: una caja de 9mm puede dar 30 balas.")]
    [Min(1)] public int ammoAmountPerPickup = 30;

    // ─────────────────────────────────────────────────────────────────────

    [Header("─── SEED (solo si type = Seed) ──────────────────")]
    [Tooltip("Prefab de la planta que crece al plantar esta semilla. " +
             "Diferente del worldPrefab (que es la semilla en el suelo).")]
    public GameObject plantPrefab;

    [Tooltip("Tiempo en segundos que tarda en crecer completamente.")]
    [Min(1f)] public float growthTime = 120f;

    [Tooltip("ID del tipo de suelo donde puede plantarse. " +
             "Vacío = puede plantarse en cualquier suelo.")]
    public string requiredSoilId = "";

    // ─────────────────────────────────────────────────────────────────────

    [Header("─── TOOL (solo si type = Tool) ──────────────────")]
    [Tooltip("Datos de la herramienta. Mismo ToolData que usa ToolController. " +
             "IMPORTANTE: el worldPrefab debe tener el componente ToolController.")]
    public ScriptableObject toolData; // Tipo base para no crear dependencia

    // ─────────────────────────────────────────────────────────────────────

    [Header("─── CONSUMABLE (solo si type = Consumable) ───────")]
    [Tooltip("Vida que restaura al usarse. 0 = no restaura vida.")]
    [Min(0f)] public float healthRestore = 0f;

    [Tooltip("Stamina que restaura al usarse. 0 = no restaura stamina.")]
    [Min(0f)] public float staminaRestore = 0f;

    [Tooltip("Hambre que restaura al usarse. 0 = no restaura hambre.")]
    [Min(0f)] public float hungerRestore = 0f;

    [Tooltip("Duración de efectos temporales en segundos. 0 = instantáneo.")]
    [Min(0f)] public float effectDuration = 0f;

    // ─────────────────────────────────────────────────────────────────────

    [Header("─── MATERIAL (solo si type = Material) ──────────")]
    [Tooltip("Valor en crafting. Cuántas unidades de este material " +
             "equivalen a 1 unidad de recurso de referencia.")]
    [Min(0.1f)] public float craftingValue = 1f;

    [Tooltip("Tag de categoría de material. Ejemplo: 'Wood', 'Metal', 'Biomatter'. " +
             "Usado por el sistema de crafting para verificar recetas.")]
    public string materialCategory = "";

    // ─────────────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el color asociado a la rareza de este item.
    /// Útil para colorear bordes de slots automáticamente.
    /// </summary>
    public Color GetRarityColor()
    {
        return rarity switch
        {
            ItemRarity.Common    => new Color(0.65f, 0.65f, 0.65f), // Gris
            ItemRarity.Uncommon  => new Color(0.10f, 0.75f, 0.25f), // Verde
            ItemRarity.Rare      => new Color(0.10f, 0.45f, 0.95f), // Azul
            ItemRarity.Epic      => new Color(0.60f, 0.15f, 0.90f), // Violeta
            ItemRarity.Legendary => new Color(0.95f, 0.75f, 0.10f), // Dorado
            _                   => Color.white
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    //  VALIDACIÓN EN EDITOR
    // ─────────────────────────────────────────────────────────────────────

    private void OnValidate()
    {
        // Si cambiamos el tipo, ajustamos automáticamente isStackable
        // a los valores más comunes para ese tipo (el developer puede cambiarlo)
        if (type == ItemType.Weapon || type == ItemType.Tool || type == ItemType.QuestItem)
        {
            isStackable = false;
            maxStack = 1;
        }
        else if (type == ItemType.Ammo || type == ItemType.Seed || type == ItemType.Material)
        {
            if (!isStackable)
            {
                isStackable = true;
                if (maxStack <= 1) maxStack = 99;
            }
        }

        // Si es QuestItem, no se puede tirar
        if (type == ItemType.QuestItem)
            canBeDropped = false;

        // Si no tiene icono, log de advertencia
        if (icon == null)
            Debug.LogWarning($"[InventoryItemData] '{name}' no tiene icono asignado.", this);

        // Sincronizar slotBorderColor con rareza automáticamente
        // (el developer puede sobreescribir)
        slotBorderColor = GetRarityColor();
    }
}