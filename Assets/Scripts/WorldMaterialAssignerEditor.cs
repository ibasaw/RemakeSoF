using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldMaterialAssigner))]
public class WorldMaterialAssignerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldMaterialAssigner script = (WorldMaterialAssigner)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Smoothness = 0"))
        {
            script.ApplySmoothnessZero();
        }
    }
}
