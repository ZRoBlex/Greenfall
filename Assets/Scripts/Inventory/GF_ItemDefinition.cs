// ============================================================
//  GF_ItemDefinition.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Crear: Click derecho → Create → Greenfall → Item Definition
//
//  Este ScriptableObject define UN tipo de item.
//  No es el item en el mundo — es la "receta" del item.
//  El inventario guarda: (referencia a este SO + cantidad).
// ============================================================

using System;
using UnityEngine;

// ── Enums ──────────────────────────────────────────────────────────────────

public enum GF_ItemType
{
    Weapon,      // Activa el sistema de armas (GF_WeaponAdapter)
    Ammo,        // Munición, stackeable, se consume al recargar
    Seed,        // Semilla plantable
    Material,    // Recurso de crafting/construcción
    Consumable,  // Se usa y desaparece (comida, medicina)
    Tool,        // Herramienta recolectora
    QuestItem,   // No se puede tirar
    Misc         // Cualquier otra cosa
}

public enum GF_ItemRarity
{
    Common,      // Gris
    Uncommon,    // Verde
    Rare,        // Azul
    Epic,        // Violeta
    Legendary    // Dorado
}

public enum GF_SlotEffect
{
    None,     // Sin efecto
    Glow,     // Borde que pulsa
    Pulse,    // Ícono que sube y baja
    Shine,    // Destello cruzando el ícono
    Rainbow   // Borde arcoíris (legendarios)
}

public enum GF_VFXShape
{
    Burst,    // Explosión radial
    Rise,     // Sube hacia arriba
    Spiral,   // Espiral ascendente
    Scatter,  // Aleatorio
    Sparkle   // Destellos estáticos
}

// ── Config de VFX ─────────────────────────────────────────────────────────

[Serializable]
public class GF_VFXConfig
{
    public GF_VFXShape shape          = GF_VFXShape.Burst;
    public Color       primaryColor   = Color.white;
    public Color       secondaryColor = new Color(1f, 1f, 1f, 0f);
    [Range(4,60)]  public int   count     = 16;
    [Range(0.2f,3f)] public float duration = 0.7f;
    [Range(0.1f,4f)] public float radius   = 1.2f;
    [Range(0.02f,0.4f)] public float size  = 0.09f;
    public bool useGravity  = false;
    public bool rotate      = true;
}

// ── ScriptableObject ───────────────────────────────────────────────────────

[CreateAssetMenu(fileName = "Item_", menuName = "Greenfall/Item Definition")]
public class GF_ItemDefinition : ScriptableObject
{
    // ── Identidad ─────────────────────────────────────────────────────────
    [Header("Identidad")]
    [Tooltip("ID único. Sin espacios. Ej: 'weapon_ak47', 'ammo_9mm', 'seed_corn'")]
    public string  itemId      = "item_unnamed";
    public string  displayName = "Item";
    [TextArea(1,2)]
    public string  description = "";
    public Sprite  icon;

    [Header("Tipo y Rareza")]
    public GF_ItemType   type   = GF_ItemType.Misc;
    public GF_ItemRarity rarity = GF_ItemRarity.Common;

    // ── Prefab del mundo ──────────────────────────────────────────────────
    [Header("Prefab del Mundo")]
    [Tooltip("Prefab que aparece en el suelo. DEBE tener GF_WorldPickup.\n" +
             "Para armas: también debe tener Weapon.cs.")]
    public GameObject worldPrefab;

    // ── Comportamiento ────────────────────────────────────────────────────
    [Header("Comportamiento")]
    public bool isStackable = false;
    [Min(1)] public int maxStack = 1;
    public bool canDrop      = true;
    public bool consumeOnUse = false;
    [Min(0f)] public float weight = 0.1f;

    // ── Visual de Slot ────────────────────────────────────────────────────
    [Header("Visual del Slot en UI")]
    public Color       slotBgColor     = new Color(0.07f, 0.07f, 0.10f, 0.95f);
    public Color       slotBorderColor = Color.gray;
    public GF_SlotEffect slotEffect    = GF_SlotEffect.None;
    [Range(0f,1f)] public float effectIntensity = 0.5f;

    // ── Audio ─────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0f,1f)] public float pickupVolume = 0.8f;
    public AudioClip equipSound;
    public AudioClip dropSound;

    // ── VFX al recoger ────────────────────────────────────────────────────
    [Header("VFX al Recoger")]
    public GF_VFXConfig pickupVFX = new GF_VFXConfig();

    // ── Sección WEAPON ────────────────────────────────────────────────────
    [Header("─── WEAPON ────────────────────────────────")]
    [Tooltip("WeaponStats del arma. El worldPrefab debe tener Weapon.cs.")]
    public WeaponStats weaponStats;

    // ── Sección AMMO ──────────────────────────────────────────────────────
    [Header("─── AMMO ──────────────────────────────────")]
    public AmmoTypeSO ammoType;
    [Min(1)] public int ammoPerPickup = 30;

    // ── Sección SEED ──────────────────────────────────────────────────────
    [Header("─── SEED ──────────────────────────────────")]
    public SeedItem seedData;

    // ── Sección CONSUMABLE ────────────────────────────────────────────────
    [Header("─── CONSUMABLE ────────────────────────────")]
    public float healthRestore  = 0f;
    public float hungerRestore  = 0f;
    public float staminaRestore = 0f;

    // ── Sección MATERIAL ──────────────────────────────────────────────────
    [Header("─── MATERIAL ─────────────────────────────")]
    public string materialCategory = "";
    [Min(0.1f)] public float craftingValue = 1f;

    // ── Utilidades ────────────────────────────────────────────────────────

    /// <summary>Color de rareza del item para bordes de slot.</summary>
    public Color GetRarityColor() => rarity switch
    {
        GF_ItemRarity.Common    => new Color(0.6f, 0.6f, 0.6f),
        GF_ItemRarity.Uncommon  => new Color(0.1f, 0.75f, 0.2f),
        GF_ItemRarity.Rare      => new Color(0.1f, 0.4f, 0.95f),
        GF_ItemRarity.Epic      => new Color(0.6f, 0.1f, 0.9f),
        GF_ItemRarity.Legendary => new Color(0.95f, 0.75f, 0.1f),
        _                       => Color.white
    };

    private void OnValidate()
    {
        // Ajusta isStackable y canDrop automáticamente según el tipo
        switch (type)
        {
            case GF_ItemType.Weapon:
            case GF_ItemType.Tool:
                isStackable = false;
                maxStack = 1;
                break;
            case GF_ItemType.Ammo:
            case GF_ItemType.Seed:
            case GF_ItemType.Material:
                if (!isStackable) { isStackable = true; if (maxStack <= 1) maxStack = 99; }
                break;
            case GF_ItemType.QuestItem:
                canDrop = false;
                break;
        }

        // Sincronizar borde con rareza por defecto
        slotBorderColor = GetRarityColor();

        if (icon == null)
            Debug.LogWarning($"[GF_ItemDefinition] '{name}': sin icono.", this);
    }
}