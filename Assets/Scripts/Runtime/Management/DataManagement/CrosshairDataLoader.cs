using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.CrosshairManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service fuer das Laden und Parsen von Crosshair-Definitionen aus SoF2_Crosshair.json.
    /// Stellt statische Referenzdaten bereit. Zugreifbar ueber ServiceLocator.
    /// </summary>
    public class CrosshairDataLoader
    {
        private const string DATA_PATH = "Data/SoF2_Crosshair.json";

        private readonly Dictionary<string, CrosshairDefinition> m_CrosshairsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<CrosshairDefinition> m_AllCrosshairs = new();

        /// <summary>
        /// Initialisiert den Loader und laedt alle Crosshairs automatisch.
        /// </summary>
        public CrosshairDataLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Laedt alle Crosshair-Definitionen aus der JSON-Datei in Resources.
        /// </summary>
        private void LoadFromResources()
        {
            m_CrosshairsById.Clear();
            m_AllCrosshairs.Clear();

            string filePath = Path.Combine(Application.streamingAssetsPath, DATA_PATH);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[CrosshairDataLoader] Could not load crosshair file at {filePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<CrosshairDefinition> crosshairs = JsonConvert.DeserializeObject<List<CrosshairDefinition>>(json);

                if (crosshairs == null)
                {
                    Debug.LogWarning("[CrosshairDataLoader] Crosshairs array is null or empty");
                    return;
                }

                foreach (CrosshairDefinition crosshair in crosshairs)
                {
                    if (string.IsNullOrEmpty(crosshair.Id))
                    {
                        continue;
                    }

                    m_CrosshairsById[crosshair.Id] = crosshair;
                    m_AllCrosshairs.Add(crosshair);
                }

                Debug.Log($"[CrosshairDataLoader] Loaded {m_CrosshairsById.Count} crosshairs from {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CrosshairDataLoader] Error parsing crosshair file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gibt die Crosshair-Definition anhand der ID zurueck (z.B. "ch1").
        /// </summary>
        public CrosshairDefinition GetById(string crosshairId)
        {
            m_CrosshairsById.TryGetValue(crosshairId, out CrosshairDefinition crosshair);
            return crosshair;
        }

        /// <summary>
        /// Gibt das erste (Default) Crosshair zurueck, oder null wenn keine geladen.
        /// </summary>
        public CrosshairDefinition GetDefault()
        {
            return m_AllCrosshairs.Count > 0 ? m_AllCrosshairs[0] : null;
        }

        /// <summary>
        /// Gibt alle geladenen Crosshair-Definitionen zurueck.
        /// </summary>
        public IReadOnlyList<CrosshairDefinition> GetAll()
        {
            return m_AllCrosshairs;
        }

        /// <summary>
        /// Gibt die Anzahl geladener Crosshairs zurueck.
        /// </summary>
        public int CrosshairCount => m_CrosshairsById.Count;

        /// <summary>
        /// Leert den Cache. Wird von ServiceLocator.ClearAll() aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            m_CrosshairsById.Clear();
            m_AllCrosshairs.Clear();
            Debug.Log("[CrosshairDataLoader] Cache cleared.");
        }
    }
}
