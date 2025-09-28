using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(SkeletonLoader))]
public class SkeletonLoaderEditor : Editor
{
    private SkeletonLoader skeletonLoader;
    private string[] skeletonFiles;
    private int selectedFileIndex = 0;
    
    void OnEnable()
    {
        skeletonLoader = (SkeletonLoader)target;
        RefreshSkeletonFiles();
    }
    
    void RefreshSkeletonFiles()
    {
        string skeletonsPath = "Assets/Data/skeletons/";
        if (Directory.Exists(skeletonsPath))
        {
            string[] files = Directory.GetFiles(skeletonsPath, "*.skl.json");
            skeletonFiles = new string[files.Length];
            
            for (int i = 0; i < files.Length; i++)
            {
                skeletonFiles[i] = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(files[i]));
            }
        }
        else
        {
            skeletonFiles = new string[0];
        }
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skeleton Loader Controls", EditorStyles.boldLabel);
        
        // Refresh button
        if (GUILayout.Button("Refresh Skeleton Files"))
        {
            RefreshSkeletonFiles();
        }
        
        EditorGUILayout.Space();
        
        // File selection dropdown
        if (skeletonFiles.Length > 0)
        {
            selectedFileIndex = EditorGUILayout.Popup("Select Skeleton File", selectedFileIndex, skeletonFiles);
            
            EditorGUILayout.Space();
            
            // Load button
            if (GUILayout.Button("Load Selected Skeleton"))
            {
                LoadSkeletonFile(skeletonFiles[selectedFileIndex]);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No skeleton files found in Assets/Data/skeletons/", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // Clear button
        if (GUILayout.Button("Clear All Boxes"))
        {
            skeletonLoader.ClearAllBoxes();
        }
        
        EditorGUILayout.Space();
        
        // Info section
        if (skeletonLoader != null)
        {
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
            
            // Count PCJ boxes recursively
            int pcjBoxCount = CountPCJBoxesRecursive(skeletonLoader.transform);
            EditorGUILayout.LabelField($"Created PCJ Boxes: {pcjBoxCount}");
            
            if (pcjBoxCount > 0)
            {
                EditorGUILayout.LabelField("PCJ Boxes:");
                List<string> pcjBoxNames = GetPCJBoxNamesRecursive(skeletonLoader.transform);
                foreach (string boxName in pcjBoxNames)
                {
                    EditorGUILayout.LabelField($"  - {boxName}");
                }
            }
        }
    }
    
    void LoadSkeletonFile(string fileName)
    {
        string fullPath = Path.Combine("Assets/Data/skeletons/", fileName + ".skl.json");
        
        if (File.Exists(fullPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                var skeletonData = JsonUtility.FromJson<SkeletonData>(jsonContent);
                
                if (skeletonData?.SkeletonInfo?.PCJ != null)
                {
                    skeletonLoader.ClearAllBoxes();
                    CreatePCJBoxes(skeletonData);
                    
                    Debug.Log($"Loaded skeleton '{fileName}' with {skeletonData.SkeletonInfo.PCJ.Length} PCJ entries");
                }
                else
                {
                    Debug.LogError("Invalid skeleton data structure");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading skeleton file: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"Skeleton file not found: {fullPath}");
        }
    }
    
    void CreatePCJBoxes(SkeletonData skeletonData)
    {
        // Find all bone transforms in the scene
        var boneTransforms = new System.Collections.Generic.Dictionary<string, Transform>();
        Transform[] allTransforms = skeletonLoader.GetComponentsInChildren<Transform>();
        
        foreach (Transform t in allTransforms)
        {
            boneTransforms[t.name.ToLower()] = t;
        }
        
        // Create boxes for each PCJ entry
        foreach (PCJEntry pcj in skeletonData.SkeletonInfo.PCJ)
        {
            CreateBoxForPCJ(pcj, boneTransforms);
        }
        
        // Set up parent-child relationships
        SetupPCJHierarchy(skeletonData);
    }
    
    void CreateBoxForPCJ(PCJEntry pcj, System.Collections.Generic.Dictionary<string, Transform> boneTransforms)
    {
        // Find the target bone for this PCJ
        Transform targetBone = FindTargetBoneForPCJ(pcj, boneTransforms);
        if (targetBone == null)
        {
            Debug.LogWarning($"No target bone found for PCJ {pcj.Name}, skipping box creation");
            return;
        }
        
        // Create PCJ box as child of the target bone
        GameObject box = new GameObject($"PCJ_{pcj.Name}");
        box.transform.SetParent(targetBone);
        box.transform.localPosition = Vector3.zero;
        box.transform.localRotation = Quaternion.identity;
        
        // Add BoxCollider
        BoxCollider boxCollider = box.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        
        // Calculate box size from Maxs and Mins (convert from meters to centimeters)
        Vector3 size = new Vector3(
            (pcj.Maxs[0] - pcj.Mins[0]) / 100f,
            (pcj.Maxs[1] - pcj.Mins[1]) / 100f,
            (pcj.Maxs[2] - pcj.Mins[2]) / 100f
        );
        
        Vector3 center = new Vector3(
            (pcj.Maxs[0] + pcj.Mins[0]) / 200f,
            (pcj.Maxs[1] + pcj.Mins[1]) / 200f,
            (pcj.Maxs[2] + pcj.Mins[2]) / 200f
        );
        
        boxCollider.size = size;
        boxCollider.center = center;
    }
    
    Transform FindTargetBoneForPCJ(PCJEntry pcj, System.Collections.Generic.Dictionary<string, Transform> boneTransforms)
    {
        // PCJ must always be attached to BoneAim - this is the main bone for hitbox positioning
        if (pcj.BoneAim != null && pcj.BoneAim.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneAim[0].Name, boneTransforms);
            if (bone != null) return bone;
        }
        
        // Fallback: if no BoneAim, try other bones (should not happen in proper data)
        if (pcj.BoneRelative != null && pcj.BoneRelative.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneRelative[0].Name, boneTransforms);
            if (bone != null) return bone;
        }
        
        if (pcj.BoneSmooth != null && pcj.BoneSmooth.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneSmooth[0].Name, boneTransforms);
            if (bone != null) return bone;
        }
        
        if (pcj.BoneExtra != null && pcj.BoneExtra.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneExtra[0].Name, boneTransforms);
            if (bone != null) return bone;
        }
        
        return null;
    }
    
    BoneInfo FindSkelementForPCJ(string pcjName)
    {
        if (skeletonLoader?.currentSkeletonData?.SkeletonInfo?.Skelement == null) return null;
        
        // Try to find exact match first
        foreach (BoneInfo skelement in skeletonLoader.currentSkeletonData.SkeletonInfo.Skelement)
        {
            if (skelement.Name.Equals(pcjName, System.StringComparison.OrdinalIgnoreCase))
            {
                return skelement;
            }
        }
        
        // Try partial match
        foreach (BoneInfo skelement in skeletonLoader.currentSkeletonData.SkeletonInfo.Skelement)
        {
            if (skelement.Name.ToLower().Contains(pcjName.ToLower()) || pcjName.ToLower().Contains(skelement.Name.ToLower()))
            {
                Debug.Log($"Found Skelement '{pcjName}' as partial match: '{skelement.Name}'");
                return skelement;
            }
        }
        
        return null;
    }
    
    Transform FindTargetBone(PCJEntry pcj, System.Collections.Generic.Dictionary<string, Transform> boneTransforms)
    {
        // Try to find the bone from BoneAim first, then BoneRelative, then BoneSmooth
        if (pcj.BoneAim != null && pcj.BoneAim.Length > 0)
        {
            return FindBoneByName(pcj.BoneAim[0].Name, boneTransforms);
        }
        
        if (pcj.BoneRelative != null && pcj.BoneRelative.Length > 0)
        {
            return FindBoneByName(pcj.BoneRelative[0].Name, boneTransforms);
        }
        
        if (pcj.BoneSmooth != null && pcj.BoneSmooth.Length > 0)
        {
            return FindBoneByName(pcj.BoneSmooth[0].Name, boneTransforms);
        }
        
        return null;
    }
    
    Transform FindBoneByName(string boneName, System.Collections.Generic.Dictionary<string, Transform> boneTransforms)
    {
        if (string.IsNullOrEmpty(boneName)) return null;
        
        // Try exact match first
        if (boneTransforms.ContainsKey(boneName.ToLower()))
        {
            return boneTransforms[boneName.ToLower()];
        }
        
        // Try partial match
        foreach (var kvp in boneTransforms)
        {
            if (kvp.Key.Contains(boneName.ToLower()) || boneName.ToLower().Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        
        return null;
    }
    
    
    void SetupPCJHierarchy(SkeletonData skeletonData)
    {
        var pcjBoxes = new System.Collections.Generic.Dictionary<string, GameObject>();
        
        // First, collect all PCJ boxes
        for (int i = 0; i < skeletonLoader.transform.childCount; i++)
        {
            Transform child = skeletonLoader.transform.GetChild(i);
            if (child.name.StartsWith("PCJ_"))
            {
                string pcjName = child.name.Substring(4); // Remove "PCJ_" prefix
                pcjBoxes[pcjName] = child.gameObject;
            }
        }
        
        // Set up parent-child relationships
        foreach (PCJEntry pcj in skeletonData.SkeletonInfo.PCJ)
        {
            if (!string.IsNullOrEmpty(pcj.Parent) && pcjBoxes.ContainsKey(pcj.Name) && pcjBoxes.ContainsKey(pcj.Parent))
            {
                pcjBoxes[pcj.Name].transform.SetParent(pcjBoxes[pcj.Parent].transform);
            }
        }
    }
    
    int CountPCJBoxesRecursive(Transform parent)
    {
        int count = 0;
        
        // Check all children of current transform
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            
            // If this is a PCJ box, count it
            if (child.name.StartsWith("PCJ_"))
            {
                count++;
            }
            
            // Recursively count children
            count += CountPCJBoxesRecursive(child);
        }
        
        return count;
    }
    
    List<string> GetPCJBoxNamesRecursive(Transform parent)
    {
        List<string> boxNames = new List<string>();
        
        // Check all children of current transform
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            
            // If this is a PCJ box, add its name
            if (child.name.StartsWith("PCJ_"))
            {
                boxNames.Add(child.name);
            }
            
            // Recursively get children
            boxNames.AddRange(GetPCJBoxNamesRecursive(child));
        }
        
        return boxNames;
    }
}
