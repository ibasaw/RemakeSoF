using UnityEngine;
using UnityEditor;
using System.Linq;
using Tolik.RemakeSoF.Runtime;

[CustomEditor(typeof(MyPlayerMaterialAssigner))]
public class MyPlayerMaterialAssignerEditor : Editor
{
    private SerializedProperty selectedModelNameProp;
    private SerializedProperty selectedSkinNameProp;
    private SerializedProperty availableSkinsProp;
    private SerializedProperty urpShaderNameProp;
    private SerializedProperty surfaceDefinitionProp;

    private string[] availableSkins;
    private int selectedSkinIndex = -1;

    private void OnEnable()
    {
        selectedModelNameProp = serializedObject.FindProperty("selectedModelName");
        selectedSkinNameProp = serializedObject.FindProperty("selectedSkinName");
        availableSkinsProp = serializedObject.FindProperty("availableSkins");
        urpShaderNameProp = serializedObject.FindProperty("urpShaderName");
        surfaceDefinitionProp = serializedObject.FindProperty("surfaceDefinition");

        RefreshAvailableSkins(selectedModelNameProp.stringValue);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skin Selection", EditorStyles.boldLabel);

        // Model dropdown
        if (selectedModelNameProp != null)
        {
            string currentModel = selectedModelNameProp.stringValue;
            string[] modelOptions = new string[] { "average_sleeves", "suit_long_coat", "suit_sleeves", "average_armor",
            "female_armor", "female_skirt", "female_pants", "fat", "snow", "chem_suit", "dog", "osprey" };
            int currentModelIndex = System.Array.IndexOf(modelOptions, currentModel);
            int newModelIndex = EditorGUILayout.Popup("Select Model", currentModelIndex, modelOptions);
            if (newModelIndex != currentModelIndex && newModelIndex >= 0 && newModelIndex < modelOptions.Length)
            {
                selectedModelNameProp.stringValue = modelOptions[newModelIndex];
            }
        }
        EditorGUILayout.Space();

        // Refresh button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Available Skins", GUILayout.Width(200)))
        {
            RefreshAvailableSkins(selectedModelNameProp.stringValue);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Skin dropdown
        if (availableSkins != null && availableSkins.Length > 0)
        {
            // Find current selection index
            string currentSkin = selectedSkinNameProp.stringValue;
            selectedSkinIndex = System.Array.IndexOf(availableSkins, currentSkin);

            // Create dropdown
            int newIndex = EditorGUILayout.Popup("Select Skin", selectedSkinIndex, availableSkins);

            if (newIndex != selectedSkinIndex && newIndex >= 0 && newIndex < availableSkins.Length)
            {
                selectedSkinNameProp.stringValue = availableSkins[newIndex];
                selectedSkinIndex = newIndex;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No skins with '" + selectedModelNameProp.stringValue + "' model found. Make sure skin_data JSON files include this model.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Show available skins (read-only)
        EditorGUILayout.LabelField("Available Skins:", EditorStyles.boldLabel);
        if (availableSkins != null && availableSkins.Length > 0)
        {
            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < availableSkins.Length; i++)
            {
                EditorGUILayout.LabelField($"• {availableSkins[i]}");
            }
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.LabelField("No skins available");
        }

        EditorGUILayout.Space();

        // Other properties
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(urpShaderNameProp, new GUIContent("URP Shader Name"));
        EditorGUILayout.PropertyField(surfaceDefinitionProp, new GUIContent("Surface Definition"));

        EditorGUILayout.Space();

        // Apply button
        if (GUILayout.Button("Apply Selected Skin", GUILayout.Height(30)))
        {
            ApplySelectedSkin();
        }

        EditorGUILayout.Space();

        // Reset GameObjects button
        if (GUILayout.Button("Reset All GameObjects to Active", GUILayout.Height(25)))
        {
            ResetAllGameObjects();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void RefreshAvailableSkins(string modelFilter = "average_sleeves")
    {
        // Get all JSON files from skin_data directory
        var allSkinFiles = Resources.LoadAll<TextAsset>("Data/skin_data");
        var skinFiles = new System.Collections.Generic.List<string>();

        foreach (var skinFile in allSkinFiles)
        {
            try
            {
                var rootJson = Newtonsoft.Json.JsonConvert.DeserializeObject<MyPlayerMaterialAssigner.RootJson>(skinFile.text);
                if (rootJson?.prefs?.models != null &&
                    rootJson.prefs.models.TryGetValue("1", out string model) &&
                    model == modelFilter)
                {
                    skinFiles.Add(skinFile.name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error parsing skin file {skinFile.name}: {ex.Message}");
            }
        }

        availableSkins = skinFiles.ToArray();

        // Update the serialized property
        availableSkinsProp.arraySize = availableSkins.Length;
        for (int i = 0; i < availableSkins.Length; i++)
        {
            availableSkinsProp.GetArrayElementAtIndex(i).stringValue = availableSkins[i];
        }

        Debug.Log($"Found {availableSkins.Length} skins with average_sleeves model: {string.Join(", ", availableSkins)}");
    }

    private void ApplySelectedSkin()
    {
        var materialAssigner = (MyPlayerMaterialAssigner)target;
        string selectedSkin = selectedSkinNameProp.stringValue;

        if (!string.IsNullOrEmpty(selectedSkin))
        {
            materialAssigner.ChangeSkin(selectedSkin);
            Debug.Log($"Applied skin: {selectedSkin}");
        }
        else
        {
            Debug.LogWarning("No skin selected");
        }
    }

    private void ResetAllGameObjects()
    {
        var materialAssigner = (MyPlayerMaterialAssigner)target;
        materialAssigner.ResetAllGameObjectsToActivePublic();
        Debug.Log("Reset all GameObjects to active");
    }
}
