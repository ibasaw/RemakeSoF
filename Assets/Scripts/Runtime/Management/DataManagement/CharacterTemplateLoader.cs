using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service für das Laden und Parsen von Character-Templates aus Resources.
    /// Zugreifbar über ServiceLocator.
    /// </summary>
    public class CharacterTemplateLoader
    {
        private readonly Dictionary<string, CharacterTemplate> m_CharacterTemplatesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<CharacterTemplate>> m_CharacterTemplatesBySkinName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und lädt alle Templates automatisch
        /// </summary>
        public CharacterTemplateLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Loads all character templates from Resources/Data/SoF2_NPCs.json
        /// </summary>
        private void LoadFromResources(string dataPath = "Data/SoF2_NPCs.json")
        {
            m_CharacterTemplatesByName.Clear();
            m_CharacterTemplatesBySkinName.Clear();

            string filePath = Path.Combine(Application.streamingAssetsPath, dataPath);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[CharacterTemplateLoader] Character templates file not found at {filePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<CharacterTemplate> parsed = ParseCharacterTemplatesJson(json);
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

                Debug.Log($"[CharacterTemplateLoader] Loaded {m_CharacterTemplatesByName.Count} character templates with {m_CharacterTemplatesBySkinName.Count} unique templates: {string.Join(", ", m_CharacterTemplatesByName.Keys)}");
            }
            catch (Exception ex)
            {
                m_CharacterTemplatesByName.Clear();
                m_CharacterTemplatesBySkinName.Clear();
                Debug.LogError($"[CharacterTemplateLoader] Error parsing character templates: {ex}");
            }
        }

        /// <summary>
        /// Custom parser for nested CharacterTemplate JSON structure.
        /// Handles the format: { "file.npc": { "GroupInfo": {...}, "CharacterTemplate": [...] } }
        /// </summary>
        private List<CharacterTemplate> ParseCharacterTemplatesJson(string json)
        {
            List<CharacterTemplate> result = new();

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
                Debug.LogError($"[CharacterTemplateLoader] Error in ParseCharacterTemplatesJson: {ex}");
            }

            return result;
        }

        public List<CharacterTemplate> GetBySkinName(string skinName)
        {
            if (m_CharacterTemplatesBySkinName.TryGetValue(skinName, out List<CharacterTemplate> templates))
            {
                return new List<CharacterTemplate>(templates);
            }
            return new List<CharacterTemplate>();
        }

        public CharacterTemplate GetByName(string templateName)
        {
            m_CharacterTemplatesByName.TryGetValue(templateName, out CharacterTemplate template);
            return template;
        }

        public void ClearCache()
        {
            m_CharacterTemplatesByName.Clear();
            m_CharacterTemplatesBySkinName.Clear();
            Debug.Log("[CharacterTemplateLoader] Cache cleared.");
        }
    }
}
