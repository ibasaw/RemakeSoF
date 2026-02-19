using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.GoreManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Loads and caches gore area and gore piece definitions from SoF2_DATA.json.
    /// </summary>
    public class GoreDataLoader
    {
        private readonly Dictionary<string, GoreArea> m_GoreAreasByLocation = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GorePiece> m_GorePiecesByName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes the loader and loads gore data from Resources/Data/SoF2_DATA.json.
        /// </summary>
        public GoreDataLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Returns the gore area for a given location name.
        /// </summary>
        public GoreArea GetAreaByLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            m_GoreAreasByLocation.TryGetValue(location, out GoreArea area);
            return area;
        }

        /// <summary>
        /// Returns the gore piece definition for a given piece name.
        /// </summary>
        public GorePiece GetPieceByName(string pieceName)
        {
            if (string.IsNullOrWhiteSpace(pieceName))
            {
                return null;
            }

            m_GorePiecesByName.TryGetValue(pieceName, out GorePiece piece);
            return piece;
        }

        /// <summary>
        /// Returns all loaded gore areas.
        /// </summary>
        public List<GoreArea> GetAllAreas()
        {
            return new List<GoreArea>(m_GoreAreasByLocation.Values);
        }

        /// <summary>
        /// Returns all loaded gore pieces.
        /// </summary>
        public List<GorePiece> GetAllPieces()
        {
            return new List<GorePiece>(m_GorePiecesByName.Values);
        }

        /// <summary>
        /// Returns the number of loaded gore areas.
        /// </summary>
        public int GoreAreaCount => m_GoreAreasByLocation.Count;

        /// <summary>
        /// Returns the number of loaded gore pieces.
        /// </summary>
        public int GorePieceCount => m_GorePiecesByName.Count;

        /// <summary>
        /// Clears cached gore data.
        /// </summary>
        public void ClearCache()
        {
            m_GoreAreasByLocation.Clear();
            m_GorePiecesByName.Clear();
            Debug.Log("[GoreDataLoader] Cache cleared.");
        }

        private void LoadFromResources(string fileNameWithoutExtension = "SoF2_DATA")
        {
            m_GoreAreasByLocation.Clear();
            m_GorePiecesByName.Clear();

            string json = JsonDataReader.TryLoadJsonText(fileNameWithoutExtension);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[GoreDataLoader] JSON file not found: {fileNameWithoutExtension}");
                return;
            }

            SoF2DataRoot dataRoot;
            try
            {
                dataRoot = JsonConvert.DeserializeObject<SoF2DataRoot>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoreDataLoader] Failed to parse {fileNameWithoutExtension}: {ex.Message}");
                return;
            }

            if (dataRoot?.Gore == null)
            {
                Debug.LogWarning($"[GoreDataLoader] Gore section missing in {fileNameWithoutExtension}");
                return;
            }

            LoadGorePieces(dataRoot.Gore.GorePieces);
            LoadGoreAreas(dataRoot.Gore.GoreAreas);

            Debug.Log($"[GoreDataLoader] Loaded {m_GorePiecesByName.Count} gore pieces and {m_GoreAreasByLocation.Count} gore areas.");
        }

        private void LoadGorePieces(List<GorePiece> pieces)
        {
            if (pieces == null)
            {
                return;
            }

            foreach (GorePiece piece in pieces)
            {
                if (piece == null || string.IsNullOrWhiteSpace(piece.name))
                {
                    continue;
                }

                if (m_GorePiecesByName.ContainsKey(piece.name))
                {
                    Debug.LogWarning($"[GoreDataLoader] Duplicate gore piece name ignored: {piece.name}");
                    continue;
                }

                m_GorePiecesByName[piece.name] = piece;
            }
        }

        private void LoadGoreAreas(List<GoreArea> areas)
        {
            if (areas == null)
            {
                return;
            }

            foreach (GoreArea area in areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.Location))
                {
                    continue;
                }

                if (m_GoreAreasByLocation.ContainsKey(area.Location))
                {
                    Debug.LogWarning($"[GoreDataLoader] Duplicate gore area location ignored: {area.Location}");
                    continue;
                }

                m_GoreAreasByLocation[area.Location] = area;
            }
        }

        private sealed class SoF2DataRoot
        {
            [JsonProperty("Gore")]
            public GoreSection Gore { get; set; }
        }

        private sealed class GoreSection
        {
            [JsonProperty("gore_pieces")]
            public List<GorePiece> GorePieces { get; set; }

            [JsonProperty("gore_areas")]
            public List<GoreArea> GoreAreas { get; set; }
        }
    }
}