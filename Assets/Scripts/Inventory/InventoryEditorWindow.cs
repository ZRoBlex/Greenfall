// ============================================================
//  InventoryEditorWindow.cs
//  Greenfall: The Last Harvest
//  Carpeta: Assets/Greenfall/Inventory/Editor/
//
//  ABRIR: Menú Unity → Greenfall → Inventory Editor
//  (O Ctrl+Shift+I)
//
//  QUÉ HACE:
//  Ventana de editor para gestionar todo el sistema de inventario
//  sin tocar código ni el Project view. Desde aquí puedes:
//
//  TAB "Items":
//   - Ver todos los InventoryItemData del proyecto
//   - Filtrar por tipo y rareza
//   - Crear nuevos items con un click
//   - Editar propiedades básicas inline
//   - Abrir el inspector completo del item seleccionado
//
//  TAB "Inventario (Play Mode)":
//   - Ver el contenido actual del inventario en tiempo real
//   - Solo disponible en Play Mode (mientras el juego corre)
//   - Muestra qué hay en cada slot (hotbar y bolsa)
//   - Indica el slot activo
//
//  TAB "Configuración":
//   - Acceso rápido al InventorySystem de la escena
//   - Ver y editar hotbarSize y bagSize
//   - Debug: imprimir inventario a Console
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class InventoryEditorWindow : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO DE LA VENTANA
    // ─────────────────────────────────────────────────────────────────────

    private enum Tab { Items, RuntimeInventory, Config }
    private Tab    _activeTab       = Tab.Items;
    private Vector2 _scrollItems;
    private Vector2 _scrollRuntime;

    // Tab Items
    private List<InventoryItemData> _allItems = new();
    private InventoryItemData       _selectedItem;
    private string                  _searchQuery   = "";
    private ItemType?               _filterType    = null;
    private ItemRarity?             _filterRarity  = null;
    private bool                    _itemsDirty    = true; // necesita recargar

    // Colores de la UI
    private static readonly Color C_HEADER  = new Color(0.08f, 0.10f, 0.12f);
    private static readonly Color C_TAB_ACTIVE = new Color(0.15f, 0.45f, 0.80f);
    private static readonly Color C_TAB_IDLE   = new Color(0.18f, 0.18f, 0.22f);
    private static readonly Color C_GREEN    = new Color(0.15f, 0.60f, 0.25f);
    private static readonly Color C_RED      = new Color(0.65f, 0.15f, 0.15f);
    private static readonly Color C_SECTION  = new Color(0.12f, 0.12f, 0.16f);

    // ─────────────────────────────────────────────────────────────────────
    //  ABRIR VENTANA
    // ─────────────────────────────────────────────────────────────────────

    [MenuItem("Greenfall/Inventory Editor  %#i")]
    public static void Open()
    {
        var window = GetWindow<InventoryEditorWindow>("Inventory");
        window.titleContent = new GUIContent("Inventory Editor");
        window.minSize = new Vector2(360f, 500f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ON ENABLE / FOCUS
    // ─────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _itemsDirty = true;
    }

    private void OnFocus()
    {
        _itemsDirty = true; // Recargar items al traer la ventana al frente
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MAIN GUI
    // ─────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        DrawTabs();
        GUILayout.Space(4);

        switch (_activeTab)
        {
            case Tab.Items:           DrawItemsTab();   break;
            case Tab.RuntimeInventory: DrawRuntimeTab(); break;
            case Tab.Config:          DrawConfigTab();  break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HEADER
    // ─────────────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        var rect = EditorGUILayout.GetControlRect(false, 36f);
        EditorGUI.DrawRect(rect, C_HEADER);
        GUI.Label(rect, "    INVENTORY EDITOR — GREENFALL",
            new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) }
            });
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TABS
    // ─────────────────────────────────────────────────────────────────────

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal();

        DrawTab("Items",             Tab.Items);
        DrawTab("Inventario (Play)", Tab.RuntimeInventory);
        DrawTab("Configuración",     Tab.Config);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTab(string label, Tab tab)
    {
        GUI.backgroundColor = _activeTab == tab ? C_TAB_ACTIVE : C_TAB_IDLE;
        if (GUILayout.Button(label, GUILayout.Height(26)))
            _activeTab = tab;
        GUI.backgroundColor = Color.white;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TAB: ITEMS
    // ─────────────────────────────────────────────────────────────────────

    private void DrawItemsTab()
    {
        // Recargar si es necesario
        if (_itemsDirty)
        {
            LoadAllItems();
            _itemsDirty = false;
        }

        // ── Toolbar: crear + buscar ───────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = C_GREEN;
        if (GUILayout.Button("+ Crear Item Data", GUILayout.Height(24)))
            CreateNewItemData();
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Refrescar", GUILayout.Width(80), GUILayout.Height(24)))
            _itemsDirty = true;

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);

        // ── Buscador ──────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Buscar:", GUILayout.Width(50));
        _searchQuery = EditorGUILayout.TextField(_searchQuery);
        if (GUILayout.Button("X", GUILayout.Width(20))) _searchQuery = "";
        EditorGUILayout.EndHorizontal();

        // ── Filtros de tipo y rareza ───────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Tipo:", GUILayout.Width(40));

        // Botón "Todos"
        GUI.backgroundColor = _filterType == null ? C_TAB_ACTIVE : C_TAB_IDLE;
        if (GUILayout.Button("Todos", GUILayout.Height(20))) _filterType = null;
        GUI.backgroundColor = Color.white;

        // Un botón por cada tipo
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            GUI.backgroundColor = _filterType == type ? C_TAB_ACTIVE : C_TAB_IDLE;
            if (GUILayout.Button(type.ToString(), GUILayout.Height(20)))
                _filterType = (_filterType == type) ? (ItemType?)null : type;
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2);

        // ── Lista de items ─────────────────────────────────────────────────
        var filtered = FilterItems();

        EditorGUILayout.LabelField($"{filtered.Count} items encontrados",
            EditorStyles.miniLabel);
        GUILayout.Space(2);

        _scrollItems = EditorGUILayout.BeginScrollView(_scrollItems);

        foreach (var item in filtered)
        {
            DrawItemRow(item);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawItemRow(InventoryItemData item)
    {
        bool isSelected = item == _selectedItem;

        var rowRect = EditorGUILayout.GetControlRect(false, 44f);

        // Fondo de la fila
        EditorGUI.DrawRect(rowRect,
            isSelected ? new Color(0.15f, 0.30f, 0.50f, 0.8f) : new Color(0.12f, 0.12f, 0.15f, 0.6f));

        // Click para seleccionar
        if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
        {
            _selectedItem = item;
            Selection.activeObject = item;
            Event.current.Use();
            Repaint();
        }

        float x = rowRect.x + 4f;
        float y = rowRect.y + 2f;

        // Ícono del item (si tiene)
        if (item.icon != null)
        {
            var iconRect = new Rect(x, y + 4f, 36f, 36f);
            GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit);
            x += 44f;
        }
        else
        {
            // Placeholder coloreado por rareza
            var iconRect = new Rect(x, y + 4f, 36f, 36f);
            EditorGUI.DrawRect(iconRect, item.GetRarityColor() * 0.5f);
            x += 44f;
        }

        // Nombre
        GUI.Label(new Rect(x, y + 2f, 180f, 18f),
            item.displayName,
            new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } });

        // Tipo + rareza
        Color rarityColor = item.GetRarityColor();
        GUI.Label(new Rect(x, y + 22f, 180f, 16f),
            $"{item.type}  |  {item.rarity}",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = rarityColor } });

        // ID
        GUI.Label(new Rect(x + 185f, y + 2f, 160f, 16f),
            item.itemId,
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.55f) } });

        // Botón: Abrir Inspector
        if (GUI.Button(new Rect(rowRect.xMax - 90f, y + 8f, 80f, 24f), "Inspector"))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TAB: RUNTIME INVENTORY
    // ─────────────────────────────────────────────────────────────────────

    private void DrawRuntimeTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Este panel muestra el inventario en tiempo real.\n" +
                "Solo está disponible en Play Mode.",
                MessageType.Info);
            return;
        }

        if (InventorySystem.Instance == null)
        {
            EditorGUILayout.HelpBox("No hay InventorySystem en escena.", MessageType.Warning);
            return;
        }

        var inv = InventorySystem.Instance;

        EditorGUILayout.LabelField(
            $"Slots: {inv.HotbarSize} hotbar + {inv.BagSize} bolsa = {inv.TotalSlots} total",
            EditorStyles.miniLabel);
        GUILayout.Space(4);

        _scrollRuntime = EditorGUILayout.BeginScrollView(_scrollRuntime);

        // Hotbar
        DrawRuntimeSection("HOTBAR", 0, inv.HotbarSize, inv);

        GUILayout.Space(8);

        // Bolsa
        DrawRuntimeSection("BOLSA", inv.HotbarSize, inv.TotalSlots, inv);

        EditorGUILayout.EndScrollView();

        // Auto-repaint en Play Mode para ver cambios en tiempo real
        Repaint();
    }

    private void DrawRuntimeSection(string title, int from, int to, InventorySystem inv)
    {
        var headerRect = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.DrawRect(headerRect, C_SECTION);
        GUI.Label(headerRect, $"  {title}", EditorStyles.boldLabel);

        for (int i = from; i < to; i++)
        {
            var slot = inv.GetSlot(i);
            bool isActive = i == inv.ActiveSlotIndex;
            string mark   = isActive ? " ◄ ACTIVO" : "";
            string local  = inv.IsHotbarSlot(i) ? $"H[{i}]" : $"B[{i - inv.HotbarSize}]";

            var rowRect = EditorGUILayout.GetControlRect(false, 20f);
            if (isActive) EditorGUI.DrawRect(rowRect, new Color(0.9f, 0.8f, 0.1f, 0.15f));

            string content = slot.IsEmpty
                ? "(vacío)"
                : $"{slot.itemData.displayName}  x{slot.amount}  [{slot.itemData.type}]";

            GUI.Label(new Rect(rowRect.x + 4, rowRect.y + 2, 60f, 16f),
                local, EditorStyles.miniLabel);
            GUI.Label(new Rect(rowRect.x + 70f, rowRect.y + 2, rowRect.width - 80f, 16f),
                content + mark,
                new GUIStyle(EditorStyles.miniLabel) {
                    normal = { textColor = slot.IsEmpty ? Color.gray : Color.white }
                });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TAB: CONFIG
    // ─────────────────────────────────────────────────────────────────────

    private void DrawConfigTab()
    {
        GUILayout.Space(4);

        EditorGUILayout.LabelField("Sistema en Escena", EditorStyles.boldLabel);

        var invSystem = FindFirstObjectByType<InventorySystem>();
        if (invSystem == null)
        {
            EditorGUILayout.HelpBox("No hay InventorySystem en la escena activa.", MessageType.Warning);
            GUI.backgroundColor = C_GREEN;
            if (GUILayout.Button("Crear InventorySystem en escena"))
            {
                var go = new GameObject("InventorySystem");
                Undo.RegisterCreatedObjectUndo(go, "Create InventorySystem");
                go.AddComponent<InventorySystem>();
                go.AddComponent<PickupVFXSystem>();
                Selection.activeGameObject = go;
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField("InventorySystem", GUILayout.Width(130));
                if (GUILayout.Button("Seleccionar", GUILayout.Width(90)))
                    Selection.activeGameObject = invSystem.gameObject;
            }

            if (Application.isPlaying)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
                if (GUILayout.Button("Imprimir Inventario a Console"))
                {
                    // Llamamos el ContextMenu Debug a través de SendMessage
                    invSystem.SendMessage("DebugPrintInventory", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Documentación Rápida", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "CREAR NUEVO ITEM:\n" +
            "• Click derecho en Project → Create → Greenfall → Inventory → Item Data\n\n" +
            "CONFIGURAR ITEM:\n" +
            "• Asigna: itemId (único), displayName, icon, type, rarity\n" +
            "• Para armas: asigna weaponStats y worldPrefab con Weapon\n" +
            "• Para ammo: asigna ammoType, isStackable=true, maxStack\n\n" +
            "COLOCAR EN MUNDO:\n" +
            "• Crea prefab con InventoryItemController\n" +
            "• Asigna el Item Data creado\n\n" +
            "INTERACCIÓN:\n" +
            "• El jugador necesita PlayerInteractor en su GameObject",
            MessageType.None);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ─────────────────────────────────────────────────────────────────────

    private void LoadAllItems()
    {
        _allItems.Clear();
        var guids = AssetDatabase.FindAssets("t:InventoryItemData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
            if (item != null) _allItems.Add(item);
        }

        // Ordenar: primero por tipo, luego por nombre
        _allItems = _allItems
            .OrderBy(i => (int)i.type)
            .ThenBy(i => i.displayName)
            .ToList();
    }

    private List<InventoryItemData> FilterItems()
    {
        return _allItems.Where(item =>
        {
            if (_filterType != null && item.type != _filterType) return false;
            if (_filterRarity != null && item.rarity != _filterRarity) return false;
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string q = _searchQuery.ToLower();
                return item.displayName.ToLower().Contains(q)
                    || item.itemId.ToLower().Contains(q)
                    || item.description.ToLower().Contains(q);
            }
            return true;
        }).ToList();
    }

    private void CreateNewItemData()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Crear Item Data",
            "Item_NuevoItem",
            "asset",
            "Elige dónde guardar el Item Data");

        if (string.IsNullOrEmpty(path)) return;

        var newItem = CreateInstance<InventoryItemData>();
        newItem.itemId      = "item_" + System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
        newItem.displayName = System.IO.Path.GetFileNameWithoutExtension(path);

        AssetDatabase.CreateAsset(newItem, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _itemsDirty = true;
        _selectedItem = newItem;
        Selection.activeObject = newItem;

        Debug.Log($"[InventoryEditor] Item Data creado: {path}");
    }
}
#endif