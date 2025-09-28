using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Simple container to hold ghoul2 props on the imported prefab
[DisallowMultipleComponent]
public class Ghoul2Meta : MonoBehaviour
{
    public string g2_prop_name;
    public string g2_prop_shader;
    public bool g2_prop_tag;
    public bool g2_prop_off;
    // add other fields you need...
}

public class FBXGhoul2PropsImporter : AssetPostprocessor
{
    // Optional: make Unity treat these names as user properties
    // This runs BEFORE import; set any property names you expect here.
    /*void OnPreprocessModel()
    {
        var mi = assetImporter as ModelImporter;
        if (mi == null) return;

        // add names you expect in the file
        mi.extraUserProperties = new string[] {
            "g2_prop_name",
            "g2_prop_shader",
            "g2_prop_tag",
            "g2_prop_off"
        };
    }

    // Called once for each GameObject that had user properties
    void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, object[] values)
    {
        if (propNames == null || propNames.Length == 0) return;

        // find or add the meta component on this GameObject
        var meta = go.GetComponent<Ghoul2Meta>();
        if (meta == null) meta = go.AddComponent<Ghoul2Meta>();

        for (int i = 0; i < propNames.Length; i++)
        {
            var name = propNames[i];
            var val = values[i];

            // map and assign with type checks
            try
            {
                switch (name)
                {
                    case "m_is_visible":
                        if (go.name.Contains("stupidtriangle_")) continue;
                        go.SetActive(Convert.ToBoolean(val));
                        break;
                    default:
                        Debug.Log($"[FBXPropsImporter] Unknown prop {name} = {val} on {go.name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FBXPropsImporter] Failed to parse property {name} on {go.name}: {ex.Message}");
            }
        }

        // debug
        Debug.Log($"[FBXPropsImporter] Applied {propNames.Length} user properties to {go.name}");
    }*/
}
