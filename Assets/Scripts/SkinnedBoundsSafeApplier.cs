// Assets/Editor/SkinnedBoundsSafeApplier.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SkinnedBoundsSafeApplier : EditorWindow
{
    float margin = 0.10f; // 10% safety margin
    bool applyToChildren = true;
    bool writeToPrefab = false;

    [MenuItem("Tools/SkinnedMesh/Safe Bounds Applier")]
    public static void OpenWindow() => GetWindow<SkinnedBoundsSafeApplier>("Safe Bounds Applier");

    void OnGUI()
    {
        GUILayout.Label("Safer SkinnedMeshRenderer Bounds Applier", EditorStyles.boldLabel);
        margin = EditorGUILayout.Slider("Extra margin (rel)", margin, 0f, 1f);
        applyToChildren = EditorGUILayout.Toggle("Apply to children", applyToChildren);
        writeToPrefab = EditorGUILayout.Toggle("Write to prefab asset (best effort)", writeToPrefab);

        if (GUILayout.Button("Scan selection"))
        {
            ScanSelection();
        }
        EditorGUILayout.HelpBox("Scan zeigt baked vs local bounds. 'Apply' schreibt nur wenn du bestätigst.", MessageType.Info);
    }

    void ScanSelection()
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", "Bitte mindestens ein GameObject auswählen.", "OK");
            return;
        }

        var entries = new List<(GameObject go, SkinnedMeshRenderer smr, Bounds baked, Bounds local, Bounds suggested)>();

        foreach (var go in selected)
        {
            SkinnedMeshRenderer[] smrs = applyToChildren ? go.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[] { go.GetComponent<SkinnedMeshRenderer>() };
            foreach (var smr in smrs)
            {
                if (smr == null) continue;
                var baked = BakeSafe(smr);
                var local = smr.localBounds;
                var suggested = baked;
                // expand by margin
                Vector3 expand = suggested.size * margin;
                suggested.Expand(expand);
                entries.Add((go, smr, baked, local, suggested));
            }
        }

        // Present a confirmation window with details
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Found {entries.Count} SkinnedMeshRenderer(s):");
        int i = 0;
        foreach (var e in entries)
        {
            i++;
            sb.AppendLine($"{i}) GO='{e.go.name}' SMR='{e.smr.name}'");
            sb.AppendLine($"   local.size={e.local.size}, baked.size={e.baked.size}");
            sb.AppendLine($"   suggested.size={e.suggested.size}");
            sb.AppendLine("");
        }

        bool confirm = EditorUtility.DisplayDialogComplex("Scan Ergebnisse", sb.ToString(), "Apply all suggested", "Cancel", "Show per-item dialog") == 0;
        if (confirm)
        {
            int changed = 0;
            foreach (var e in entries)
            {
                Undo.RecordObject(e.smr, "Set localBounds");
                e.smr.localBounds = e.suggested;
                EditorUtility.SetDirty(e.smr);
                changed++;
                if (writeToPrefab) TryWriteToPrefab(e.smr, e.suggested);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Done", $"Applied suggested bounds to {changed} SMR(s).", "OK");
        }
        else
        {
            // optional per-item dialog
            if (EditorUtility.DisplayDialog("Per-item", "Would you like to review each SMR and apply selectively?", "Yes", "No"))
            {
                foreach (var e in entries)
                {
                    bool apply = EditorUtility.DisplayDialog("Apply bounds?",
                        $"GO='{e.go.name}' SMR='{e.smr.name}'\nlocal.size={e.local.size}\nbaked.size={e.baked.size}\nsuggested.size={e.suggested.size}\n\nApply suggested bounds to this SMR?",
                        "Apply", "Skip");
                    if (apply)
                    {
                        Undo.RecordObject(e.smr, "Set localBounds");
                        e.smr.localBounds = e.suggested;
                        EditorUtility.SetDirty(e.smr);
                        if (writeToPrefab) TryWriteToPrefab(e.smr, e.suggested);
                    }
                }
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Done", "Selective apply finished.", "OK");
            }
        }
    }

    static Bounds BakeSafe(SkinnedMeshRenderer smr)
    {
        var m = new Mesh();
        var b = new Bounds();
        try
        {
            smr.BakeMesh(m);
            b = m.bounds;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"BakeMesh failed for {smr.name}: {ex.Message}");
        }
        finally
        {
            if (m != null) Object.DestroyImmediate(m);
        }
        // if baked is zero (weird), fallback to a small box
        if (b.size == Vector3.zero) b = new Bounds(Vector3.zero, Vector3.one * 0.1f);
        return b;
    }

    static void TryWriteToPrefab(SkinnedMeshRenderer smrInstance, Bounds newBounds)
    {
        var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(smrInstance.gameObject);
        if (prefabRoot == null) return;
        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
        if (string.IsNullOrEmpty(prefabPath)) return;

        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        var assetSmrs = prefabContents.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        bool modified = false;
        foreach (var aSmr in assetSmrs)
        {
            if (aSmr.sharedMesh == smrInstance.sharedMesh || aSmr.name == smrInstance.name)
            {
                var so = new SerializedObject(aSmr);
                so.FindProperty("m_LocalAABB.m_Center").vector3Value = newBounds.center;
                so.FindProperty("m_LocalAABB.m_Extent").vector3Value = newBounds.extents;
                so.ApplyModifiedProperties();
                modified = true;
            }
        }
        if (modified) PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);
    }
}
