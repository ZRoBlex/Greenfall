//#if UNITY_EDITOR
//using UnityEditor;
//using UnityEngine;

//[CustomEditor(typeof(StructureConfig))]
//public class StructureConfigEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        StructureConfig config = (StructureConfig)target;

//        DrawDefaultInspector();

//        GUILayout.Space(10);
//        GUILayout.Label("Grid Occupancy Editor", EditorStyles.boldLabel);

//        for (int y = config.gridSize.y - 1; y >= 0; y--)
//        {
//            GUILayout.BeginHorizontal();
//            for (int x = 0; x < config.gridSize.x; x++)
//            {
//                Vector3Int cell = new Vector3Int(x, y, 0);
//                bool occupied = config.occupiedCells.Contains(cell);

//                GUI.backgroundColor = occupied ? Color.green : Color.gray;

//                if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(25)))
//                {
//                    if (occupied)
//                        config.occupiedCells.Remove(cell);
//                    else
//                        config.occupiedCells.Add(cell);

//                    EditorUtility.SetDirty(config);
//                }
//            }
//            GUILayout.EndHorizontal();
//        }

//        GUI.backgroundColor = Color.white;
//    }
//}
//#endif

//using UnityEditor;
//using UnityEngine;
//using System.Collections.Generic;

//[CustomEditor(typeof(StructureConfig))]
//public class StructureConfigEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();

//        StructureConfig config = (StructureConfig)target;

//        if (GUILayout.Button("Generate Occupied Cells"))
//        {
//            config.occupiedCells = new List<Vector3Int>();

//            for (int x = 0; x < config.gridSize.x; x++)
//                for (int y = 0; y < config.gridSize.y; y++)
//                    for (int z = 0; z < config.gridSize.z; z++)
//                    {
//                        config.occupiedCells.Add(new Vector3Int(x, y, z));
//                    }

//            EditorUtility.SetDirty(config);
//        }
//    }
//}

//using UnityEditor;
//using UnityEngine;
//using System.Collections.Generic;

//[CustomEditor(typeof(StructureConfig))]
//public class StructureConfigEditor : Editor
//{
//    StructureConfig config;
//    const float cellSize = 20f;

//    void OnEnable()
//    {
//        config = (StructureConfig)target;
//    }

//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();

//        GUILayout.Space(10);
//        GUILayout.Label("Cell Occupation Editor", EditorStyles.boldLabel);

//        if (config.gridBounds.x <= 0 ||
//            config.gridBounds.y <= 0 ||
//            config.gridBounds.z <= 0)
//        {
//            EditorGUILayout.HelpBox(
//                "Grid Bounds must be greater than 0",
//                MessageType.Warning
//            );
//            return;
//        }

//        DrawGridEditor();
//    }

//    void DrawGridEditor()
//    {
//        for (int y = config.gridBounds.y - 1; y >= 0; y--)
//        {
//            GUILayout.Label($"Layer Y = {y}");

//            for (int z = 0; z < config.gridBounds.z; z++)
//            {
//                GUILayout.BeginHorizontal();

//                for (int x = 0; x < config.gridBounds.x; x++)
//                {
//                    Vector3Int cell = new Vector3Int(x, y, z);
//                    bool occupied = config.occupiedCells.Contains(cell);

//                    GUI.backgroundColor = occupied ? Color.cyan : Color.gray;

//                    if (GUILayout.Button("", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
//                    {
//                        ToggleCell(cell);
//                    }
//                }

//                GUILayout.EndHorizontal();
//            }

//            GUILayout.Space(5);
//        }

//        GUI.backgroundColor = Color.white;
//    }

//    void ToggleCell(Vector3Int cell)
//    {
//        Undo.RecordObject(config, "Toggle Cell");

//        if (config.occupiedCells.Contains(cell))
//            config.occupiedCells.Remove(cell);
//        else
//            config.occupiedCells.Add(cell);

//        EditorUtility.SetDirty(config);
//    }
//}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StructureConfig))]
public class StructureConfigEditor : Editor
{
    StructureConfig config;
    int selectedLayer = 0;
    const int cellSize = 24;

    void OnEnable()
    {
        config = (StructureConfig)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        if (config.gridBounds.x <= 0 ||
            config.gridBounds.y <= 0 ||
            config.gridBounds.z <= 0)
        {
            EditorGUILayout.HelpBox(
                "Grid Bounds must be greater than zero",
                MessageType.Warning
            );
            return;
        }

        DrawLayerSelector();
        DrawGrid2D();
    }

    void DrawLayerSelector()
    {
        GUILayout.Label("Select Height (Y)", EditorStyles.boldLabel);

        selectedLayer = EditorGUILayout.IntSlider(
            "Layer",
            selectedLayer,
            0,
            config.gridBounds.y - 1
        );

        GUILayout.Space(5);
    }


    void DrawGrid2D()
    {
        GUILayout.Label("Click cells to enable / disable", EditorStyles.boldLabel);

        for (int z = config.gridBounds.z - 1; z >= 0; z--)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < config.gridBounds.x; x++)
            {
                Vector3Int cell = new Vector3Int(x, selectedLayer, z);
                bool active = config.occupiedCells.Contains(cell);

                GUI.backgroundColor = active ? Color.cyan : Color.gray;

                if (GUILayout.Button("", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    ToggleCell(cell);
                }
            }

            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }

    void ToggleCell(Vector3Int cell)
    {
        Undo.RecordObject(config, "Toggle Cell");

        if (config.occupiedCells.Contains(cell))
            config.occupiedCells.Remove(cell);
        else
            config.occupiedCells.Add(cell);

        EditorUtility.SetDirty(config);
    }
}
