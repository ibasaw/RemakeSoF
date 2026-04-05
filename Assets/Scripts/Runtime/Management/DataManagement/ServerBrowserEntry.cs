using System;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Repraesentiert einen einzelnen Server-Eintrag im Server-Browser.
    /// Wird vom Master-Server als JSON geliefert und vom Client deserialisiert.
    /// </summary>
    [Serializable]
    public class ServerBrowserEntry
    {
        /// <summary>Eindeutige Server-ID (vom Master-Server vergeben).</summary>
        public string id;

        /// <summary>Servername (sv_hostname).</summary>
        public string hostname;

        /// <summary>IP-Adresse des Servers.</summary>
        public string ip;

        /// <summary>Port des Servers.</summary>
        public int port;

        /// <summary>Aktuelle Map.</summary>
        public string mapName;

        /// <summary>Aktiver Gametype (z.B. "tdm", "hideandseek").</summary>
        public string gametype;

        /// <summary>Aktuell verbundene Spieler.</summary>
        public int currentPlayers;

        /// <summary>Maximale Spieleranzahl.</summary>
        public int maxPlayers;

        /// <summary>Ping in Millisekunden (optional, 0 wenn unbekannt).</summary>
        public int ping;

        /// <summary>Ob der Server ein Passwort erfordert (sv_password gesetzt).</summary>
        public bool hasPassword;

        /// <summary>Application-Version des Servers.</summary>
        public string version;
    }

    /// <summary>
    /// Wrapper fuer die JSON-Deserialisierung eines Server-Arrays vom Master-Server.
    /// JsonUtility kann keine Top-Level Arrays deserialisieren, daher dieser Wrapper.
    /// </summary>
    [Serializable]
    public class ServerBrowserEntryList
    {
        /// <summary>Liste aller registrierten Server.</summary>
        public ServerBrowserEntry[] servers;
    }

    /// <summary>
    /// Paginierte Antwort vom Master-Server fuer den Server-Browser.
    /// Enthaelt eine Seite von Servern sowie Metadaten fuer Infinite Scroll.
    /// </summary>
    [Serializable]
    public class ServerBrowserPageResponse
    {
        /// <summary>Server-Eintraege dieser Seite.</summary>
        public ServerBrowserEntry[] servers;

        /// <summary>Gesamtanzahl aller verfuegbaren Server.</summary>
        public int total;

        /// <summary>Offset ab dem diese Seite beginnt.</summary>
        public int offset;

        /// <summary>Ob weitere Server nach dieser Seite verfuegbar sind.</summary>
        public bool hasMore;
    }

    /// <summary>
    /// Payload der beim Registrieren eines Servers an den Master-Server gesendet wird.
    /// Enthaelt alle relevanten Server-CVARs aus der ServerConfiguration.
    /// </summary>
    [Serializable]
    public class ServerRegistrationPayload
    {
        /// <summary>Servername (sv_hostname).</summary>
        public string hostname;

        /// <summary>Oeffentliche IP-Adresse des Servers (oder leer fuer Auto-Detect).</summary>
        public string ip;

        /// <summary>Port des Servers.</summary>
        public int port;

        /// <summary>Aktuelle Map.</summary>
        public string mapName;

        /// <summary>Aktiver Gametype.</summary>
        public string gametype;

        /// <summary>Aktuell verbundene Spieler.</summary>
        public int currentPlayers;

        /// <summary>Maximale Spieleranzahl.</summary>
        public int maxPlayers;

        /// <summary>Ob der Server ein Passwort erfordert.</summary>
        public bool hasPassword;

        /// <summary>Server-Passwort (sv_password). Wird vom Auth-Server gespeichert, damit Clients es abgleichen koennen. Leer = kein Passwort.</summary>
        public string password;

        /// <summary>RCON-Passwort (rconPassword). Wird fuer Deregistrierung benoetigt. Leer = Auth-Server generiert eines.</summary>
        public string rconPassword;

        /// <summary>Application-Version.</summary>
        public string version;
    }

    /// <summary>
    /// Antwort des Master-Servers auf eine Server-Registrierung.
    /// </summary>
    [Serializable]
    public class ServerRegistrationResponse
    {
        /// <summary>Vom Master-Server vergebene Server-ID (fuer Heartbeat/Deregistrierung).</summary>
        public string serverId;

        /// <summary>Status-Nachricht.</summary>
        public string message;

        /// <summary>RCON-Passwort. Vom Auth-Server generiert falls keines mitgesendet wurde.</summary>
        public string rconPassword;
    }
}
