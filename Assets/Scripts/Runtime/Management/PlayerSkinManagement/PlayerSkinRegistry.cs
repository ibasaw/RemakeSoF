using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Registry for managing available skin definitions and metadata.
    /// Handles loading and querying of skin data from Resources. (JSON files)
    /// Registry = Daten-Katalog (statisch/read-only)
    /// Alle verfügbaren Skins
    /// Surface-Definitionen
    /// Lookup-Funktionen
    /// Zustandslos – nur Referenzdaten
    /// PlayerSkinRegistry → lädt Skin-Metadaten (JSON: welche Texturen, Surfaces, Shader)
    /// </summary>
    public class PlayerSkinDataRegistry
    {
        private Dictionary<string, List<SkinDefinition>> m_SkinDataByModel = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, SkinDefinition> m_SkinDataByName = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Dictionary<string, ShaderEntry>> m_LegacyShaderDefinitionsByModel = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, SkinSurfaceDefinition> m_SkinSurfaceDefinitionsByModel = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SkinSurfaceDefinition> SkinSurfaceDefinitionsByModel => m_SkinSurfaceDefinitionsByModel;
        public TextureManager TextureManager => ApplicationEntryPoint.Singleton.TextureManager;
        public PlayerSkinDataRegistry()
        {
            LoadAllSkinDataFromResources();
            LoadSurfaceDefinitionsFromResources();

            m_SkinDataByModel.Keys.ToList().ForEach(model =>
            {
                m_LegacyShaderDefinitionsByModel[model] = LoadLegacyShaderDefinitionForModel($"Data/shaders/{model}");
            });
            Debug.Log($"[PlayerSkinRegistry] Loaded {m_LegacyShaderDefinitionsByModel.Count} legacy shader definitions for models: {string.Join(", ", m_LegacyShaderDefinitionsByModel.Keys)}");
        }

        public string GetNextSkinName(string currentName)
        {
            var keys = m_SkinDataByName.Keys.ToList();
            if (keys.Count == 0) return null;
            int idx = keys.IndexOf(currentName);
            int nextIdx = (idx + 1) % keys.Count;
            return keys[nextIdx];
        }

        public string GetPreviousSkinName(string currentName)
        {
            var keys = m_SkinDataByName.Keys.ToList();
            if (keys.Count == 0) return null;
            int idx = keys.IndexOf(currentName);
            int prevIdx = (idx - 1 + keys.Count) % keys.Count;
            return keys[prevIdx];
        }

        /// <summary>
        /// Manually load original SoF2 .shader data definition from Resources folder (world,player)
        /// </summary>
        private Dictionary<string, ShaderEntry> LoadLegacyShaderDefinitionForModel(string modelName)
        {
            // Erwartet z. B. "Data/shaders/model_name"
            string fullPath = Path.Combine(Application.dataPath, "Resources", modelName + ".shader");
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[PlayerSkinManager] Shader definition file not found at: {fullPath}");
                return null;
            }

            try
            {
                string shaderText = File.ReadAllText(fullPath);
                if (string.IsNullOrEmpty(shaderText))
                {
                    Debug.LogError($"[PlayerSkinManager] Shader definition file is empty or invalid at: {fullPath}");
                    return null;
                }

                return ShaderDataReader.ParseShaderEntries(shaderText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerSkinManager] Error reading/parsing shader file: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Loads all available skin data from Resources folder
        /// </summary>
        public void LoadAllSkinDataFromResources(string resourcePath = "Data/skin_data")
        {
            m_SkinDataByModel.Clear();
            m_SkinDataByName.Clear();

            var allSkinFiles = Resources.LoadAll<TextAsset>(resourcePath);

            foreach (var skinFile in allSkinFiles)
            {
                try
                {
                    string fileName = skinFile.name;
                    //Debug.Log($"[PlayerSkinRegistry] Loading skin file: {fileName}");
                    var skinDefinition = JsonConvert.DeserializeObject<SkinDefinition>(skinFile.text);

                    if (skinDefinition?.prefs?.models != null &&
                        skinDefinition.prefs.models.TryGetValue("1", out string model))
                    {
                        // Add to model-based dictionary
                        if (!m_SkinDataByModel.ContainsKey(model))
                        {
                            m_SkinDataByModel[model] = new List<SkinDefinition>();
                        }
                        m_SkinDataByModel[model].Add(skinDefinition);

                        // Add to name-based dictionary for quick lookup
                        if (!string.IsNullOrEmpty(skinFile.name))
                        {
                            m_SkinDataByName[skinFile.name] = skinDefinition;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlayerSkinRegistry] Error parsing skin file {skinFile.name}: {ex.Message}");
                }
            }

            Debug.Log($"[PlayerSkinRegistry] Loaded {m_SkinDataByModel.Count} model types with {m_SkinDataByName.Count} total skins: " +
                string.Join(", ", m_SkinDataByModel.Select(kvp => $"{kvp.Key} ({kvp.Value.Count} skins)")));
        }

        public Dictionary<string, ShaderEntry> GetLegacyShaderDefinitionForModel(string modelName)
        {
            m_LegacyShaderDefinitionsByModel.TryGetValue(modelName, out var shaderEntries);
            return shaderEntries;
        }

        /// <summary>
        /// Get all skins available for a specific model type
        /// </summary>
        public List<SkinDefinition> GetSkinsForModel(string modelType)
        {
            if (m_SkinDataByModel.TryGetValue(modelType, out var skins))
            {
                return new List<SkinDefinition>(skins);
            }
            return new List<SkinDefinition>();
        }

        /// <summary>
        /// Get a specific skin by name
        /// </summary>
        public SkinDefinition GetSkinByName(string skinName)
        {
            m_SkinDataByName.TryGetValue(skinName, out var skin);
            return skin;
        }

        /// <summary>
        /// Get all available model types
        /// </summary>
        public string[] GetAvailableModels()
        {
            return m_SkinDataByModel.Keys.ToArray();
        }

        /// <summary>
        /// Check if a skin exists
        /// </summary>
        public bool HasSkin(string skinName)
        {
            return m_SkinDataByName.ContainsKey(skinName);
        }

        /// <summary>
        /// Get total number of loaded skins
        /// </summary>
        public int TotalSkinCount => m_SkinDataByName.Count;

        /// <summary>
        /// Get number of skins for a specific model
        /// </summary>
        public int GetSkinCountForModel(string modelType)
        {
            return m_SkinDataByModel.TryGetValue(modelType, out var skins) ? skins.Count : 0;
        }

        /// <summary>
        /// Loads and parses NPC_definition.json from Resources/Data.
        /// </summary>
        private void LoadSurfaceDefinitionsFromResources(string resourcePath = "Data/NPC_definition")
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[PlayerSkinRegistry] Surface definition not found at Resources/{resourcePath}.json");
                return;
            }

            ParseSurfaceDefinition(asset.text);
        }

        /// <summary>
        /// Parses the surface definition JSON (variant -> parts mapping).
        /// </summary>
        /// <param name="json">Raw JSON from definition file.</param>
        private void ParseSurfaceDefinition(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[PlayerSkinRegistry] Surface definition json is empty");
                return;
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, SkinSurfaceDefinition>>(json);
                if (parsed == null)
                {
                    Debug.LogWarning("[PlayerSkinRegistry] Surface definition parsed to null");
                    m_SkinSurfaceDefinitionsByModel.Clear();
                    return;
                }

                m_SkinSurfaceDefinitionsByModel = new Dictionary<string, SkinSurfaceDefinition>(parsed, StringComparer.OrdinalIgnoreCase);
                Debug.Log($"[PlayerSkinRegistry] Surface definitions parsed. Variants: {m_SkinSurfaceDefinitionsByModel.Count}. Keys: {string.Join(", ", m_SkinSurfaceDefinitionsByModel.Keys)}");
            }
            catch (Exception ex)
            {
                m_SkinSurfaceDefinitionsByModel.Clear();
                Debug.LogError($"[PlayerSkinRegistry] Error parsing surface definition: {ex}");
            }
        }
    }
}
