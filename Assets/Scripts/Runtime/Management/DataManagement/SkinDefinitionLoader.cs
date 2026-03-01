using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service für das Laden und Parsen von Skin-Definitionen aus Resources.
    /// Zugreifbar über ServiceLocator.
    /// </summary>
    public class SkinDefinitionLoader
    {
        private readonly Dictionary<string, List<SkinDefinition>> m_SkinDefinitionsByModelName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SkinDefinition> m_SkinDefinitionByName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und lädt alle Skin-Daten automatisch
        /// </summary>
        public SkinDefinitionLoader()
        {
            LoadAllFromResources();
        }

        /// <summary>
        /// Loads all available skin data from Resources folder
        /// </summary>
        private void LoadAllFromResources(string resourcePath = "Data/skin_data")
        {
            m_SkinDefinitionsByModelName.Clear();
            m_SkinDefinitionByName.Clear();

            TextAsset[] allSkinFiles = Resources.LoadAll<TextAsset>(resourcePath);

            foreach (TextAsset skinFile in allSkinFiles)
            {
                try
                {
                    string fileName = skinFile.name;
                    SkinDefinition skinDefinition = JsonConvert.DeserializeObject<SkinDefinition>(skinFile.text);

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
                    Debug.LogWarning($"[SkinDefinitionLoader] Error parsing skin file {skinFile.name}: {ex.Message}");
                }
            }

            Debug.Log($"[SkinDefinitionLoader] Loaded {m_SkinDefinitionsByModelName.Count} model types with {m_SkinDefinitionByName.Count} total skins: " +
                string.Join(", ", m_SkinDefinitionsByModelName.Select(kvp => $"{kvp.Key} ({kvp.Value.Count} skins)")));
        }

        public string GetNextSkinName(string currentName)
        {
            List<string> keys = m_SkinDefinitionByName.Keys.ToList();
            if (keys.Count == 0) return null;
            int idx = keys.IndexOf(currentName);
            int nextIdx = (idx + 1) % keys.Count;
            return keys[nextIdx];
        }

        public string GetPreviousSkinName(string currentName)
        {
            List<string> keys = m_SkinDefinitionByName.Keys.ToList();
            if (keys.Count == 0) return null;
            int idx = keys.IndexOf(currentName);
            int prevIdx = (idx - 1 + keys.Count) % keys.Count;
            return keys[prevIdx];
        }

        public SkinDefinition GetByName(string skinName)
        {
            m_SkinDefinitionByName.TryGetValue(skinName, out SkinDefinition skin);
            return skin;
        }

        public string GetAnimationSetNameForModelName(string modelName)
        {
            //TODO: anatoli - improve this mapping, maybe also load it from a config file or scriptable object to avoid hardcoding?
            var animationSetMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"average_sleeves", "average_sleeves" },
                { "chem_suit", "average_sleeves" },
                { "suit_long_coat", "average_sleeves" },
                {"suit_sleeves", "average_sleeves" },
                {"fat", "average_sleeves" },
                {"snow", "average_sleeves" },
                {"average_armor", "average_sleeves" },

                {"female_pants", "female_pants" },
                { "female_skirt", "female_pants" },
                { "female_armor", "female_pants" },

                //TODO: anatoli - missing animation sets for these models. 
                //{ "dog", "dog" }, // TODO: blender fix animation skeleton and then add back to mapping
                //{ "osprey", "osprey" },
            };

            animationSetMapping.TryGetValue(modelName, out string animationSetName);
            return animationSetName;
        }

        public List<SkinDefinition> GetSkinsForModel(string modelType)
        {
            if (m_SkinDefinitionsByModelName.TryGetValue(modelType, out List<SkinDefinition> skins))
            {
                return new List<SkinDefinition>(skins);
            }
            return new List<SkinDefinition>();
        }

        public string[] GetAvailableModels()
        {
            return m_SkinDefinitionsByModelName.Keys.ToArray();
        }

        public bool HasSkin(string skinName)
        {
            return m_SkinDefinitionByName.ContainsKey(skinName);
        }

        public int TotalSkinCount => m_SkinDefinitionByName.Count;

        public int GetSkinCountForModel(string modelType)
        {
            return m_SkinDefinitionsByModelName.TryGetValue(modelType, out List<SkinDefinition> skins) ? skins.Count : 0;
        }

        public void ClearCache()
        {
            m_SkinDefinitionsByModelName.Clear();
            m_SkinDefinitionByName.Clear();
            Debug.Log("[SkinDefinitionLoader] Cache cleared.");
        }
    }
}
