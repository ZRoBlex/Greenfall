// ============================================================
//  GF_InventoryEditorWindow.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/Editor/
//
//  ABRIR: Menú Unity → Greenfall → Inventory Editor (Ctrl+Shift+I)
//
//  PESTAÑAS:
//  ITEMS      → Lista todos los GF_ItemDefinition del proyecto
//              Filtro por tipo/rareza, búsqueda, creación rápida
//  CONFIG     → Edita el GF_InventoryConfig activo con preview
//  TEMAS      → Guarda/carga configuraciones visuales completas
//  DEBUG      → (Play Mode) Estado del inventario en tiempo real
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GF_InventoryEditorWindow : EditorWindow
{
    // ── Estado ────────────────────────────────────────────────────────────
    private enum Tab { Items, Config, Themes, Debug }
    private Tab _tab = Tab.Items;

    private Vector2 _scrollItems;
    private Vector2 _scrollDebug;
    private Vector2 _scrollConfig;

    // Items
    private List<GF_ItemDefinition> _items    = new();
    private bool                    _dirty    = true;
    private string                  _search   = "";
    private GF_ItemType?            _fType    = null;
    private GF_ItemRarity?          _fRarity  = null;
    private GF_ItemDefinition       _selected;

    // Config
    private GF_InventoryConfig _activeConfig;
    private SerializedObject   _configSO;

    // Themes
    private string _themeName = "Nuevo Tema";

    // Colores
    static readonly Color C_HEADER = new Color(0.07f, 0.09f, 0.11f);
    static readonly Color C_TAB_ON = new Color(0.15f, 0.45f, 0.80f);
    static readonly Color C_TAB_OFF= new Color(0.16f, 0.16f, 0.20f);
    static readonly Color C_GREEN  = new Color(0.15f, 0.60f, 0.22f);
    static readonly Color C_RED    = new Color(0.65f, 0.15f, 0.15f);
    static readonly Color C_AMBER  = new Color(0.80f, 0.55f, 0.10f);
    static readonly Color C_ROW_A  = new Color(0.10f, 0.10f, 0.13f, 0.9f);
    static readonly Color C_ROW_B  = new Color(0.12f, 0.12f, 0.16f, 0.9f);
    static readonly Color C_SEL    = new Color(0.14f, 0.30f, 0.55f, 0.9f);

    // ── Abrir ─────────────────────────────────────────────────────────────

    [MenuItem("Greenfall/Inventory Editor  %#i")]
    public static void Open()
    {
        var w = GetWindow<GF_InventoryEditorWindow>("Inventory");
        w.titleContent = new GUIContent("GF Inventory");
        w.minSize      = new Vector2(380f, 520f);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _dirty = true;
        AutoDetectConfig();
    }

    private void OnFocus() { _dirty = true; AutoDetectConfig(); }

    private void AutoDetectConfig()
    {
        if (_activeConfig != null) return;
        var guids = AssetDatabase.FindAssets("t:GF_InventoryConfig");
        if (guids.Length > 0)
        {
            _activeConfig = AssetDatabase.LoadAssetAtPath<GF_InventoryConfig>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            if (_activeConfig != null)
                _configSO = new SerializedObject(_activeConfig);
        }
    }

    // ── Main GUI ──────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        DrawTabBar();
        GUILayout.Space(4);

        switch (_tab)
        {
            case Tab.Items:  DrawItemsTab();  break;
            case Tab.Config: DrawConfigTab(); break;
            case Tab.Themes: DrawThemesTab(); break;
            case Tab.Debug:  DrawDebugTab();  break;
        }
    }

    // ── Header ────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        var r = EditorGUILayout.GetControlRect(false, 38f);
        EditorGUI.DrawRect(r, C_HEADER);
        GUI.Label(r, "    GREENFALL  ·  INVENTORY EDITOR",
            new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14, alignment = TextAnchor.MiddleLeft,
                normal   = { textColor = new Color(0.7f, 0.88f, 1f) }
            });
    }

    private void DrawTabBar()
    {
        EditorGUILayout.BeginHorizontal();
        DrawTabBtn("Items",   Tab.Items);
        DrawTabBtn("Config",  Tab.Config);
        DrawTabBtn("Temas",   Tab.Themes);
        DrawTabBtn("Debug",   Tab.Debug);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTabBtn(string label, Tab t)
    {
        GUI.backgroundColor = _tab == t ? C_TAB_ON : C_TAB_OFF;
        if (GUILayout.Button(label, GUILayout.Height(26))) _tab = t;
        GUI.backgroundColor = Color.white;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB — ITEMS
    // ═════════════════════════════════════════════════════════════════════

    private void DrawItemsTab()
    {
        if (_dirty) { LoadItems(); _dirty = false; }

        // Toolbar
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = C_GREEN;
        if (GUILayout.Button("+ Crear Item Definition", GUILayout.Height(24)))
            CreateItem();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("↻ Refrescar", GUILayout.Width(80), GUILayout.Height(24)))
            _dirty = true;
        EditorGUILayout.EndHorizontal();

        // Búsqueda
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Buscar:", GUILayout.Width(50));
        _search = EditorGUILayout.TextField(_search);
        if (GUILayout.Button("✕", GUILayout.Width(22))) _search = "";
        EditorGUILayout.EndHorizontal();

        // Filtro de tipo
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Tipo:", GUILayout.Width(36));
        DrawFilterBtn("Todos", null, ref _fType);
        foreach (GF_ItemType t in System.Enum.GetValues(typeof(GF_ItemType)))
            DrawFilterBtn(t.ToString(), t, ref _fType);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);

        // Lista
        var filtered = FilterItems();
        EditorGUILayout.LabelField($"{filtered.Count} items", EditorStyles.miniLabel);
        GUILayout.Space(2);

        _scrollItems = EditorGUILayout.BeginScrollView(_scrollItems);
        for (int i = 0; i < filtered.Count; i++)
            DrawItemRow(filtered[i], i % 2 == 0 ? C_ROW_A : C_ROW_B);
        EditorGUILayout.EndScrollView();
    }

    private void DrawFilterBtn<T>(string lbl, T? val, ref T? state) where T : struct
    {
        bool active = val == null ? state == null : state != null && state.Value.Equals(val.Value);
        GUI.backgroundColor = active ? C_TAB_ON : C_TAB_OFF;
        if (GUILayout.Button(lbl, GUILayout.Height(18)))
            state = val;
        GUI.backgroundColor = Color.white;
    }

    private void DrawItemRow(GF_ItemDefinition item, Color rowBg)
    {
        var r = EditorGUILayout.GetControlRect(false, 46f);
        EditorGUI.DrawRect(r, item == _selected ? C_SEL : rowBg);

        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            _selected = item;
            Selection.activeObject = item;
            Event.current.Use();
            Repaint();
        }

        float x = r.x + 4f, y = r.y + 2f;

        // Ícono
        if (item.icon != null)
            GUI.DrawTexture(new Rect(x, y + 4f, 38f, 38f), item.icon.texture, ScaleMode.ScaleToFit);
        else
        {
            var iconR = new Rect(x, y + 4f, 38f, 38f);
            EditorGUI.DrawRect(iconR, item.GetRarityColor() * 0.5f);
        }
        x += 46f;

        // Nombre + tipo
        GUI.Label(new Rect(x, y + 2f, 200f, 18f), item.displayName,
            new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } });
        GUI.Label(new Rect(x, y + 22f, 200f, 16f),
            $"{item.type}  ·  {item.rarity}",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = item.GetRarityColor() } });

        // ID
        GUI.Label(new Rect(x + 205f, y + 2f, 140f, 14f), item.itemId,
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.45f,0.45f,0.5f) } });

        // Botón inspector
        if (GUI.Button(new Rect(r.xMax - 88f, y + 10f, 80f, 24f), "Inspector"))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB — CONFIG
    // ═════════════════════════════════════════════════════════════════════

    private void DrawConfigTab()
    {
        // Selector de config
        EditorGUI.BeginChangeCheck();
        _activeConfig = (GF_InventoryConfig)EditorGUILayout.ObjectField(
            "Config activa", _activeConfig, typeof(GF_InventoryConfig), false);
        if (EditorGUI.EndChangeCheck() && _activeConfig != null)
            _configSO = new SerializedObject(_activeConfig);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = C_GREEN;
        if (GUILayout.Button("Crear nueva Config"))
            CreateConfig();
        GUI.backgroundColor = Color.white;
        if (_activeConfig != null && GUILayout.Button("Abrir Inspector", GUILayout.Width(110)))
            Selection.activeObject = _activeConfig;
        EditorGUILayout.EndHorizontal();

        if (_activeConfig == null)
        {
            EditorGUILayout.HelpBox("Crea o selecciona un GF_InventoryConfig.", MessageType.Info);
            return;
        }

        GUILayout.Space(6);

        // Aplicar al GF_Inventory de la escena
        var inv = FindFirstObjectByType<GF_Inventory>();
        if (inv != null && inv.config != _activeConfig)
        {
            GUI.backgroundColor = C_AMBER;
            if (GUILayout.Button("▶  Aplicar esta Config al GF_Inventory de la escena"))
            {
                Undo.RecordObject(inv, "Assign Config");
                inv.config = _activeConfig;
                EditorUtility.SetDirty(inv);
            }
            GUI.backgroundColor = Color.white;
        }

        GUILayout.Space(4);

        // Editar el config
        if (_configSO == null) _configSO = new SerializedObject(_activeConfig);
        _configSO.Update();

        _scrollConfig = EditorGUILayout.BeginScrollView(_scrollConfig);
        DrawDefaultInspectorExceptScript(_configSO);
        EditorGUILayout.EndScrollView();

        if (_configSO.ApplyModifiedProperties())
            EditorUtility.SetDirty(_activeConfig);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB — TEMAS
    // ═════════════════════════════════════════════════════════════════════

    private void DrawThemesTab()
    {
        EditorGUILayout.HelpBox(
            "Los Temas son simplemente assets GF_InventoryConfig.\n" +
            "Crea varios con nombres distintos y cambia entre ellos asignando\n" +
            "el que quieras al campo 'config' de GF_Inventory y GF_InventoryUI.",
            MessageType.Info);

        GUILayout.Space(4);

        // Listar configs existentes
        var guids = AssetDatabase.FindAssets("t:GF_InventoryConfig");
        EditorGUILayout.LabelField($"{guids.Length} temas encontrados:", EditorStyles.boldLabel);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg  = AssetDatabase.LoadAssetAtPath<GF_InventoryConfig>(path);
            if (cfg == null) continue;

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(cfg.themeName, EditorStyles.boldLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField(cfg.themeNotes, EditorStyles.miniLabel);

            GUI.backgroundColor = cfg == _activeConfig ? C_TAB_ON : Color.white;
            if (GUILayout.Button("Seleccionar", GUILayout.Width(90)))
            {
                _activeConfig = cfg;
                _configSO     = new SerializedObject(cfg);
                _tab          = Tab.Config;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(8);
        GUI.backgroundColor = C_GREEN;
        if (GUILayout.Button("+ Crear Nuevo Tema"))
            CreateConfig();
        GUI.backgroundColor = Color.white;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAB — DEBUG
    // ═════════════════════════════════════════════════════════════════════

    private void DrawDebugTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Solo disponible en Play Mode.", MessageType.Info);
            return;
        }

        var inv = GF_Inventory.Instance;
        if (inv == null)
        {
            EditorGUILayout.HelpBox("No hay GF_Inventory en escena.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(
            $"Estado: {inv.GameState}  |  Slot activo: {inv.ActiveIndex}",
            EditorStyles.boldLabel);

        GUILayout.Space(4);

        // Hotbar
        DrawDebugSection("HOTBAR", 0, inv.hotbarSize, inv);
        GUILayout.Space(6);
        DrawDebugSection("BOLSA", inv.hotbarSize, inv.TotalSlots, inv);

        Repaint(); // Live refresh
    }

    private void DrawDebugSection(string title, int from, int to, GF_Inventory inv)
    {
        var hr = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.DrawRect(hr, new Color(0.10f, 0.10f, 0.14f));
        GUI.Label(hr, $"  {title}", EditorStyles.boldLabel);

        _scrollDebug = EditorGUILayout.BeginScrollView(_scrollDebug,
            GUILayout.Height(Mathf.Min((to - from) * 22f + 4f, 200f)));

        for (int i = from; i < to; i++)
        {
            var slot   = inv.GetSlot(i);
            bool active = i == inv.ActiveIndex;
            string local = inv.IsHotbarSlot(i) ? $"H[{i}]" : $"B[{i - inv.hotbarSize}]";

            var r = EditorGUILayout.GetControlRect(false, 20f);
            if (active) EditorGUI.DrawRect(r, new Color(0.9f, 0.8f, 0.1f, 0.15f));

            string content = slot.IsEmpty
                ? "(vacío)"
                : $"{slot.definition.displayName}  x{slot.amount}  [{slot.definition.type}]";

            GUI.Label(new Rect(r.x + 4, r.y + 2, 55f, 16f), local, EditorStyles.miniLabel);
            GUI.Label(new Rect(r.x + 62f, r.y + 2, r.width - 70f, 16f),
                content + (active ? "  ◄" : ""),
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = slot.IsEmpty ? Color.gray : Color.white }
                });
        }

        EditorGUILayout.EndScrollView();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  UTILIDADES
    // ═════════════════════════════════════════════════════════════════════

    private void LoadItems()
    {
        _items.Clear();
        var guids = AssetDatabase.FindAssets("t:GF_ItemDefinition");
        foreach (var g in guids)
        {
            var item = AssetDatabase.LoadAssetAtPath<GF_ItemDefinition>(
                AssetDatabase.GUIDToAssetPath(g));
            if (item != null) _items.Add(item);
        }
        _items = _items.OrderBy(x => (int)x.type).ThenBy(x => x.displayName).ToList();
    }

    private List<GF_ItemDefinition> FilterItems()
    {
        return _items.Where(item =>
        {
            if (_fType   != null && item.type   != _fType)   return false;
            if (_fRarity != null && item.rarity != _fRarity) return false;
            if (!string.IsNullOrEmpty(_search))
            {
                string q = _search.ToLower();
                return item.displayName.ToLower().Contains(q)
                    || item.itemId.ToLower().Contains(q);
            }
            return true;
        }).ToList();
    }

    private void CreateItem()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Crear Item Definition", "Item_NuevoItem", "asset", "");
        if (string.IsNullOrEmpty(path)) return;

        var item = CreateInstance<GF_ItemDefinition>();
        item.itemId      = "item_" + System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
        item.displayName = System.IO.Path.GetFileNameWithoutExtension(path);

        AssetDatabase.CreateAsset(item, path);
        AssetDatabase.SaveAssets();
        _dirty    = true;
        _selected = item;
        Selection.activeObject = item;
        Debug.Log($"[GF_Editor] Creado: {path}");
    }

    private void CreateConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Crear Inventory Config", "InventoryConfig_Default", "asset", "");
        if (string.IsNullOrEmpty(path)) return;

        var cfg = CreateInstance<GF_InventoryConfig>();
        cfg.themeName = System.IO.Path.GetFileNameWithoutExtension(path);

        AssetDatabase.CreateAsset(cfg, path);
        AssetDatabase.SaveAssets();

        _activeConfig = cfg;
        _configSO     = new SerializedObject(cfg);
        Selection.activeObject = cfg;
    }

    // Dibuja el serialized object sin el campo "Script" (más limpio)
    private static void DrawDefaultInspectorExceptScript(SerializedObject so)
    {
        var iter = so.GetIterator();
        iter.NextVisible(true);
        while (iter.NextVisible(false))
        {
            if (iter.name == "m_Script") continue;
            EditorGUILayout.PropertyField(iter, true);
        }
    }
}
#endif