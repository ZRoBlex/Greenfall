#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

[CustomEditor(typeof(FloorLootSpawner))]
public class FloorLootSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FloorLootSpawner gen = (FloorLootSpawner)target;

        GUILayout.Space(10);
        GUILayout.Label("=== Floor Loot Controls ===", EditorStyles.boldLabel);

        if (GUILayout.Button("🔫 Generate Floor Loot"))
        {
            gen.Generate();
        }

        if (GUILayout.Button("🧹 Clear Floor Loot"))
        {
            gen.Clear();
        }

        GUILayout.Space(10);
        GUILayout.Label("=== Seed Controls ===", EditorStyles.boldLabel);

        if (GUILayout.Button("🎲 Randomize Seed"))
        {
            gen.seed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log($"FloorLootSpawner: New random seed = {gen.seed}");
            EditorUtility.SetDirty(gen);
        }

        if (GUILayout.Button("🔫 Generate With Random Seed"))
        {
            gen.randomizeSeedOnGenerate = true;
            gen.Generate();
            gen.randomizeSeedOnGenerate = false;

            EditorUtility.SetDirty(gen);
        }
    }
}
