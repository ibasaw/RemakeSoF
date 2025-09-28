// Assets/Editor/SetUpdateWhenOffscreenEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetUpdateWhenOffscreenEditor : EditorWindow
{
    bool setTo = true;
    bool applyToChildren = true;
    bool applyToPrefabs = true;
    bool includeInactive = true;

    [MenuItem("Tools/SkinnedMesh/Set Update When Offscreen")]
    static void OpenWindow() => GetWindow<SetUpdateWhenOffscreenEditor>("UpdateWhenOffscreen");

    void OnGUI()
    {
        GUILayout.Label("Set SkinnedMeshRenderer.updateWhenOffscreen", EditorStyles.boldLabel);
        setTo = EditorGUILayout.Toggle("Set value", setTo);
        applyToChildren = EditorGUILayout.Toggle("Apply to children", applyToChildren);
        includeInactive = EditorGUILayout.Toggle("Include inactive objects", includeInactive);
        applyToPrefabs = EditorGUILayout.Toggle("Write to prefab assets", applyToPrefabs);

        if (GUILayout.Button("Apply to selection"))
        {
            ApplyToSelection();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Wird die Prefab-Option aktiviert, versucht das Tool, die Prefab-Asset-Datei zu öffnen und die Einstellung dort zu speichern.", MessageType.Info);
    }

    void ApplyToSelection()
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", "Bitte mindestens ein GameObject auswählen.", "OK");
            return;
        }

        int changed = 0;
        foreach (var root in selected)
        {
            var smrs = new List<SkinnedMeshRenderer>();
            if (applyToChildren)
                smrs.AddRange(root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive));
            else
            {
                var r = root.GetComponent<SkinnedMeshRenderer>();
                if (r != null) smrs.Add(r);
            }

            foreach (var smr in smrs)
            {
                if (smr == null) continue;
                Undo.RecordObject(smr, "Set updateWhenOffscreen");
                smr.updateWhenOffscreen = setTo;
                EditorUtility.SetDirty(smr);
                changed++;

                if (applyToPrefabs)
                    TryWriteToPrefabAsset(smr);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Fertig", $"Geändert: {changed} SkinnedMeshRenderer(s).", "OK");
    }

    void TryWriteToPrefabAsset(SkinnedMeshRenderer smrInstance)
    {
        var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(smrInstance.gameObject);
        if (prefabRoot == null) return;

        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
        if (string.IsNullOrEmpty(prefabPath)) return;

        // Lade Prefab-Inhalt, passe die Komponenten an und speichere das Asset
        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        bool modified = false;
        var assetSmrs = prefabContents.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var aSmr in assetSmrs)
        {
            // match by sharedMesh OR by name as fallback
            if (aSmr.sharedMesh == smrInstance.sharedMesh || aSmr.name == smrInstance.name)
            {
                aSmr.updateWhenOffscreen = setTo;
                modified = true;
            }
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(prefabContents);
    }
}
