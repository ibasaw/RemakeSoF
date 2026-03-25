using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// DTO fuer eine Gore-Effekt-Definition aus SoF2_DATA.json (gore_effects).
    /// Mappt einen logischen Effekt-Namen auf eine .efx-Datei.
    /// </summary>
    [Serializable]
    public class GoreEffectDefinition
    {
        /// <summary>
        /// Logischer Name des Effekts (z.B. "gore_mist_small", "blood_spurt_arterial").
        /// </summary>
        [JsonProperty("Name")]
        public string Name;

        /// <summary>
        /// Original .efx-Dateiname (z.B. "gore_mist_small.efx", "blood_spurt_arterial_mp.efx").
        /// Wird zur Aufloesung der konvertierten EffectDefinition-ID verwendet.
        /// </summary>
        [JsonProperty("File")]
        public string File;
    }
}
