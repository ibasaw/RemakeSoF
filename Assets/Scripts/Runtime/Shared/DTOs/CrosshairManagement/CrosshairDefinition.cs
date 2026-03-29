using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.CrosshairManagement
{
    /// <summary>
    /// Definition eines dynamisch zusammengebauten Crosshairs aus SoF2_Crosshair.json.
    /// Beschreibt Form, Groesse, Farbe und optionale Outline fuer ein Fadenkreuz.
    /// </summary>
    [Serializable]
    public class CrosshairDefinition
    {
        /// <summary>
        /// Eindeutige ID des Crosshairs (z.B. "ch1", "ch2").
        /// </summary>
        [JsonProperty("id")]
        public string Id;

        /// <summary>
        /// Anzeigename des Crosshairs (z.B. "Default Cross").
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName;

        /// <summary>
        /// Laenge jeder Linie in Pixeln (von Gap-Ende bis Linien-Ende).
        /// </summary>
        [JsonProperty("lineLength")]
        public int LineLength;

        /// <summary>
        /// Breite/Dicke jeder Linie in Pixeln.
        /// </summary>
        [JsonProperty("lineThickness")]
        public int LineThickness;

        /// <summary>
        /// Abstand vom Zentrum bis zum Beginn der Linie in Pixeln.
        /// </summary>
        [JsonProperty("gap")]
        public int Gap;

        /// <summary>
        /// Ob ein zentraler Punkt angezeigt werden soll.
        /// </summary>
        [JsonProperty("centerDot")]
        public bool CenterDot;

        /// <summary>
        /// Groesse des zentralen Punkts in Pixeln.
        /// </summary>
        [JsonProperty("centerDotSize")]
        public int CenterDotSize;

        /// <summary>
        /// Farbe als RGBA-Array [R, G, B, A] (0-255).
        /// </summary>
        [JsonProperty("color")]
        public List<int> Color;

        /// <summary>
        /// Dicke der Outline in Pixeln (0 = keine Outline).
        /// </summary>
        [JsonProperty("outlineThickness")]
        public int OutlineThickness;

        /// <summary>
        /// Outline-Farbe als RGBA-Array [R, G, B, A] (0-255).
        /// </summary>
        [JsonProperty("outlineColor")]
        public List<int> OutlineColor;
    }
}
