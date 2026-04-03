using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Serialisierbare Spawn-Point-Position aus SoF2_Maps.json.
    /// </summary>
    [Serializable]
    public class SpawnPointData
    {
        public float x;
        public float y;
        public float z;

        /// <summary>Konvertiert die JSON-Daten in einen Unity Vector3.</summary>
        public Vector3 ToVector3() => new(x, y, z);
    }

    /// <summary>
    /// Definition einer Map aus SoF2_Maps.json.
    /// Enthaelt Map-Name, Countdown-Wert und team-basierte Spawn-Points.
    /// </summary>
    [Serializable]
    public class MapDefinition
    {
        /// <summary>Addressable-Key / ID der Map (z.B. "maps/cem1").</summary>
        public string id;

        /// <summary>Anzeigename der Map (z.B. "Cemetery").</summary>
        public string mapName;

        /// <summary>Texturpfad fuer den Ladescreen-Hintergrund (z.B. "gfx/menus/levelshots/cem1").</summary>
        public string levelShotBackgroundTexturePath;

        /// <summary>Spawn-Positionen fuer Team 1.</summary>
        public SpawnPointData[] team1SpawnPoints;

        /// <summary>Spawn-Positionen fuer Team 2.</summary>
        public SpawnPointData[] team2SpawnPoints;
    }
}
