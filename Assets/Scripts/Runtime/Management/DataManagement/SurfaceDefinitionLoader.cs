using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service für das Laden und Parsen von Surface-Definitionen aus Resources.
    /// Zugreifbar über ServiceLocator.
    /// </summary>
    public class SurfaceDefinitionLoader
    {
        private Dictionary<string, SkinSurfaceDefinition> m_SkinSurfaceDefinitionsByModel = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und lädt alle Surface-Daten automatisch
        /// </summary>
        public SurfaceDefinitionLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Loads and parses NPC_definition.json from Resources/Data.
        /// </summary>
        private void LoadFromResources(string resourcePath = "Data/NPC_definition")
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[SurfaceDefinitionLoader] Surface definition not found at Resources/{resourcePath}.json");
                return;
            }

            ParseSurfaceDefinition(asset.text);
        }

        /// <summary>
        /// Parses the surface definition JSON (variant -> parts mapping).
        /// </summary>
        private void ParseSurfaceDefinition(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SurfaceDefinitionLoader] Surface definition json is empty");
                return;
            }

            try
            {
                Dictionary<string, SkinSurfaceDefinition> parsed = JsonConvert.DeserializeObject<Dictionary<string, SkinSurfaceDefinition>>(json);
                if (parsed == null)
                {
                    Debug.LogWarning("[SurfaceDefinitionLoader] Surface definition parsed to null");
                    m_SkinSurfaceDefinitionsByModel.Clear();
                    return;
                }

                m_SkinSurfaceDefinitionsByModel = new Dictionary<string, SkinSurfaceDefinition>(parsed, StringComparer.OrdinalIgnoreCase);
                Debug.Log($"[SurfaceDefinitionLoader] Surface definitions parsed. Variants: {m_SkinSurfaceDefinitionsByModel.Count}. Keys: {string.Join(", ", m_SkinSurfaceDefinitionsByModel.Keys)}");
            }
            catch (Exception ex)
            {
                m_SkinSurfaceDefinitionsByModel.Clear();
                Debug.LogError($"[SurfaceDefinitionLoader] Error parsing surface definition: {ex}");
            }
        }

        public SkinSurfaceDefinition GetByModelName(string modelName)
        {
            m_SkinSurfaceDefinitionsByModel.TryGetValue(modelName, out SkinSurfaceDefinition surfaceDefinition);
            return surfaceDefinition;
        }

        public void ClearCache()
        {
            m_SkinSurfaceDefinitionsByModel.Clear();
            Debug.Log("[SurfaceDefinitionLoader] Cache cleared.");
        }
    }
}
