using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FBXGhoul2PropsImporter : AssetPostprocessor
{
    // Called once for each GameObject that had user properties
    // This version dynamically handles ALL properties without needing to predefine them
    void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, object[] values)
    {
        if (propNames == null || propNames.Length == 0) return;

        // find or add the meta component on this GameObject
        var meta = go.GetComponent<Ghoul2Meta>();
        if (meta == null) meta = go.AddComponent<Ghoul2Meta>();

        int propertiesSet = 0;
        
        for (int i = 0; i < propNames.Length; i++)
        {
            var name = propNames[i];
            var val = values[i];
            
            try
            {
                // Handle special cases first
                if (name == "m_is_visible")
                {
                    // Special handling for visibility - apply directly to GameObject
                    if (!go.name.Contains("stupidtriangle_"))
                    {
                        bool isVisible = Convert.ToBoolean(val);
                        go.SetActive(isVisible);
                        meta.SetProperty(name, isVisible);
                        propertiesSet++;
                    }
                }
                else
                {
                    // Dynamically store all other properties
                    meta.SetProperty(name, val);
                    propertiesSet++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FBXPropsImporter] Failed to set property '{name}' = '{val}' on {go.name}: {ex.Message}");
            }
        }
        
        // Debug log with more detailed info
        string shaderFile = meta.GetString("shader_file", meta.GetString("g2_prop_shader", ""));
        Debug.Log($"[FBXPropsImporter] {go.name}: Set {propertiesSet}/{propNames.Length} properties. Shader: '{shaderFile}'");
        
        // Log all properties for debugging (only for first few objects to avoid spam)
        if (propNames.Length > 0)
        {
            string propList = string.Join(", ", propNames.Select((name, idx) => $"{name}={values[idx]}"));
            Debug.Log($"[FBXPropsImporter] All properties on {go.name}: {propList}");
        }
    }
}
