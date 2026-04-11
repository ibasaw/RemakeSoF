using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service fuer das Laden der SoF2 Surface-Zuordnungen aus SoF2_data_per_surface.json.
    /// Ermittelt anhand von Surface-Typ und Munitionstyp die korrekte Impact-Effect-ID,
    /// sowie Footstep-, Landing- und Shell-Casing-Sound-Pfade.
    /// Lookup-Kette: SurfaceType + AmmoType → EffectId (z.B. "concrete" + "5.56mm" → "effects/impact_concrete_rifle").
    /// Zugreifbar ueber ServiceLocator.
    /// </summary>
    public class SurfaceImpactDataLoader
    {
        private const string DATA_PATH = "Data/SoF2_data_per_surface.json";
        private const string DEFAULT_SURFACE = "default";
        private const string DEFAULT_EFFECT = "effects/impact_default";

        /// <summary>
        /// Surface → AmmoType → EffectId Mapping.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceAmmoEffects =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Surface → AmmoType → DebrisEffectId Mapping.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceAmmoDebris =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Surface → AmmoType → Shellsound-Basispfad Mapping (z.B. "sound/player/bullet_impacts/casings/casing_default").
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceAmmoShellsounds =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Surface → AmmoType → Impact-Sound-Basispfad Mapping (z.B. "sound/player/bullet_impacts/pistol/default").
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceAmmoImpactSounds =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Surface → Sound-Feld → Basispfad Mapping (footstep, footstepStealth, footstepProne, land, land_pain, land_death).
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceSounds =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Surface → Sound-Feld → Decal-Textur-Pfad Mapping (footstep, footstepStealth).
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> m_SurfaceDecals =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Sound-Felder die aus dem JSON geparst werden.</summary>
        private static readonly string[] s_SoundFields =
        {
            "footstep", "footstepStealth", "footstepProne",
            "land", "land_pain", "land_death"
        };

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
            string filePath = Path.Combine(Application.streamingAssetsPath, DATA_PATH);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SurfaceImpactDataLoader] Could not load {filePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                JObject root = JObject.Parse(json);

                foreach (KeyValuePair<string, JToken> surfaceEntry in root)
                {
                    string surfaceName = surfaceEntry.Key;
                    JToken surfaceData = surfaceEntry.Value;

                    // Sound-Felder parsen (footstep, land, etc.)
                    Dictionary<string, string> sounds = new(StringComparer.OrdinalIgnoreCase);
                    Dictionary<string, string> decals = new(StringComparer.OrdinalIgnoreCase);
                    foreach (string field in s_SoundFields)
                    {
                        string soundPath = surfaceData[field]?["sound"]?.ToString();
                        if (!string.IsNullOrEmpty(soundPath))
                        {
                            sounds[field] = soundPath;
                        }

                        string decalPath = surfaceData[field]?["decal"]?.ToString();
                        if (!string.IsNullOrEmpty(decalPath))
                        {
                            decals[field] = decalPath;
                        }
                    }

                    if (sounds.Count > 0)
                    {
                        m_SurfaceSounds[surfaceName] = sounds;
                    }

                    if (decals.Count > 0)
                    {
                        m_SurfaceDecals[surfaceName] = decals;
                    }

                    // AmmoTypes parsen (effect, debris, shellsound)
                    JToken ammoTypesToken = surfaceData["ammoTypes"];

                    if (ammoTypesToken == null || ammoTypesToken.Type != JTokenType.Object)
                    {
                        continue;
                    }

                    Dictionary<string, string> ammoEffects = new(StringComparer.OrdinalIgnoreCase);
                    Dictionary<string, string> ammoDebris = new(StringComparer.OrdinalIgnoreCase);
                    Dictionary<string, string> ammoShellsounds = new(StringComparer.OrdinalIgnoreCase);
                    Dictionary<string, string> ammoImpactSounds = new(StringComparer.OrdinalIgnoreCase);

                    foreach (KeyValuePair<string, JToken> ammoEntry in (JObject)ammoTypesToken)
                    {
                        string effectId = ammoEntry.Value["effect"]?.ToString();
                        string debrisId = ammoEntry.Value["debris"]?.ToString();
                        string shellsound = ammoEntry.Value["shellsound"]?.ToString();
                        string impactSound = ammoEntry.Value["sound"]?.ToString();

                        if (!string.IsNullOrEmpty(effectId) && !effectId.Contains(" "))
                        {
                            ammoEffects[ammoEntry.Key] = effectId;
                        }

                        if (!string.IsNullOrEmpty(debrisId) && !debrisId.Contains(" "))
                        {
                            ammoDebris[ammoEntry.Key] = debrisId;
                        }

                        if (!string.IsNullOrEmpty(shellsound))
                        {
                            ammoShellsounds[ammoEntry.Key] = shellsound;
                        }

                        if (!string.IsNullOrEmpty(impactSound))
                        {
                            ammoImpactSounds[ammoEntry.Key] = impactSound;
                        }
                    }

                    if (ammoEffects.Count > 0)
                    {
                        m_SurfaceAmmoEffects[surfaceName] = ammoEffects;
                    }

                    if (ammoDebris.Count > 0)
                    {
                        m_SurfaceAmmoDebris[surfaceName] = ammoDebris;
                    }

                    if (ammoShellsounds.Count > 0)
                    {
                        m_SurfaceAmmoShellsounds[surfaceName] = ammoShellsounds;
                    }

                    if (ammoImpactSounds.Count > 0)
                    {
                        m_SurfaceAmmoImpactSounds[surfaceName] = ammoImpactSounds;
                    }
                }

                Debug.Log($"[SurfaceImpactDataLoader] Loaded {m_SurfaceAmmoEffects.Count} surface types, {m_SurfaceSounds.Count} with sounds");
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

        /// <summary>
        /// Ermittelt die Debris-Effect-ID fuer einen gegebenen Surface-Typ und Munitionstyp.
        /// Fallback-Kette: ExacterSurface → "default" Surface → leer (kein Debris).
        /// </summary>
        public string GetDebrisEffectId(string surfaceType, string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
            {
                return "";
            }

            // Versuch 1: Exakte Surface + AmmoType
            if (!string.IsNullOrEmpty(surfaceType)
                && m_SurfaceAmmoDebris.TryGetValue(surfaceType, out Dictionary<string, string> ammoDebris)
                && ammoDebris.TryGetValue(ammoType, out string debrisId))
            {
                return debrisId;
            }

            // Versuch 2: Default-Surface + AmmoType
            if (m_SurfaceAmmoDebris.TryGetValue(DEFAULT_SURFACE, out Dictionary<string, string> defaultDebris)
                && defaultDebris.TryGetValue(ammoType, out string defaultDebrisId))
            {
                return defaultDebrisId;
            }

            return "";
        }

        /// <summary>
        /// Ermittelt den Footstep-Sound-Basispfad fuer einen Surface-Typ.
        /// fieldName: "footstep" (Laufen), "footstepStealth" (Walk), "footstepProne" (Prone).
        /// Basispfad wird mit Nummer suffixiert (z.B. "sound/player/steps/concrete/concrete" → concrete0.wav, concrete1.wav).
        /// Fallback: default Surface.
        /// </summary>
        public string GetSurfaceSoundPath(string surfaceType, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return "";
            }

            // Versuch 1: Exakte Surface
            if (!string.IsNullOrEmpty(surfaceType)
                && m_SurfaceSounds.TryGetValue(surfaceType, out Dictionary<string, string> sounds)
                && sounds.TryGetValue(fieldName, out string path))
            {
                return path;
            }

            // Versuch 2: Default-Surface
            if (m_SurfaceSounds.TryGetValue(DEFAULT_SURFACE, out Dictionary<string, string> defaultSounds)
                && defaultSounds.TryGetValue(fieldName, out string defaultPath))
            {
                return defaultPath;
            }

            return "";
        }

        /// <summary>
        /// Ermittelt den Shellsound-Basispfad fuer einen Surface-Typ und Munitionstyp.
        /// Fallback: default Surface → leer.
        /// </summary>
        public string GetShellsoundPath(string surfaceType, string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
            {
                return "";
            }

            // Versuch 1: Exakte Surface + AmmoType
            if (!string.IsNullOrEmpty(surfaceType)
                && m_SurfaceAmmoShellsounds.TryGetValue(surfaceType, out Dictionary<string, string> ammoShell)
                && ammoShell.TryGetValue(ammoType, out string shellPath))
            {
                return shellPath;
            }

            // Versuch 2: Default-Surface + AmmoType
            if (m_SurfaceAmmoShellsounds.TryGetValue(DEFAULT_SURFACE, out Dictionary<string, string> defaultShell)
                && defaultShell.TryGetValue(ammoType, out string defaultShellPath))
            {
                return defaultShellPath;
            }

            return "";
        }

        /// <summary>
        /// Ermittelt den Impact-Sound-Basispfad fuer einen Surface-Typ und Munitionstyp.
        /// Fuer Munitionstypen mit dediziertem Aufschlag-Sound am Trefferpunkt (z.B. "blunt" → Pistolenschlag).
        /// Fallback: default Surface → leer (kein Impact-Sound).
        /// </summary>
        public string GetImpactSoundPath(string surfaceType, string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
            {
                return "";
            }

            // Versuch 1: Exakte Surface + AmmoType
            if (!string.IsNullOrEmpty(surfaceType)
                && m_SurfaceAmmoImpactSounds.TryGetValue(surfaceType, out Dictionary<string, string> ammoSounds)
                && ammoSounds.TryGetValue(ammoType, out string soundPath))
            {
                return soundPath;
            }

            // Versuch 2: Default-Surface + AmmoType
            if (m_SurfaceAmmoImpactSounds.TryGetValue(DEFAULT_SURFACE, out Dictionary<string, string> defaultSounds)
                && defaultSounds.TryGetValue(ammoType, out string defaultSoundPath))
            {
                return defaultSoundPath;
            }

            return "";
        }

        /// <summary>
        /// Ermittelt den Footstep-Decal-Texturpfad fuer einen Surface-Typ.
        /// fieldName: "footstep" (Laufen), "footstepStealth" (Walk).
        /// Fallback: default Surface → leer.
        /// </summary>
        public string GetSurfaceDecalPath(string surfaceType, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return "";
            }

            // Versuch 1: Exakte Surface
            if (!string.IsNullOrEmpty(surfaceType)
                && m_SurfaceDecals.TryGetValue(surfaceType, out Dictionary<string, string> decals)
                && decals.TryGetValue(fieldName, out string path))
            {
                return path;
            }

            // Versuch 2: Default-Surface
            if (m_SurfaceDecals.TryGetValue(DEFAULT_SURFACE, out Dictionary<string, string> defaultDecals)
                && defaultDecals.TryGetValue(fieldName, out string defaultPath))
            {
                return defaultPath;
            }

            return "";
        }

        /// <summary>
        /// Leert den internen Cache. Wird von ServiceLocator.ClearAll() per Reflection aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            m_SurfaceAmmoEffects.Clear();
            m_SurfaceAmmoDebris.Clear();
            m_SurfaceAmmoShellsounds.Clear();
            m_SurfaceAmmoImpactSounds.Clear();
            m_SurfaceSounds.Clear();
            m_SurfaceDecals.Clear();
        }
    }
}
