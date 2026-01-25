#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BiomeMap))]
public class BiomeMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BiomeMap map = (BiomeMap)target;

        GUILayout.Space(10);
        GUILayout.Label("🌍 Biome", EditorStyles.boldLabel);

        if (GUILayout.Button("🎲 Randomize Biome Seed"))
        {
            Undo.RecordObject(map, "Randomize Biome Seed");
            map.seed = Random.Range(int.MinValue, int.MaxValue);
            map.RecalculateOffsets();   // 👈 CLAVE
            EditorUtility.SetDirty(map);
            SceneView.RepaintAll();     // 👈 fuerza refresh visual
        }


        if (GUILayout.Button("🔄 Sync Seed from WorldPropGenerator"))
        {
            WorldPropGenerator gen = FindAnyObjectByType<WorldPropGenerator>();
            if (gen != null)
            {
                Undo.RecordObject(map, "Sync Biome Seed");
                map.seed = gen.seed;
                map.RecalculateOffsets();   // 👈
                EditorUtility.SetDirty(map);
                SceneView.RepaintAll();     // 👈
            }
            else
            {
                Debug.LogWarning("No WorldPropGenerator found in scene.");
            }
        }


        if (GUILayout.Button("👁 Toggle Preview"))
        {
            Undo.RecordObject(map, "Toggle Preview");
            map.drawPreview = !map.drawPreview;
            EditorUtility.SetDirty(map);
        }
    }
}
#endif
