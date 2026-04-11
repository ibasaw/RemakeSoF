using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Laedt Gametype-Definitionen aus Resources/Data/SoF2_Gametypes.json.
    /// Pure Service, registriert im ServiceLocator.
    /// </summary>
    public class GametypeDefinitionLoader
    {
        /// <summary>Pfad zur JSON-Datei relativ zu StreamingAssets/.</summary>
        const string k_DataPath = "Data/SoF2_Gametypes.json";

        /// <summary>Lookup: Gametype-ID → GametypeDefinition.</summary>
        readonly Dictionary<string, GametypeDefinition> m_Gametypes = new();

        /// <summary>Geordnete Liste aller Gametype-IDs.</summary>
        readonly List<string> m_GametypeOrder = new();

        /// <summary>
        /// Konstruktor: Laedt alle Gametype-Definitionen.
        /// </summary>
        public GametypeDefinitionLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Gibt die GametypeDefinition fuer einen Gametype-Identifier zurueck.
        /// </summary>
        /// <param name="gametypeId">Der Gametype-Identifier (z.B. "tdm", "hideandseek").</param>
        /// <returns>Die GametypeDefinition oder null wenn nicht gefunden.</returns>
        public GametypeDefinition GetByGametypeId(string gametypeId)
        {
            m_Gametypes.TryGetValue(gametypeId, out GametypeDefinition definition);
            return definition;
        }

        /// <summary>
        /// Gibt alle verfuegbaren Gametype-IDs zurueck.
        /// </summary>
        public IReadOnlyList<string> GetAllGametypeIds() => m_GametypeOrder;

        /// <summary>
        /// Gibt alle geladenen Gametype-Definitionen zurueck.
        /// </summary>
        public IEnumerable<GametypeDefinition> GetAll() => m_Gametypes.Values;

        /// <summary>
        /// Laedt die Gametype-Definitionen aus Resources.
        /// </summary>
        void LoadFromResources()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, k_DataPath);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[GametypeDefinitionLoader] Gametypes nicht gefunden: {filePath}");
                return;
            }

            string text = File.ReadAllText(filePath);

            // JsonUtility kann keine Top-Level-Arrays deserialisieren, daher Wrapper
            string wrappedJson = "{\"items\":" + text + "}";
            GametypeDefinitionArray wrapper = JsonUtility.FromJson<GametypeDefinitionArray>(wrappedJson);

            if (wrapper?.items == null)
            {
                Debug.LogError("[GametypeDefinitionLoader] Konnte Gametypes nicht parsen.");
                return;
            }

            foreach (GametypeDefinition definition in wrapper.items)
            {
                if (string.IsNullOrEmpty(definition.gametype))
                {
                    continue;
                }

                m_Gametypes[definition.gametype] = definition;
                m_GametypeOrder.Add(definition.gametype);
            }

            Debug.Log($"[GametypeDefinitionLoader] {m_Gametypes.Count} Gametypes geladen.");
        }

        /// <summary>Hilfsklasse fuer JsonUtility Top-Level-Array-Deserialisierung.</summary>
        [System.Serializable]
        class GametypeDefinitionArray
        {
            public GametypeDefinition[] items;
        }
    }
}
