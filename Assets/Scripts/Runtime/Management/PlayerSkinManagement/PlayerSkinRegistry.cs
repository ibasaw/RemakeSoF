using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
        private readonly Dictionary<string, List<SkinDefinition>> m_SkinDefinitionsByModelName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SkinDefinition> m_SkinDefinitionByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, ShaderEntry>> m_LegacyShaderEntriesByModel = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, SkinSurfaceDefinition> m_SkinSurfaceDefinitionsByModel = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SkinSurfaceDefinition> SkinSurfaceDefinitionsByModel => m_SkinSurfaceDefinitionsByModel;
        private readonly Dictionary<string, CharacterTemplate> m_CharacterTemplatesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<CharacterTemplate>> m_CharacterTemplatesBySkinName = new(StringComparer.OrdinalIgnoreCase);

        public PlayerSkinDataRegistry()
        {
            LoadAllSkinDataFromResources();
            LoadSurfaceDefinitionsFromResources();
            LoadAllCharacterTemplatesFromResources();

            m_SkinDefinitionsByModelName.Keys.ToList().ForEach(model =>
            {
                m_LegacyShaderEntriesByModel[model] = LoadLegacyShaderEntriesForModel($"Data/shaders/{model}");
            });

            Debug.Log($"[PlayerSkinRegistry] Loaded {m_LegacyShaderEntriesByModel.Count} legacy shader definitions for models: {string.Join(", ", m_LegacyShaderEntriesByModel.Keys)}");
        }

        public string GetNextSkinName(string currentName)
        {
            var keys = m_SkinDefinitionByName.Keys.ToList();
            if (keys.Count == 0) return null;
            int idx = keys.IndexOf(currentName);
            int nextIdx = (idx + 1) % keys.Count;
            return keys[nextIdx];
        }

        public string GetPreviousSkinName(string currentName)
        {
            var keys = m_SkinDefinitionByName.Keys.ToList();
            if (keys.Count == 0) return null;
            int idx = keys.IndexOf(currentName);
            int prevIdx = (idx - 1 + keys.Count) % keys.Count;
            return keys[prevIdx];
        }

        /// <summary>
        /// Manually load original SoF2 .shader data definition from Resources folder (world,player)
        /// </summary>
        private Dictionary<string, ShaderEntry> LoadLegacyShaderEntriesForModel(string modelName)
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
            m_SkinDefinitionsByModelName.Clear();
            m_SkinDefinitionByName.Clear();

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
                        if (!m_SkinDefinitionsByModelName.ContainsKey(model))
                        {
                            m_SkinDefinitionsByModelName[model] = new List<SkinDefinition>();
                        }
                        m_SkinDefinitionsByModelName[model].Add(skinDefinition);

                        // Add to name-based dictionary for quick lookup
                        if (!string.IsNullOrEmpty(skinFile.name))
                        {
                            m_SkinDefinitionByName[skinFile.name] = skinDefinition;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlayerSkinRegistry] Error parsing skin file {skinFile.name}: {ex.Message}");
                }
            }

            Debug.Log($"[PlayerSkinRegistry] Loaded {m_SkinDefinitionsByModelName.Count} model types with {m_SkinDefinitionByName.Count} total skins: " +
                string.Join(", ", m_SkinDefinitionsByModelName.Select(kvp => $"{kvp.Key} ({kvp.Value.Count} skins)")));
        }

        public Dictionary<string, ShaderEntry> GetLegacyShaderDefinitionForModel(string modelName)
        {
            m_LegacyShaderEntriesByModel.TryGetValue(modelName, out var shaderEntries);
            return shaderEntries;
        }

        /// <summary>
        /// Get all skins available for a specific model type
        /// </summary>
        public List<SkinDefinition> GetSkinsForModel(string modelType)
        {
            if (m_SkinDefinitionsByModelName.TryGetValue(modelType, out var skins))
            {
                return new List<SkinDefinition>(skins);
            }
            return new List<SkinDefinition>();
        }

        /// <summary>
        /// Get a specific SkinDefinition by name
        /// </summary>
        public SkinDefinition GetSkinDefinitionByName(string skinName)
        {
            m_SkinDefinitionByName.TryGetValue(skinName, out var skin);
            return skin;
        }

        public List<CharacterTemplate> GetCharacterTemplatesBySkinName(string skinName)
        {
            if (m_CharacterTemplatesBySkinName.TryGetValue(skinName, out var templates))
            {
                return new List<CharacterTemplate>(templates);
            }
            return new List<CharacterTemplate>();
        }

        public CharacterTemplate GetCharacterTemplateByName(string templateName)
        {
            m_CharacterTemplatesByName.TryGetValue(templateName, out var template);
            return template;
        }

        /// <summary>
        /// Get all available model types
        /// </summary>
        public string[] GetAvailableModels()
        {
            return m_SkinDefinitionsByModelName.Keys.ToArray();
        }

        /// <summary>
        /// Check if a skin exists
        /// </summary>
        public bool HasSkin(string skinName)
        {
            return m_SkinDefinitionByName.ContainsKey(skinName);
        }

        /// <summary>
        /// Get total number of loaded skins
        /// </summary>
        public int TotalSkinCount => m_SkinDefinitionByName.Count;

        /// <summary>
        /// Get number of skins for a specific model
        /// </summary>
        public int GetSkinCountForModel(string modelType)
        {
            return m_SkinDefinitionsByModelName.TryGetValue(modelType, out var skins) ? skins.Count : 0;
        }

        private void LoadAllCharacterTemplatesFromResources()
        {
            m_CharacterTemplatesByName.Clear();
            m_CharacterTemplatesBySkinName.Clear();
            string resourcePath = "Data/SoF2_NPCs";
            TextAsset dataRaw = Resources.Load<TextAsset>(resourcePath);
            if (dataRaw == null)
            {
                Debug.LogWarning($"[PlayerSkinRegistry] Character templates file not found at Resources/{resourcePath}.json");
                return;
            }
            try
            {
                List<CharacterTemplate> parsed = ParseCharacterTemplatesJson(dataRaw.text);
                foreach (CharacterTemplate template in parsed)
                {
                    if (!string.IsNullOrEmpty(template.Name))
                    {
                        m_CharacterTemplatesByName[template.Name] = template;
                    }

                    // Build skin name lookup
                    if (template.SkinTemplates != null)
                    {
                        foreach (SkinTemplate skinTemplate in template.SkinTemplates)
                        {
                            if (!string.IsNullOrEmpty(skinTemplate.SkinName))
                            {
                                if (!m_CharacterTemplatesBySkinName.ContainsKey(skinTemplate.SkinName))
                                {
                                    m_CharacterTemplatesBySkinName[skinTemplate.SkinName] = new List<CharacterTemplate>();
                                }
                                m_CharacterTemplatesBySkinName[skinTemplate.SkinName].Add(template);
                            }
                        }
                    }
                }

                Debug.Log($"[PlayerSkinRegistry] Loaded {m_CharacterTemplatesByName.Count} character templates with {m_CharacterTemplatesBySkinName.Count} unique skins: {string.Join(", ", m_CharacterTemplatesByName.Keys)}");
            }
            catch (Exception ex)
            {
                m_CharacterTemplatesByName.Clear();
                m_CharacterTemplatesBySkinName.Clear();
                Debug.LogError($"[PlayerSkinRegistry] Error parsing character templates: {ex}");
            }
        }

        /// <summary>
        /// Custom parser for nested CharacterTemplate JSON structure.
        /// Handles the format: { "file.npc": { "GroupInfo": {...}, "CharacterTemplate": [...] } }
        /// </summary>
        private List<CharacterTemplate> ParseCharacterTemplatesJson(string json)
        {
            var result = new List<CharacterTemplate>();

            if (string.IsNullOrEmpty(json))
                return result;

            try
            {
                // Parse as dynamic nested dictionary
                Dictionary<string, NpcFileEntry> parsed = JsonConvert.DeserializeObject<Dictionary<string, NpcFileEntry>>(json);

                if (parsed == null)
                    return result;

                foreach (KeyValuePair<string, NpcFileEntry> kvp in parsed)
                {
                    string fileName = kvp.Key;
                    NpcFileEntry fileEntry = kvp.Value;

                    if (fileEntry?.CharacterTemplate == null)
                        continue;

                    foreach (CharacterTemplate template in fileEntry.CharacterTemplate)
                    {
                        if (template != null)
                        {
                            template.ParentTemplate = fileEntry.GroupInfo?.ParentTemplate;
                            result.Add(template);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerSkinRegistry] Error in ParseCharacterTemplatesJson: {ex}");
            }

            return result;
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
                Dictionary<string, SkinSurfaceDefinition> parsed = JsonConvert.DeserializeObject<Dictionary<string, SkinSurfaceDefinition>>(json);
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

        public void ClearAllCaches()
        {
            m_SkinDefinitionsByModelName.Clear();
            m_SkinDefinitionByName.Clear();
            m_LegacyShaderEntriesByModel.Clear();
            m_SkinSurfaceDefinitionsByModel.Clear();
            m_CharacterTemplatesByName.Clear();
            m_CharacterTemplatesBySkinName.Clear();
            Debug.Log("[PlayerSkinRegistry] Cleared all cached json/shader/surface/template skin data.");
        }
    }
}
