// Place this file in Assets/Editor/FBXMapGeneratorAdvanced.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FBXMapGeneratorAdvanced : EditorWindow
{
    GameObject fbxSource = null;
    enum PrototypeMode { WholeObjects, MeshOnly }
    PrototypeMode mode = PrototypeMode.WholeObjects;

    int platformCount = 25;
    float startY = 0f;
    float minVerticalStep = 1.0f;
    float maxVerticalStep = 2.5f;
    float horizontalRange = 3.0f;
    float zJitter = 1.0f;

    bool randomRotation = true;
    float minRotation = -30f;
    float maxRotation = 30f;

    bool useSeed = false;
    int randomSeed = 0;

    bool clearPrevious = true;
    string generatedRootName = "generated_map";

    // overlap avoidance
    int attemptsPerPlatform = 8;
    float overlapPadding = 0.05f; // small extra spacing

    [MenuItem("Tools/FBX Map Generator Advanced")]
    static void ShowWindow() => GetWindow<FBXMapGeneratorAdvanced>("FBX Map Generator Advanced");

    void OnGUI()
    {
        GUILayout.Label("FBX → Randomized Procedural Jump-Map", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        fbxSource = (GameObject)EditorGUILayout.ObjectField("FBX / Prefab Root", fbxSource, typeof(GameObject), false);
        mode = (PrototypeMode)EditorGUILayout.EnumPopup("Prototyp-Modus", mode);

        EditorGUILayout.Space();
        platformCount = EditorGUILayout.IntField("Anzahl Plattformen", Mathf.Max(1, platformCount));
        startY = EditorGUILayout.FloatField("Start Y", startY);

        EditorGUILayout.MinMaxSlider(new GUIContent("Vertikaler Schritt (min - max)"), ref minVerticalStep, ref maxVerticalStep, 0.1f, 10f);
        if (minVerticalStep > maxVerticalStep) { var t = minVerticalStep; minVerticalStep = maxVerticalStep; maxVerticalStep = t; }
        EditorGUILayout.LabelField($"Schritt min: {minVerticalStep:F2}  max: {maxVerticalStep:F2}");

        horizontalRange = EditorGUILayout.FloatField("Horizontal Range (X)", horizontalRange);
        zJitter = EditorGUILayout.FloatField("Z Jitter", zJitter);

        randomRotation = EditorGUILayout.Toggle("Zufällige Rotation", randomRotation);
        if (randomRotation)
        {
            EditorGUILayout.MinMaxSlider(new GUIContent("Rotation Range (°)"), ref minRotation, ref maxRotation, -180f, 180f);
            EditorGUILayout.LabelField($"Rotation min: {minRotation:F0}°  max: {maxRotation:F0}°");
        }

        EditorGUILayout.Space();
        useSeed = EditorGUILayout.Toggle("Seed verwenden (reproduzierbar)", useSeed);
        if (useSeed) randomSeed = EditorGUILayout.IntField("Seed", randomSeed);

        EditorGUILayout.Space();
        attemptsPerPlatform = EditorGUILayout.IntField("Versuche pro Plattform (Overlap vermeiden)", Mathf.Max(1, attemptsPerPlatform));
        overlapPadding = EditorGUILayout.FloatField("Overlap Padding (m)", Mathf.Max(0f, overlapPadding));

        EditorGUILayout.Space();
        generatedRootName = EditorGUILayout.TextField("Generated root name", generatedRootName);
        clearPrevious = EditorGUILayout.Toggle("Vorherige 'generated_map' löschen", clearPrevious);

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(fbxSource == null);
        if (GUILayout.Button("Generate Map"))
        {
            GenerateMap();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clear generated_map GameObjects in Scene"))
        {
            ClearGeneratedMapsInScene();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Modus 'MeshOnly' verwendet nur MeshFilter+MeshRenderer der Children (nützlich, wenn dein FBX viele einzelne Meshes enthält).", MessageType.Info);
    }

    void ClearGeneratedMapsInScene()
    {
        var all = GameObject.FindObjectsOfType<GameObject>();
        int removed = 0;
        foreach (var go in all)
        {
            if (go.name == generatedRootName)
            {
                Undo.DestroyObjectImmediate(go);
                removed++;
            }
        }
        EditorUtility.DisplayDialog("Clear generated_map", $"Entfernt: {removed} GameObject(s) namens '{generatedRootName}'.", "OK");
    }

    void GenerateMap()
    {
        if (fbxSource == null)
        {
            EditorUtility.DisplayDialog("Fehler", "Kein FBX/Prefab angegeben.", "OK");
            return;
        }

        if (useSeed) Random.InitState(randomSeed);

        // Sammle Prototypen je nach Modus
        var prototypesWhole = new List<GameObject>();
        var prototypesMesh = new List<MeshPrototype>();

        var transforms = fbxSource.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t == fbxSource.transform) continue;

            var mr = t.GetComponent<MeshRenderer>();
            var mf = t.GetComponent<MeshFilter>();
            if (mr != null && mf != null)
            {
                prototypesWhole.Add(t.gameObject);
                prototypesMesh.Add(new MeshPrototype { mesh = mf.sharedMesh, materials = mr.sharedMaterials, sourceTransform = t });
            }
            else if (mr != null) // renderer but no filter? still add as whole
            {
                prototypesWhole.Add(t.gameObject);
            }
        }

        if (mode == PrototypeMode.WholeObjects && prototypesWhole.Count == 0)
        {
            EditorUtility.DisplayDialog("Keine Prototypen", "Keine Child-GameObjects mit MeshRenderer im FBX gefunden.", "OK");
            return;
        }
        if (mode == PrototypeMode.MeshOnly && prototypesMesh.Count == 0)
        {
            EditorUtility.DisplayDialog("Keine Mesh-Prototypen", "Keine MeshFilter/MeshRenderer Kombinationen im FBX gefunden.", "OK");
            return;
        }

        if (clearPrevious) ClearGeneratedMapsInScene();

        GameObject root = new GameObject(generatedRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create generated_map root");

        float currentY = startY;
        List<Bounds> placedBounds = new List<Bounds>();

        for (int i = 0; i < platformCount; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < attemptsPerPlatform && !placed; attempt++)
            {
                // choose prototype randomly
                if (mode == PrototypeMode.WholeObjects)
                {
                    var proto = prototypesWhole[Random.Range(0, prototypesWhole.Count)];
                    // instantiate
                    GameObject instance = InstantiatePrototypeObject(proto);
                    if (instance == null) break;
                    Undo.RegisterCreatedObjectUndo(instance, "Instantiate platform");
                    instance.transform.SetParent(root.transform, true);

                    // pick candidate pos
                    float x = Random.Range(-horizontalRange, horizontalRange);
                    float z = Random.Range(-zJitter, zJitter);
                    float verticalStep = Random.Range(minVerticalStep, maxVerticalStep);
                    float candidateY = currentY + verticalStep;

                    instance.transform.position = new Vector3(x, candidateY, z);

                    if (randomRotation)
                    {
                        float yRot = Random.Range(minRotation, maxRotation);
                        instance.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
                    }

                    // compute instance bounds (world)
                    var mr = instance.GetComponentInChildren<MeshRenderer>();
                    Bounds b;
                    if (mr != null) b = mr.bounds;
                    else b = new Bounds(instance.transform.position, Vector3.one * 1.0f);

                    // inflate padding
                    b.Expand(overlapPadding);

                    if (!IntersectsAny(b, placedBounds))
                    {
                        placedBounds.Add(b);
                        currentY = candidateY; // only advance height when successfully placed
                        placed = true;

                        instance.name = $"platform_{i:D2}_{proto.name}";
                        // ensure collider exists
                        EnsureCollider(instance);
                    }
                    else
                    {
                        // failed -> destroy and retry
                        Undo.DestroyObjectImmediate(instance);
                    }
                }
                else // MeshOnly
                {
                    var protoMesh = prototypesMesh[Random.Range(0, prototypesMesh.Count)];
                    GameObject instance = CreateMeshOnlyInstance(protoMesh);
                    Undo.RegisterCreatedObjectUndo(instance, "Create mesh-only platform");
                    instance.transform.SetParent(root.transform, true);

                    float x = Random.Range(-horizontalRange, horizontalRange);
                    float z = Random.Range(-zJitter, zJitter);
                    float verticalStep = Random.Range(minVerticalStep, maxVerticalStep);
                    float candidateY = currentY + verticalStep;
                    instance.transform.position = new Vector3(x, candidateY, z);

                    if (randomRotation)
                    {
                        float yRot = Random.Range(minRotation, maxRotation);
                        instance.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
                    }

                    // compute bounds from mesh bounds scaled by lossyScale
                    var mf = instance.GetComponent<MeshFilter>();
                    Bounds localMeshBounds = mf.sharedMesh.bounds;
                    Vector3 scaledSize = Vector3.Scale(localMeshBounds.size, instance.transform.lossyScale);
                    Vector3 worldCenter = instance.transform.TransformPoint(localMeshBounds.center);
                    Bounds b = new Bounds(worldCenter, scaledSize);
                    b.Expand(overlapPadding);

                    if (!IntersectsAny(b, placedBounds))
                    {
                        placedBounds.Add(b);
                        currentY = candidateY;
                        placed = true;
                        instance.name = $"platform_{i:D2}_mesh";
                        EnsureCollider(instance);
                    }
                    else
                    {
                        Undo.DestroyObjectImmediate(instance);
                    }
                }
            } // attempts
            if (!placed)
            {
                Debug.LogWarning($"Plattform {i} konnte nicht ohne Überlappung platziert werden (Versuche: {attemptsPerPlatform}). Überspringe.");
            }
        } // for platforms

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    GameObject InstantiatePrototypeObject(GameObject proto)
    {
        GameObject instance = null;
        #if UNITY_2018_3_OR_NEWER
        if (PrefabUtility.IsPartOfPrefabAsset(proto))
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(proto, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (instance == null) instance = (GameObject)PrefabUtility.InstantiatePrefab(proto);
        }
        else
        #endif
        {
            instance = (GameObject)Instantiate(proto);
            instance.name = proto.name;
        }
        if (instance != null) instance.SetActive(true);
        return instance;
    }

    GameObject CreateMeshOnlyInstance(MeshPrototype proto)
    {
        GameObject go = new GameObject("mesh_proto");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = proto.mesh;
        mr.sharedMaterials = proto.materials != null ? proto.materials : new Material[0];

        // match approximate scale/rotation of source transform to preserve proportions
        go.transform.localScale = proto.sourceTransform.lossyScale;
        go.transform.rotation = proto.sourceTransform.rotation;
        go.SetActive(true);
        return go;
    }

    bool IntersectsAny(Bounds b, List<Bounds> list)
    {
        foreach (var pb in list)
            if (pb.Intersects(b)) return true;
        return false;
    }

    void EnsureCollider(GameObject instance)
    {
        var anyCol = instance.GetComponentInChildren<Collider>();
        if (anyCol == null)
        {
            // try to add box collider based on renderer bounds
            var mr = instance.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                // create a BoxCollider on root and set center/size relative to root
                var bc = instance.AddComponent<BoxCollider>();
                var rootT = instance.transform;
                var worldCenter = mr.bounds.center;
                var size = mr.bounds.size;

                // transform center to local
                bc.center = rootT.InverseTransformPoint(worldCenter);
                // convert to local size by dividing by lossyScale
                Vector3 localSize = new Vector3(
                    SafeDivide(size.x, rootT.lossyScale.x),
                    SafeDivide(size.y, rootT.lossyScale.y),
                    SafeDivide(size.z, rootT.lossyScale.z)
                );
                bc.size = localSize;
            }
            else
            {
                // fallback small collider
                var bc = instance.AddComponent<BoxCollider>();
                bc.size = Vector3.one * 1f;
            }
        }
    }

    float SafeDivide(float a, float b) => Mathf.Approximately(b, 0f) ? a : a / b;

    class MeshPrototype
    {
        public Mesh mesh;
        public Material[] materials;
        public Transform sourceTransform;
    }
}
