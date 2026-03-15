using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.EffectManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service fuer das Laden und Parsen von Effektdefinitionen aus JSON-Dateien.
    /// Laedt SoF2_Effects.json (Tracer/Trail/Explosions) und alle Dateien im Effects/-Unterordner
    /// (Impact-Effekte pro Surface-Typ). Stellt Effektdaten anhand ihrer ID bereit.
    /// Zugreifbar ueber ServiceLocator.
    /// </summary>
    public class EffectDataLoader
    {
        private const string RESOURCE_PATH = "Data/SoF2_Effects";
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
        /// Laedt alle Effektdefinitionen: Haupt-JSON + alle Dateien im Effects/-Unterordner.
        /// </summary>
        private void LoadFromResources()
        {
            m_EffectsById.Clear();

            // Haupt-Datei laden (Tracer, Trails, Explosions)
            LoadJsonFile(RESOURCE_PATH);

            // Alle JSON-Dateien im Effects/-Unterordner laden (Impact-Effekte)
            TextAsset[] effectFiles = Resources.LoadAll<TextAsset>(EFFECTS_FOLDER);
            foreach (TextAsset file in effectFiles)
            {
                LoadJsonAsset(file);
            }

            Debug.Log($"[EffectDataLoader] Loaded {m_EffectsById.Count} effects total");
        }

        /// <summary>
        /// Laedt eine einzelne JSON-Datei aus Resources anhand des Pfads.
        /// </summary>
        private void LoadJsonFile(string resourcePath)
        {
            TextAsset file = Resources.Load<TextAsset>(resourcePath);
            if (file == null)
            {
                Debug.LogWarning($"[EffectDataLoader] Could not load {resourcePath}");
                return;
            }

            LoadJsonAsset(file);
        }

        /// <summary>
        /// Parst ein TextAsset als JSON-Array von EffectDefinitions und registriert sie.
        /// </summary>
        private void LoadJsonAsset(TextAsset asset)
        {
            try
            {
                List<EffectDefinition> effects = JsonConvert.DeserializeObject<List<EffectDefinition>>(asset.text);

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
                Debug.LogError($"[EffectDataLoader] Error parsing {asset.name}: {ex.Message}");
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
    }
}
