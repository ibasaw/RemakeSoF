using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoneAxis
{
    public string Name;
    public string Fwd;
    public string Up;
    public string Right;
}

[System.Serializable]
public class PCJDefinition
{
    public string Name;
    public string Parent;
    public float[] Maxs = new float[3];
    public float[] Mins = new float[3];
    public BoneAxis[] BoneAim;
    public BoneAxis[] BoneRelative;
    public BoneAxis[] BoneSmooth;
    public BoneAxis[] BoneExtra;
}

[System.Serializable]
public class SkeletonInfo
{
    public string GLMFile;
    public string FramesFile;
    public int MaxSpeed;
    public string HeaderAnim;
    public string TrailerAnim;
    public PCJDefinition[] PCJ;
}

[System.Serializable]
public class SkeletonConfig
{
    public SkeletonInfo SkeletonInfo;
}

public static class SkeletonConfigLoader
{
    private static Dictionary<string, PCJDefinition> pcjDefinitions = new Dictionary<string, PCJDefinition>();
    private static bool isLoaded = false;

    public static void LoadSkeletonConfig(string jsonPath)
    {
        if (isLoaded) return;

        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);
            if (jsonFile == null)
            {
                Debug.LogError($"Could not load skeleton config from: {jsonPath}");
                return;
            }

            SkeletonConfig config = JsonUtility.FromJson<SkeletonConfig>(jsonFile.text);
            
            // Load PCJ definitions into dictionary
            pcjDefinitions.Clear();
            foreach (var pcj in config.SkeletonInfo.PCJ)
            {
                pcjDefinitions[pcj.Name] = pcj;
            }

            isLoaded = true;
            Debug.Log($"Loaded {pcjDefinitions.Count} PCJ definitions from skeleton config");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading skeleton config: {e.Message}");
        }
    }

    public static PCJDefinition GetPCJDefinition(string name)
    {
        if (!isLoaded)
        {
            Debug.LogWarning("Skeleton config not loaded. Call LoadSkeletonConfig first.");
            return null;
        }

        if (pcjDefinitions.TryGetValue(name, out PCJDefinition definition))
        {
            return definition;
        }

        Debug.LogWarning($"PCJ definition not found for: {name}");
        return null;
    }

    public static Vector3 GetAngleLimits(string pcjName, bool isMax)
    {
        PCJDefinition pcj = GetPCJDefinition(pcjName);
        if (pcj == null) return Vector3.zero;

        if (isMax)
        {
            return new Vector3(pcj.Maxs[0], pcj.Maxs[1], pcj.Maxs[2]);
        }
        else
        {
            return new Vector3(pcj.Mins[0], pcj.Mins[1], pcj.Mins[2]);
        }
    }

    public static bool IsLoaded => isLoaded;
}
