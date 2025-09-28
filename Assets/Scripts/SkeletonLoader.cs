using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class BoneInfo
{
    public string Name;
    public string Bone;
    public string Bone1;
    public string Bone2;
    public string Parent;
}

[System.Serializable]
public class BoneOrientation
{
    public string Name;
    public string Fwd;
    public string Up;
    public string Right;
}

[System.Serializable]
public class PCJEntry
{
    public string Name;
    public string Parent;
    public float[] Maxs;
    public float[] Mins;
    public BoneOrientation[] BoneAim;
    public BoneOrientation[] BoneRelative;
    public BoneOrientation[] BoneSmooth;
    public BoneOrientation[] BoneExtra;
}

[System.Serializable]
public class SkeletonInfo
{
    public string GLMFile;
    public string FramesFile;
    public int MaxSpeed;
    public string HeaderAnim;
    public string TrailerAnim;
    public BoneInfo[] Skelement;
    public PCJEntry[] PCJ;
}

[System.Serializable]
public class SkeletonData
{
    public SkeletonInfo SkeletonInfo;
}

public class SkeletonLoader : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown skeletonDropdown;
    public Button loadButton;
    public Button clearButton;
    
    [Header("Box Settings")]
    public bool isTrigger = true;
    
    [Header("Auto Load")]
    public bool autoLoadOnStart = true;
    public int autoLoadIndex = 0; // Index of skeleton file to auto-load
    
    [Header("Debug")]
    public bool debugMode = true;
    
    public SkeletonData currentSkeletonData;
    private List<GameObject> createdBoxes = new List<GameObject>();
    private Dictionary<string, Transform> boneTransforms = new Dictionary<string, Transform>();
    private Dictionary<string, GameObject> pcjBoxes = new Dictionary<string, GameObject>();
    private string[] skeletonFiles;
    
    private string skeletonsPath = "Assets/Data/skeletons/";
    
    void Start()
    {
        SetupUI();
        LoadSkeletonFileList();
        
        // Automatically load skeleton on start if enabled
        if (autoLoadOnStart && skeletonFiles != null && skeletonFiles.Length > 0)
        {
            int loadIndex = Mathf.Clamp(autoLoadIndex, 0, skeletonFiles.Length - 1);
            LoadSkeletonFile(skeletonFiles[loadIndex]);
            
            if (debugMode)
            {
                Debug.Log($"Auto-loaded skeleton: {skeletonFiles[loadIndex]}");
            }
        }
    }
    
    void SetupUI()
    {
        if (skeletonDropdown == null)
        {
            skeletonDropdown = FindObjectOfType<TMP_Dropdown>();
        }
        
        if (loadButton == null)
        {
            loadButton = GetComponentInChildren<Button>();
        }
        
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSelectedSkeleton);
        }
        
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearAllBoxes);
        }
    }
    
    void LoadSkeletonFileList()
    {
        // Find all .skl.json files in the skeletons directory
        string[] files = Directory.GetFiles(skeletonsPath, "*.skl.json");
        List<string> fileNames = new List<string>();
        
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
            fileNames.Add(fileName);
        }
        
        skeletonFiles = fileNames.ToArray();
        
        if (skeletonDropdown != null)
        {
            skeletonDropdown.ClearOptions();
            skeletonDropdown.AddOptions(fileNames);
        }
        
        if (debugMode)
        {
            Debug.Log($"Found {fileNames.Count} skeleton files: {string.Join(", ", fileNames)}");
        }
    }
    
    public void LoadSkeletonFile(string fileName)
    {
        string fullPath = Path.Combine(skeletonsPath, fileName + ".skl.json");
        
        if (File.Exists(fullPath))
        {
            LoadSkeletonFromFile(fullPath);
        }
        else
        {
            Debug.LogError($"Skeleton file not found: {fullPath}");
        }
    }
    
    public void LoadSelectedSkeleton()
    {
        if (skeletonDropdown == null || skeletonDropdown.value < 0) return;
        
        string selectedFile = skeletonDropdown.options[skeletonDropdown.value].text;
        LoadSkeletonFile(selectedFile);
    }
    
    void LoadSkeletonFromFile(string filePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            currentSkeletonData = JsonUtility.FromJson<SkeletonData>(jsonContent);
            
            if (currentSkeletonData?.SkeletonInfo?.PCJ != null)
            {
                ClearAllBoxes();
                CreatePCJBoxes();
                
                if (debugMode)
                {
                    Debug.Log($"Loaded skeleton with {currentSkeletonData.SkeletonInfo.PCJ.Length} PCJ entries");
                }
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
    
    void CreatePCJBoxes()
    {
        if (currentSkeletonData?.SkeletonInfo?.PCJ == null) return;
        
        // First, find all bone transforms in the scene
        FindBoneTransforms();
        
        // Create boxes for each PCJ entry
        foreach (PCJEntry pcj in currentSkeletonData.SkeletonInfo.PCJ)
        {
            CreateBoxForPCJ(pcj);
        }
        
        // Note: PCJ boxes are now always created as children of their respective bones
        // No additional hierarchy setup needed as boxes will move with their bones
    }
    
    void FindBoneTransforms()
    {
        boneTransforms.Clear();
        
        // Find all transforms in the current object and all its children recursively
        FindBoneTransformsRecursive(transform);
        
        if (debugMode)
        {
            Debug.Log($"Found {boneTransforms.Count} bone transforms in hierarchy");
            foreach (var kvp in boneTransforms)
            {
                Debug.Log($"  - {kvp.Key} -> {kvp.Value.name}");
            }
        }
    }
    
    void FindBoneTransformsRecursive(Transform parent)
    {
        // Add current transform
        boneTransforms[parent.name.ToLower()] = parent;
        
        // Recursively search all children
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            FindBoneTransformsRecursive(child);
        }
    }
    
    void CreateBoxForPCJ(PCJEntry pcj)
    {
        // Find the target bone for this PCJ (use BoneAim first, then BoneRelative, then BoneSmooth)
        Transform targetBone = FindTargetBoneForPCJ(pcj);
        if (targetBone == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"No target bone found for PCJ {pcj.Name}, skipping box creation");
            }
            return;
        }
        
        // Create PCJ box as child of the target bone
        GameObject box = new GameObject($"PCJ_{pcj.Name}");
        box.transform.SetParent(targetBone);
        box.transform.localPosition = Vector3.zero;
        box.transform.localRotation = Quaternion.identity;
        
        // Add BoxCollider
        BoxCollider boxCollider = box.AddComponent<BoxCollider>();
        boxCollider.isTrigger = isTrigger;
        
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
        
        pcjBoxes[pcj.Name] = box;
        createdBoxes.Add(box);
        
        if (debugMode)
        {
            Debug.Log($"Created PCJ box for {pcj.Name} with size {size} at bone {targetBone.name}");
        }
    }
    
    
    Transform FindTargetBoneForPCJ(PCJEntry pcj)
    {
        // PCJ must always be attached to BoneAim - this is the main bone for hitbox positioning
        if (pcj.BoneAim != null && pcj.BoneAim.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneAim[0].Name);
            if (bone != null) return bone;
        }
        
        // Fallback: if no BoneAim, try other bones (should not happen in proper data)
        if (pcj.BoneRelative != null && pcj.BoneRelative.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneRelative[0].Name);
            if (bone != null) return bone;
        }
        
        if (pcj.BoneSmooth != null && pcj.BoneSmooth.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneSmooth[0].Name);
            if (bone != null) return bone;
        }
        
        if (pcj.BoneExtra != null && pcj.BoneExtra.Length > 0)
        {
            Transform bone = FindBoneByName(pcj.BoneExtra[0].Name);
            if (bone != null) return bone;
        }
        
        return null;
    }
    
    BoneInfo FindSkelementForPCJ(string pcjName)
    {
        if (currentSkeletonData?.SkeletonInfo?.Skelement == null) return null;
        
        // Try to find exact match first
        foreach (BoneInfo skelement in currentSkeletonData.SkeletonInfo.Skelement)
        {
            if (skelement.Name.Equals(pcjName, System.StringComparison.OrdinalIgnoreCase))
            {
                return skelement;
            }
        }
        
        // Try partial match
        foreach (BoneInfo skelement in currentSkeletonData.SkeletonInfo.Skelement)
        {
            if (skelement.Name.ToLower().Contains(pcjName.ToLower()) || pcjName.ToLower().Contains(skelement.Name.ToLower()))
            {
                if (debugMode)
                {
                    Debug.Log($"Found Skelement '{pcjName}' as partial match: '{skelement.Name}'");
                }
                return skelement;
            }
        }
        
        return null;
    }
    
    Transform FindTargetBone(PCJEntry pcj)
    {
        // Try to find the bone from BoneAim first, then BoneRelative, then BoneSmooth
        if (pcj.BoneAim != null && pcj.BoneAim.Length > 0)
        {
            return FindBoneByName(pcj.BoneAim[0].Name);
        }
        
        if (pcj.BoneRelative != null && pcj.BoneRelative.Length > 0)
        {
            return FindBoneByName(pcj.BoneRelative[0].Name);
        }
        
        if (pcj.BoneSmooth != null && pcj.BoneSmooth.Length > 0)
        {
            return FindBoneByName(pcj.BoneSmooth[0].Name);
        }
        
        return null;
    }
    
    Transform FindBoneByName(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return null;
        
        string searchName = boneName.ToLower();
        
        // Try exact match first
        if (boneTransforms.ContainsKey(searchName))
        {
            return boneTransforms[searchName];
        }
        
        // Try partial match - bone name contains search term
        foreach (var kvp in boneTransforms)
        {
            if (kvp.Key.Contains(searchName))
            {
                if (debugMode)
                {
                    Debug.Log($"Found bone '{boneName}' as partial match: '{kvp.Key}'");
                }
                return kvp.Value;
            }
        }
        
        // Try reverse partial match - search term contains bone name
        foreach (var kvp in boneTransforms)
        {
            if (searchName.Contains(kvp.Key))
            {
                if (debugMode)
                {
                    Debug.Log($"Found bone '{boneName}' as reverse partial match: '{kvp.Key}'");
                }
                return kvp.Value;
            }
        }
        
        if (debugMode)
        {
            Debug.LogWarning($"No bone found for '{boneName}'");
        }
        
        return null;
    }
    
    
    
    public void ClearAllBoxes()
    {
        // Clear all PCJ boxes by searching through all bones recursively
        List<GameObject> boxesToDestroy = new List<GameObject>();
        
        // Search recursively through all bones for PCJ boxes
        FindPCJBoxesRecursive(transform, boxesToDestroy);
        
        // Destroy all found boxes
        foreach (GameObject box in boxesToDestroy)
        {
            if (box != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(box);
                }
                else
                {
                    DestroyImmediate(box);
                }
            }
        }
        
        createdBoxes.Clear();
        pcjBoxes.Clear();
        
        if (debugMode)
        {
            Debug.Log($"Cleared {boxesToDestroy.Count} PCJ boxes");
        }
    }
    
    void FindPCJBoxesRecursive(Transform parent, List<GameObject> boxesToDestroy)
    {
        // Check all children of current transform
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            
            // If this is a PCJ box, add it to destroy list
            if (child.name.StartsWith("PCJ_"))
            {
                boxesToDestroy.Add(child.gameObject);
            }
            else
            {
                // Recursively search children
                FindPCJBoxesRecursive(child, boxesToDestroy);
            }
        }
    }
    
}
