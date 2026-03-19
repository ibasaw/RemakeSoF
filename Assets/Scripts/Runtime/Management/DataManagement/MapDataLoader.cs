using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Laedt Map-Definitionen aus Resources/Data/SoF2_Maps.json.
    /// Stellt Zugriff auf Spawn-Points, Countdown-Werte und Map-Metadaten bereit.
    /// Pure Service, registriert im ServiceLocator.
    /// </summary>
    public class MapDataLoader
    {
        /// <summary>Pfad zur JSON-Datei relativ zu Resources/.</summary>
        private const string k_ResourcePath = "Data/SoF2_Maps";

        /// <summary>Lookup: Map-ID → MapDefinition.</summary>
        private readonly Dictionary<string, MapDefinition> m_Maps = new();

        /// <summary>Geordnete Liste aller Map-IDs fuer Rotation.</summary>
        private readonly List<string> m_MapOrder = new();

        /// <summary>
        /// Konstruktor: Laedt alle Map-Definitionen aus der JSON-Datei.
        /// </summary>
        public MapDataLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Gibt die MapDefinition fuer eine Map-ID zurueck.
        /// </summary>
        /// <param name="mapId">Die Map-ID (z.B. "maps/cem1").</param>
        /// <returns>Die MapDefinition oder null wenn nicht gefunden.</returns>
        public MapDefinition GetByMapId(string mapId)
        {
            m_Maps.TryGetValue(mapId, out MapDefinition definition);
            return definition;
        }

        /// <summary>
        /// Gibt die naechste Map-ID in der Rotation zurueck.
        /// Nach der letzten Map kommt die erste (Round-Robin).
        /// </summary>
        /// <param name="currentMapId">Die aktuelle Map-ID.</param>
        /// <returns>Die naechste Map-ID oder die erste falls currentMapId nicht gefunden.</returns>
        public string GetNextMapId(string currentMapId)
        {
            if (m_MapOrder.Count == 0)
            {
                return currentMapId;
            }

            int currentIndex = m_MapOrder.IndexOf(currentMapId);
            int nextIndex = (currentIndex + 1) % m_MapOrder.Count;
            return m_MapOrder[nextIndex];
        }

        /// <summary>
        /// Gibt die Spawn-Points fuer beide Teams einer Map zurueck.
        /// </summary>
        /// <param name="mapId">Die Map-ID.</param>
        /// <returns>Zwei Listen mit Vector3-Positionen fuer Team 1 und Team 2.</returns>
        public (List<Vector3> teamOne, List<Vector3> teamTwo) GetSpawnPoints(string mapId)
        {
            MapDefinition definition = GetByMapId(mapId);
            if (definition == null)
            {
                Debug.LogWarning($"[MapDataLoader] Keine Map-Definition fuer '{mapId}' gefunden!");
                return (new(), new());
            }

            List<Vector3> teamOne = ConvertSpawnPoints(definition.team1SpawnPoints);
            List<Vector3> teamTwo = ConvertSpawnPoints(definition.team2SpawnPoints);

            return (teamOne, teamTwo);
        }

        /// <summary>
        /// Konvertiert ein Array von SpawnPointData in eine Liste von Vector3.
        /// </summary>
        private List<Vector3> ConvertSpawnPoints(SpawnPointData[] spawnPoints)
        {
            List<Vector3> result = new();
            if (spawnPoints == null)
            {
                return result;
            }

            foreach (SpawnPointData point in spawnPoints)
            {
                result.Add(point.ToVector3());
            }

            return result;
        }

        /// <summary>
        /// Laedt die JSON-Datei aus Resources und baut das Lookup-Dictionary.
        /// Unterstuetzt sowohl einzelne Map-Objekte als auch Arrays.
        /// </summary>
        private void LoadFromResources()
        {
            TextAsset textAsset = Resources.Load<TextAsset>(k_ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"[MapDataLoader] Konnte '{k_ResourcePath}' nicht aus Resources laden!");
                return;
            }

            string json = textAsset.text.Trim();

            // JSON kann ein einzelnes Objekt oder ein Array sein
            if (json.StartsWith("["))
            {
                // Array: Wrapper-Klasse fuer JsonUtility (unterstuetzt kein Top-Level-Array)
                MapDefinition[] maps = JsonHelper.FromJsonArray<MapDefinition>(json);
                foreach (MapDefinition map in maps)
                {
                    RegisterMap(map);
                }
            }
            else
            {
                // Einzelnes Objekt
                MapDefinition map = JsonUtility.FromJson<MapDefinition>(json);
                RegisterMap(map);
            }

            Debug.Log($"[MapDataLoader] {m_Maps.Count} Map-Definition(en) geladen.");
        }

        /// <summary>
        /// Registriert eine MapDefinition im Lookup-Dictionary.
        /// </summary>
        private void RegisterMap(MapDefinition map)
        {
            if (map == null || string.IsNullOrEmpty(map.id))
            {
                Debug.LogWarning("[MapDataLoader] Ungueltige Map-Definition uebersprungen.");
                return;
            }

            m_Maps[map.id] = map;
            if (!m_MapOrder.Contains(map.id))
            {
                m_MapOrder.Add(map.id);
            }
        }

        /// <summary>
        /// Helper fuer JsonUtility Top-Level-Array-Deserialisierung.
        /// JsonUtility unterstuetzt kein direktes Array-Parsing, daher Wrapper.
        /// </summary>
        private static class JsonHelper
        {
            /// <summary>
            /// Deserialisiert ein JSON-Array in ein C#-Array.
            /// </summary>
            public static T[] FromJsonArray<T>(string json)
            {
                // Wrapping: {"items": [...]} damit JsonUtility es parsen kann
                string wrappedJson = "{\"items\":" + json + "}";
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
                return wrapper.items;
            }

            [System.Serializable]
            private class Wrapper<T>
            {
                public T[] items;
            }
        }
    }
}
