using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Dynamic container to hold all custom properties from FBX files
/// Automatically stores any custom property found during import
/// </summary>
[DisallowMultipleComponent]
public class Ghoul2Meta : MonoBehaviour
{
    [Header("Dynamic Properties")]
    [SerializeField] private List<string> propertyNames = new List<string>();
    [SerializeField] private List<string> propertyValues = new List<string>();
    [SerializeField] private List<string> propertyTypes = new List<string>();
    
    // Dictionary for fast runtime access
    private Dictionary<string, object> properties = new Dictionary<string, object>();
    
    /// <summary>
    /// Set a property value dynamically during import
    /// </summary>
    public void SetProperty(string name, object value)
    {
        // Convert value to appropriate type and store
        properties[name] = value;
        
        // Update serialized lists for inspector display
        int index = propertyNames.IndexOf(name);
        if (index >= 0)
        {
            // Update existing property
            propertyValues[index] = value?.ToString() ?? "null";
            propertyTypes[index] = value?.GetType().Name ?? "null";
        }
        else
        {
            // Add new property
            propertyNames.Add(name);
            propertyValues.Add(value?.ToString() ?? "null");
            propertyTypes.Add(value?.GetType().Name ?? "null");
        }
    }
    
    /// <summary>
    /// Get a property value by name
    /// </summary>
    public T GetProperty<T>(string name, T defaultValue = default(T))
    {
        if (properties.TryGetValue(name, out object value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;
                    
                // Try to convert the value
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Ghoul2Meta] Failed to convert property '{name}' to {typeof(T).Name}: {ex.Message}");
                return defaultValue;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Get a string property (most common case)
    /// </summary>
    public string GetString(string name, string defaultValue = "")
    {
        return GetProperty(name, defaultValue);
    }
    
    /// <summary>
    /// Get a boolean property
    /// </summary>
    public bool GetBool(string name, bool defaultValue = false)
    {
        return GetProperty(name, defaultValue);
    }
    
    /// <summary>
    /// Get a numeric property
    /// </summary>
    public float GetFloat(string name, float defaultValue = 0f)
    {
        return GetProperty(name, defaultValue);
    }
    
    /// <summary>
    /// Check if a property exists
    /// </summary>
    public bool HasProperty(string name)
    {
        return properties.ContainsKey(name);
    }
    
    /// <summary>
    /// Get all property names
    /// </summary>
    public string[] GetPropertyNames()
    {
        return properties.Keys.ToArray();
    }
    
    // Convenience properties for common cases
    public string ShaderFile => GetString("shader_file", GetString("g2_prop_shader", ""));
    public string PropName => GetString("g2_prop_name");
    public bool PropTag => GetBool("g2_prop_tag");
    public bool PropOff => GetBool("g2_prop_off");
    public bool IsVisible => GetBool("m_is_visible", true);
    public string Q3MapMaterial => GetString("q3map_material", "");
    
    private void Awake()
    {
        // Rebuild properties dictionary from serialized data
        RebuildPropertiesDictionary();
    }
    
    private void Start()
    {
        // Debug log all properties
        /*if (properties.Count > 0)
        {
            Debug.Log($"[Ghoul2Meta] {gameObject.name} has {properties.Count} properties:");
            foreach (var kvp in properties)
            {
                Debug.Log($"  {kvp.Key} = {kvp.Value} ({kvp.Value?.GetType().Name})");
            }
        }*/
    }
    
    /// <summary>
    /// Rebuild the properties dictionary from serialized lists
    /// </summary>
    private void RebuildPropertiesDictionary()
    {
        properties.Clear();
        
        for (int i = 0; i < propertyNames.Count && i < propertyValues.Count && i < propertyTypes.Count; i++)
        {
            string name = propertyNames[i];
            string valueStr = propertyValues[i];
            string typeName = propertyTypes[i];
            
            // Try to convert back to original type
            object value = ConvertStringToType(valueStr, typeName);
            properties[name] = value;
        }
    }
    
    /// <summary>
    /// Convert string representation back to original type
    /// </summary>
    private object ConvertStringToType(string valueStr, string typeName)
    {
        if (string.IsNullOrEmpty(valueStr) || valueStr == "null")
            return null;
            
        try
        {
            switch (typeName)
            {
                case "Boolean":
                    return Convert.ToBoolean(valueStr);
                case "Int32":
                    return Convert.ToInt32(valueStr);
                case "Single":
                    return Convert.ToSingle(valueStr);
                case "Double":
                    return Convert.ToDouble(valueStr);
                case "String":
                default:
                    return valueStr;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Ghoul2Meta] Failed to convert '{valueStr}' to {typeName}: {ex.Message}");
            return valueStr; // Fallback to string
        }
    }
}
