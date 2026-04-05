using System;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Respawn-Typ fuer Gametypes, angelehnt an SoF2 respawnType_t.
    /// </summary>
    public enum RespawnType
    {
        /// <summary>Normales Respawning (DM/TDM/CTF).</summary>
        Normal,
        /// <summary>Intervall-Respawning.</summary>
        Interval,
        /// <summary>Kein Respawning (Elimination/Demolition/Infiltration/HideAndSeek).</summary>
        None
    }

    /// <summary>
    /// Definition eines Gametypes aus SoF2_Gametypes.json.
    /// Enthaelt Metadaten und Gameplay-Regeln die fuer alle Gametypes gelten.
    /// </summary>
    [Serializable]
    public class GametypeDefinition
    {
        /// <summary>Gametype-Identifier (z.B. "tdm", "ctf", "hideandseek").</summary>
        public string gametype;

        /// <summary>Anzeigename (z.B. "Team Deathmatch").</summary>
        public string gametypeName;

        /// <summary>Beschreibung fuer UI.</summary>
        public string gametypeDescription;

        /// <summary>Bildpfad fuer UI.</summary>
        public string gametypeImage;

        /// <summary>Team-basierter Modus.</summary>
        public bool teams;

        /// <summary>Respawn-Typ als String ("normal", "interval", "none").</summary>
        public string respawnType;

        /// <summary>Waffen-Pickups deaktiviert.</summary>
        public bool pickupsDisabled;

        /// <summary>Kill-Anzeige aktiv.</summary>
        public bool showKills;

        /// <summary>Rundenbasierter Modus (mit Restart-Logik).</summary>
        public bool roundBased;

        /// <summary>Anzeigename fuer Team 1 / Rot (z.B. "Hider").</summary>
        public string team1Name;

        /// <summary>Anzeigename fuer Team 2 / Blau (z.B. "Seeker").</summary>
        public string team2Name;

        /// <summary>
        /// Parsed den respawnType-String in ein RespawnType-Enum.
        /// </summary>
        public RespawnType GetRespawnType()
        {
            return respawnType switch
            {
                "none" => RespawnType.None,
                "interval" => RespawnType.Interval,
                _ => RespawnType.Normal
            };
        }
    }
}
