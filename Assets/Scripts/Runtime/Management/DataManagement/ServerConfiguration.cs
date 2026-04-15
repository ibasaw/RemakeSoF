using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// SoF2-authentische Server-Konfiguration, serialisierbar aus SoF2_Server_Configuration.json.
    /// Felder orientieren sich an den originalen SoF2/Q3-CVARs.
    /// </summary>
    [Serializable]
    public class ServerConfiguration
    {
        // ── Server-CVARs ──────────────────────────────────────────────

        /// <summary>Servername (sv_hostname).</summary>
        public string sv_hostname;

        /// <summary>Server-Beschreibung (sv_description).</summary>
        public string sv_description;

        /// <summary>Message of the Day (sv_motd).</summary>
        public string sv_motd;

        /// <summary>Maximale Spieleranzahl (sv_maxclients).</summary>
        public int sv_maxclients;

        /// <summary>Minimale Spieleranzahl fuer Match-Start (sv_minclients).</summary>
        public int sv_minclients;

        /// <summary>Server-Port (sv_port).</summary>
        public int sv_port;

        /// <summary>Server-IP/Bind-Adresse (sv_ip).</summary>
        public string sv_ip;

        /// <summary>Server-Tickrate (sv_tickrate).</summary>
        public int sv_tickrate;

        /// <summary>Downloads erlauben (sv_allowdownload).</summary>
        public bool sv_allowdownload;

        /// <summary>Pure-Server-Modus (sv_pure).</summary>
        public bool sv_pure;

        /// <summary>Server-Passwort (sv_password). Leer = kein Passwort.</summary>
        public string sv_password;

        /// <summary>Passwort fuer reservierte Slots (sv_privatePassword). Leer = keine privaten Slots.</summary>
        public string sv_privatePassword;

        /// <summary>Remote-Console-Passwort (rconPassword). Leer = RCON deaktiviert.</summary>
        public string rconPassword;

        /// <summary>Master-Server-Adresse fuer Server-Registrierung (sv_master).</summary>
        public string sv_master;

        // ── Game-CVARs ────────────────────────────────────────────────

        /// <summary>Aktiver Gametype-Identifier (g_gametype), z.B. "tdm", "ctf", "hideandseek".</summary>
        public string g_gametype;

        /// <summary>Start-Map (g_mapname).</summary>
        public string g_mapname;

        /// <summary>Friendly Fire aktiviert (g_friendlyfire).</summary>
        public bool g_friendlyfire;

        // ── Limits ────────────────────────────────────────────────────

        /// <summary>Zeitlimit in Sekunden (timelimit).</summary>
        public int timelimit;

        /// <summary>Frag-Limit fuer DM/TDM (fraglimit).</summary>
        public int fraglimit;

        /// <summary>Score-Limit fuer CTF/Objective-Modi (scorelimit).</summary>
        public int scorelimit;

        /// <summary>Runden-Limit fuer rundenbasierte Modi (roundlimit).</summary>
        public int roundlimit;

        /// <summary>Rundenzeit-Limit in Sekunden (roundtimelimit).</summary>
        public int roundtimelimit;

        /// <summary>DM-Flags Bitfeld (dmflags).</summary>
        public int dmflags;

        // ── Gameplay-CVARs ────────────────────────────────────────────

        /// <summary>Gravitation (g_gravity), Default 800.</summary>
        public int g_gravity;

        /// <summary>Spieler-Geschwindigkeit (g_speed), Default 320.</summary>
        public int g_speed;

        /// <summary>Knockback-Multiplikator (g_knockback).</summary>
        public int g_knockback;

        /// <summary>Waffen-Respawn-Zeit in Sekunden (g_weaponrespawn).</summary>
        public int g_weaponrespawn;

        /// <summary>Automatisches Respawn erzwingen (g_forcerespawn), 0 = aus.</summary>
        public int g_forcerespawn;

        /// <summary>Inaktivitaets-Timeout in Sekunden (g_inactivity).</summary>
        public int g_inactivity;

        /// <summary>Warmup-Zeit in Sekunden (g_warmup).</summary>
        public int g_warmup;

        /// <summary>Voting erlauben (g_allowvote).</summary>
        public bool g_allowvote;

        /// <summary>Team-Auto-Join (g_teamAutoJoin).</summary>
        public bool g_teamAutoJoin;

        /// <summary>Team-Balance erzwingen (g_teamForceBalance).</summary>
        public bool g_teamForceBalance;

        // ── Map-Rotation ──────────────────────────────────────────────

        /// <summary>Map-Rotation (sv_mapRotation).</summary>
        public string[] sv_mapRotation;

        // ── Hide and Seek spezifisch ──────────────────────────────────

        /// <summary>Versteckzeit in Sekunden (hideandseek_hidetime).</summary>
        public int hideandseek_hidetime;

        /// <summary>Suchzeit in Sekunden (hideandseek_seektime).</summary>
        public int hideandseek_seektime;

        /// <summary>Anzahl Sucher (hideandseek_seekercount).</summary>
        public int hideandseek_seekercount;

        /// <summary>Erzwungenes Model fuer Hider (hideandseek_hidermodel), leer = frei waehlbar.</summary>
        public string hideandseek_hidermodel;

        /// <summary>Erzwungenes Model fuer Seeker (hideandseek_seekermodel), leer = frei waehlbar.</summary>
        public string hideandseek_seekermodel;

        /// <summary>Hider duerfen Waffen benutzen (hideandseek_hiderweapons).</summary>
        public bool hideandseek_hiderweapons;

        /// <summary>Seeker duerfen Waffen benutzen (hideandseek_seekerweapons).</summary>
        public bool hideandseek_seekerweapons;

        /// <summary>Runden-Limit fuer Hide and Seek (hideandseek_roundlimit).</summary>
        public int hideandseek_roundlimit;

        // ── Bot-Konfiguration ─────────────────────────────────────────

        /// <summary>Start-Waffen fuer Team Rot (g_redTeamStartWeapons). Leer/null = Default (alle Waffen). Knife ist immer enthalten.</summary>
        public string[] g_redTeamStartWeapons;

        /// <summary>Start-Waffen fuer Team Blau (g_blueTeamStartWeapons). Leer/null = Default (alle Waffen). Knife ist immer enthalten.</summary>
        public string[] g_blueTeamStartWeapons;

        // ── Bot-Konfiguration ─────────────────────────────────────────

        /// <summary>Anzahl AI-Bots die beim Server-Start gespawnt werden (sv_botcount). 0 = keine Bots.</summary>
        public int sv_botcount;

        /// <summary>Bot-Namen (sv_botnames). Round-Robin-Zuweisung. Leer = Default-Namen.</summary>
        public string[] sv_botnames;

        /// <summary>Bot-Skins (sv_botskins). Round-Robin-Zuweisung. Leer = Default-Skin.</summary>
        public string[] sv_botskins;
    }
}
