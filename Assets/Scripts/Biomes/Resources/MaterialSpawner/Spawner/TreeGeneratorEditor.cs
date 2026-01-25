#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

[CustomEditor(typeof(TreeGenerator))]
public class TreeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TreeGenerator gen = (TreeGenerator)target;

        if (GUILayout.Button("🌲 Generate Trees"))
        {
            gen.Generate();
        }

        if (GUILayout.Button("🧹 Clear Trees"))
        {
            gen.Clear();
        }
    }
}
