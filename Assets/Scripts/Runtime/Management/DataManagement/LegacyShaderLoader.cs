using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service für das Laden und Parsen von Legacy SoF2 .shader Dateien.
    /// Zugreifbar über ServiceLocator.
    /// </summary>
    public class LegacyShaderLoader
    {
        private readonly Dictionary<string, Dictionary<string, ShaderEntry>> m_LegacyShaderEntriesByModel = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und lädt alle Shader-Definitionen automatisch aus Data/shaders/
        /// </summary>
        public LegacyShaderLoader()
        {
            LoadAllFromResources();
        }

        /// <summary>
        /// Lädt alle .shader Dateien aus dem Data/shaders/ Ordner
        /// </summary>
        private void LoadAllFromResources(string resourcePath = "Data/shaders")
        {
            m_LegacyShaderEntriesByModel.Clear();

            string fullPath = Path.Combine(Application.dataPath, "Resources", resourcePath);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[LegacyShaderLoader] Shader directory not found at: {fullPath}");
                return;
            }

            string[] shaderFiles = Directory.GetFiles(fullPath, "*.shader", SearchOption.TopDirectoryOnly);
            
            foreach (string shaderFilePath in shaderFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(shaderFilePath);
                Dictionary<string, ShaderEntry> entries = LoadLegacyShaderEntriesForModel(shaderFilePath);
                if (entries != null)
                {
                    m_LegacyShaderEntriesByModel[fileName] = entries;
                }else{
                    //warn shaders exists
                    Debug.LogWarning($"[LegacyShaderLoader] Failed to load shader entries for model '{fileName}' from file: {shaderFilePath}");
                }
            }

            Debug.Log($"[LegacyShaderLoader] Loaded {m_LegacyShaderEntriesByModel.Count} legacy shader definitions: {string.Join(", ", m_LegacyShaderEntriesByModel.Keys)}");
        }

        /// <summary>
        /// Lädt eine einzelne .shader Datei und parsed die Einträge
        /// </summary>
        private Dictionary<string, ShaderEntry> LoadLegacyShaderEntriesForModel(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[LegacyShaderLoader] Shader definition file not found at: {fullPath}");
                return null;
            }

            try
            {
                string shaderText = File.ReadAllText(fullPath);
                if (string.IsNullOrEmpty(shaderText))
                {
                    Debug.LogError($"[LegacyShaderLoader] Shader definition file is empty or invalid at: {fullPath}");
                    return null;
                }

                return ShaderDataReader.ParseShaderEntries(shaderText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LegacyShaderLoader] Error reading/parsing shader file: {ex}");
                return null;
            }
        }

        public Dictionary<string, ShaderEntry> GetByModelName(string modelName)
        {
            m_LegacyShaderEntriesByModel.TryGetValue(modelName, out Dictionary<string, ShaderEntry> shaderEntries);
            return shaderEntries;
        }

        /// <summary>
        /// Gibt alle geladenen Shader-Definitionen zurück
        /// </summary>
        public Dictionary<string, Dictionary<string, ShaderEntry>> GetAll()
        {
            return m_LegacyShaderEntriesByModel;
        }

        public void ClearCache()
        {
            m_LegacyShaderEntriesByModel.Clear();
            Debug.Log("[LegacyShaderLoader] Cache cleared.");
        }
    }
}
