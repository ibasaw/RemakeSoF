using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.EffectManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service fuer das Laden und Parsen von Effektdefinitionen aus JSON-Dateien.
    /// Laedt alle JSON-Dateien im Effects/-Unterordner (Impact, Debris, Explosions, Muzzle, etc.).
    /// Stellt Effektdaten anhand ihrer ID bereit.
    /// Zugreifbar ueber ServiceLocator.
    /// </summary>
    public class EffectDataLoader
    {
        private const string EFFECTS_FOLDER = "Data/Effects";

        private readonly Dictionary<string, EffectDefinition> m_EffectsById = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und laedt alle Effekte automatisch.
        /// </summary>
        public EffectDataLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Laedt alle Effektdefinitionen aus dem Effects/-Unterordner.
        /// </summary>
        private void LoadFromResources()
        {
            m_EffectsById.Clear();

            string folderPath = Path.Combine(Application.streamingAssetsPath, EFFECTS_FOLDER);

            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"[EffectDataLoader] Effects folder not found at {folderPath}");
                return;
            }

            string[] effectFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string effectFilePath in effectFiles)
            {
                LoadJsonFile(effectFilePath);
            }

            Debug.Log($"[EffectDataLoader] Loaded {m_EffectsById.Count} effects total");
        }

        /// <summary>
        /// Parst eine JSON-Datei als Array von EffectDefinitions und registriert sie.
        /// </summary>
        private void LoadJsonFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                List<EffectDefinition> effects = JsonConvert.DeserializeObject<List<EffectDefinition>>(json);

                if (effects == null)
                {
                    return;
                }

                foreach (EffectDefinition effect in effects)
                {
                    if (string.IsNullOrEmpty(effect.Id))
                    {
                        continue;
                    }

                    m_EffectsById[effect.Id] = effect;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EffectDataLoader] Error parsing {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gibt die Effektdefinition anhand der ID zurueck (z.B. "effects/tracerTest2").
        /// </summary>
        public EffectDefinition GetById(string effectId)
        {
            m_EffectsById.TryGetValue(effectId, out EffectDefinition effect);
            return effect;
        }

        /// <summary>
        /// Gibt alle geladenen Effektdefinitionen zurueck.
        /// </summary>
        public IReadOnlyCollection<EffectDefinition> GetAll()
        {
            return m_EffectsById.Values;
        }

        /// <summary>
        /// Leert den internen Cache. Wird von ServiceLocator.ClearAll() per Reflection aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            m_EffectsById.Clear();
        }
    }
}
