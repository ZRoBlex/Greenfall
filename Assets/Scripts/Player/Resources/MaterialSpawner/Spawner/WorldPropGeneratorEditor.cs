#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

[CustomEditor(typeof(WorldPropGenerator))]
public class WorldPropGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldPropGenerator gen = (WorldPropGenerator)target;

        GUILayout.Space(10);
        GUILayout.Label("=== Generation Controls ===", EditorStyles.boldLabel);

        if (GUILayout.Button("🌍 Generate All"))
        {
            gen.GenerateAll();
        }

        if (GUILayout.Button("🌲 Generate Trees Only"))
        {
            gen.GenerateTreesOnly();
        }

        if (GUILayout.Button("🪨 Generate Rocks Only"))
        {
            gen.GenerateRocksOnly();
        }

        if (GUILayout.Button("🧹 Clear All"))
        {
            gen.Clear();
        }

        GUILayout.Space(10);
        GUILayout.Label("=== Seed Controls ===", EditorStyles.boldLabel);

        if (GUILayout.Button("🎲 Randomize Seed"))
        {
            gen.RandomizeSeed();
            EditorUtility.SetDirty(gen);
        }

        if (GUILayout.Button("🌍 Generate With Random Seed"))
        {
            gen.randomizeSeedOnGenerate = true;
            gen.GenerateAll();
            gen.randomizeSeedOnGenerate = false;

            EditorUtility.SetDirty(gen);
        }
    }
}
