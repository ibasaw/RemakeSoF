using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Payload, der beim Verbindungsaufbau vom Client an den Server übermittelt wird.
    /// Enthält Basis-Verbindungsdaten sowie Spieler-Identitätsdaten.
    /// </summary>
    [Serializable]
    public class ConnectionPayload
    {
        /// <summary>
        /// Anwendungsversion für Kompatibilitätsprüfung.
        /// </summary>
        public string applicationVersion;

        /// <summary>
        /// Anzeigename des Spielers (aus dem MetagameModel nach Authentifizierung).
        /// </summary>
        public string playerName;

        /// <summary>
        /// Der zuletzt gewählte Skin-Name des Spielers.
        /// </summary>
        public string skinName;
    }
}
