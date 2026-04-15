// ============================================================
//  GF_InventoryConfig.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Crear: Click derecho → Create → Greenfall → Inventory Config
//
//  Controla TODA la apariencia y layout del inventario.
//  Puedes tener varios assets y cambiar entre ellos desde
//  la ventana de editor (Greenfall → Inventory Editor).
// ============================================================

using System;
using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "InventoryConfig_", menuName = "Greenfall/Inventory Config")]
public class GF_InventoryConfig : ScriptableObject
{
    // ── Layout General ────────────────────────────────────────────────────
    [Header("Layout — Slots")]
    [Tooltip("Número de slots en la hotbar (barra inferior).")]
    [Range(1,10)] public int hotbarSlotCount = 5;

    [Tooltip("Número de columnas en el inventario completo (panel con Tab).")]
    [Range(2,8)] public int bagColumns = 5;

    [Tooltip("Número de filas en el inventario completo.")]
    [Range(2,8)] public int bagRows = 4;

    [Tooltip("Tamaño en píxeles de cada slot (ancho y alto iguales).")]
    [Range(40,120)] public float slotSize = 64f;

    [Tooltip("Separación entre slots en píxeles.")]
    [Range(0,16)] public float slotSpacing = 4f;

    [Tooltip("Padding interno del ícono dentro del slot (0=ícono ocupa todo el slot).")]
    [Range(0,20)] public float iconPadding = 6f;

    // ── Posición de la Hotbar ─────────────────────────────────────────────
    [Header("Layout — Posición Hotbar")]
    [Tooltip("Offset en píxeles desde el centro inferior de la pantalla.")]
    public Vector2 hotbarOffset = new Vector2(0f, 20f);

    // ── Posición del Panel de Inventario ──────────────────────────────────
    [Header("Layout — Panel de Inventario")]
    [Tooltip("Offset del panel de inventario completo desde el centro de la pantalla.")]
    public Vector2 bagPanelOffset = Vector2.zero;

    [Tooltip("Padding del panel de inventario (espacio interior).")]
    public Vector2 bagPanelPadding = new Vector2(16f, 16f);

    // ── Colores de Slot ───────────────────────────────────────────────────
    [Header("Colores — Slot")]
    public Color emptySlotBg       = new Color(0.06f, 0.06f, 0.09f, 0.92f);
    public Color emptySlotBorder   = new Color(0.25f, 0.25f, 0.30f, 0.70f);
    public Color selectedBorder    = new Color(0.90f, 0.80f, 0.20f, 1.00f);
    public Color selectedBgOverlay = new Color(1.00f, 0.95f, 0.40f, 0.12f);

    [Tooltip("Cuántos píxeles sube el ícono cuando el slot está seleccionado.")]
    [Range(0,20)] public float selectedLiftPx = 8f;

    [Tooltip("Velocidad de la animación de selección.")]
    [Range(4,20)] public float liftSpeed = 12f;

    // ── Colores del Panel ─────────────────────────────────────────────────
    [Header("Colores — Panel de Inventario")]
    public Color panelBgColor     = new Color(0.05f, 0.05f, 0.08f, 0.94f);
    public Color panelBorderColor = new Color(0.20f, 0.20f, 0.25f, 1.00f);
    [Range(1,6)] public float panelBorderWidth = 2f;

    // ── Animación del Panel ───────────────────────────────────────────────
    [Header("Animación — Fade del Panel")]
    [Range(4,20)] public float fadeSpeed = 10f;

    // ── Texto ─────────────────────────────────────────────────────────────
    [Header("Texto")]
    [Tooltip("Fuente para todo el texto del inventario. Asignar en Inspector.")]
    public TMP_FontAsset font;

    [Range(8,20)]  public float amountTextSize      = 11f;
    [Range(10,22)] public float interactionTextSize  = 15f;
    [Range(10,22)] public float inventoryFullTextSize = 14f;

    public Color amountTextColor       = Color.white;
    public Color interactionTextColor  = Color.white;
    public Color inventoryFullColor    = new Color(1f, 0.3f, 0.3f, 1f);

    // ── Texto de Interacción ──────────────────────────────────────────────
    [Header("Texto de Interacción")]
    public string interactionKeyLabel  = "[E]";
    public string inventoryFullMessage = "Inventario lleno";

    // ── Borde del slot de arma activa ─────────────────────────────────────
    [Header("Arma Activa")]
    [Tooltip("Si true, el slot activo del arma tiene un borde pulsante adicional.")]
    public bool activeWeaponPulse   = true;
    public Color activeWeaponColor  = new Color(0.90f, 0.80f, 0.20f, 0.80f);

    // ── Input ─────────────────────────────────────────────────────────────
    [Header("Input (teclas de respaldo)")]
    public KeyCode toggleInventoryKey = KeyCode.Tab;
    public KeyCode interactKey        = KeyCode.E;
    public KeyCode dropKey            = KeyCode.G;
    public KeyCode reloadKey          = KeyCode.R;

    // ── Nombre del tema (para mostrar en editor) ──────────────────────────
    [Header("Metadatos")]
    [Tooltip("Nombre descriptivo de este tema visual. Solo para organización.")]
    public string themeName = "Default";
    [TextArea(1,2)]
    public string themeNotes = "";
}