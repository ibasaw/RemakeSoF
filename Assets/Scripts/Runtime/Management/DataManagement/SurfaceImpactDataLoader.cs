using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service fuer das Laden der SoF2 Surface-Impact-Zuordnungen aus SoF2_data_per_surface.json.
    /// Ermittelt anhand von Surface-Typ und Munitionstyp die korrekte Impact-Effect-ID.
    /// Lookup-Kette: SurfaceType + AmmoType → EffectId (z.B. "concrete" + "5.56mm" → "effects/impact_concrete_rifle").
    /// Zugreifbar ueber ServiceLocator.
    /// </summary>
    public class SurfaceImpactDataLoader
    {
        private const string RESOURCE_PATH = "Data/SoF2_data_per_surface";
        private const string DEFAULT_SURFACE = "default";
        private const string DEFAULT_EFFECT = "effects/impact_default";

        /// <summary>
        /// Surface → AmmoType → EffectId Mapping.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceAmmoEffects =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialisiert den Loader und laedt die Surface-Impact-Daten automatisch.
        /// </summary>
        public SurfaceImpactDataLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Laedt und parst SoF2_data_per_surface.json aus Resources.
        /// Extrahiert nur die ammoTypes.effect-Zuordnungen.
        /// </summary>
        private void LoadFromResources()
        {
            TextAsset asset = Resources.Load<TextAsset>(RESOURCE_PATH);
            if (asset == null)
            {
                Debug.LogWarning($"[SurfaceImpactDataLoader] Could not load {RESOURCE_PATH}");
                return;
            }

            try
            {
                JObject root = JObject.Parse(asset.text);

                foreach (KeyValuePair<string, JToken> surfaceEntry in root)
                {
                    string surfaceName = surfaceEntry.Key;
                    JToken ammoTypesToken = surfaceEntry.Value["ammoTypes"];

                    if (ammoTypesToken == null || ammoTypesToken.Type != JTokenType.Object)
                    {
                        continue;
                    }

                    Dictionary<string, string> ammoEffects = new(StringComparer.OrdinalIgnoreCase);

                    foreach (KeyValuePair<string, JToken> ammoEntry in (JObject)ammoTypesToken)
                    {
                        string effectId = ammoEntry.Value["effect"]?.ToString();

                        if (!string.IsNullOrEmpty(effectId) && !effectId.Contains(" "))
                        {
                            ammoEffects[ammoEntry.Key] = effectId;
                        }
                    }

                    if (ammoEffects.Count > 0)
                    {
                        m_SurfaceAmmoEffects[surfaceName] = ammoEffects;
                    }
                }

                Debug.Log($"[SurfaceImpactDataLoader] Loaded {m_SurfaceAmmoEffects.Count} surface types");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurfaceImpactDataLoader] Error parsing: {ex.Message}");
            }
        }

        /// <summary>
        /// Ermittelt die Impact-Effect-ID fuer einen gegebenen Surface-Typ und Munitionstyp.
        /// Fallback-Kette: ExacterSurface → "default" Surface → DEFAULT_EFFECT.
        /// </summary>
        public string GetImpactEffectId(string surfaceType, string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
            {
                return DEFAULT_EFFECT;
            }

            // Versuch 1: Exakte Surface + AmmoType
            if (!string.IsNullOrEmpty(surfaceType)
                && m_SurfaceAmmoEffects.TryGetValue(surfaceType, out Dictionary<string, string> ammoEffects)
                && ammoEffects.TryGetValue(ammoType, out string effectId))
            {
                return effectId;
            }

            // Versuch 2: Default-Surface + AmmoType
            if (m_SurfaceAmmoEffects.TryGetValue(DEFAULT_SURFACE, out Dictionary<string, string> defaultEffects)
                && defaultEffects.TryGetValue(ammoType, out string defaultEffectId))
            {
                return defaultEffectId;
            }

            return DEFAULT_EFFECT;
        }
    }
}
