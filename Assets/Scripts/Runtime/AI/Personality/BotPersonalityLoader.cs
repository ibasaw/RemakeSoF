using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.Personality
{
    /// <summary>
    /// Laedt Bot-Persoenlichkeitsprofile aus StreamingAssets/Data/SoF2_BotPersonalities.json.
    /// Pure Service, registriert im ServiceLocator (Server-only).
    /// Stellt gewichtete Zufallsauswahl per <see cref="PickRandom"/> bereit.
    /// </summary>
    public class BotPersonalityLoader
    {
        /// <summary>Pfad zur JSON-Datei relativ zu StreamingAssets/.</summary>
        const string k_DataPath = "Data/SoF2_BotPersonalities.json";

        /// <summary>Geladene Profile (immutable nach Konstruktor).</summary>
        readonly List<BotPersonality> m_Profiles = new();

        /// <summary>Summe aller Spawn-Gewichte (cached fuer PickRandom).</summary>
        float m_TotalWeight;

        /// <summary>Anzahl geladener Profile.</summary>
        public int ProfileCount => m_Profiles.Count;

        /// <summary>
        /// Konstruktor: Laedt Profile aus der JSON-Datei und cached Summe der Gewichte.
        /// </summary>
        public BotPersonalityLoader()
        {
            LoadFromStreamingAssets();
        }

        /// <summary>
        /// Liefert ein Profil per Spawn-Gewicht-gewichteter Zufallsauswahl.
        /// Bei leerer Liste oder allen Gewichten == 0 wird ein Default-Profil zurueckgegeben.
        /// </summary>
        public BotPersonality PickRandom()
        {
            if (m_Profiles.Count == 0 || m_TotalWeight <= 0f)
            {
                return new BotPersonality();
            }

            float roll = Random.value * m_TotalWeight;
            float acc = 0f;
            for (int i = 0; i < m_Profiles.Count; i++)
            {
                acc += Mathf.Max(0f, m_Profiles[i].weight);
                if (roll <= acc)
                {
                    return m_Profiles[i];
                }
            }

            return m_Profiles[m_Profiles.Count - 1];
        }

        /// <summary>
        /// Liefert ein Profil nach Name (Case-insensitive). Null wenn nicht gefunden.
        /// </summary>
        public BotPersonality GetByName(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                return null;
            }

            for (int i = 0; i < m_Profiles.Count; i++)
            {
                if (string.Equals(m_Profiles[i].name, profileName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return m_Profiles[i];
                }
            }

            return null;
        }

        void LoadFromStreamingAssets()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, k_DataPath);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[BotPersonalityLoader] Datei nicht gefunden: {filePath} — Default-Profil aktiv.");
                return;
            }

            string json = File.ReadAllText(filePath);
            BotPersonalityCollection root = JsonUtility.FromJson<BotPersonalityCollection>(json);
            if (root?.profiles == null || root.profiles.Count == 0)
            {
                Debug.LogWarning("[BotPersonalityLoader] Keine Profile in JSON gefunden — Default-Profil aktiv.");
                return;
            }

            m_TotalWeight = 0f;
            for (int i = 0; i < root.profiles.Count; i++)
            {
                BotPersonality p = root.profiles[i];
                if (p == null)
                {
                    continue;
                }

                m_Profiles.Add(p);
                m_TotalWeight += Mathf.Max(0f, p.weight);
            }

            Debug.Log($"[BotPersonalityLoader] {m_Profiles.Count} Profile geladen, TotalWeight={m_TotalWeight:F2}");
        }
    }
}
