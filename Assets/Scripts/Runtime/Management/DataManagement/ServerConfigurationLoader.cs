using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Laedt die SoF2-Server-Konfiguration aus Resources/Data/SoF2_Server_Configuration.json.
    /// Pure Service, registriert im ServiceLocator. Stellt die aktive ServerConfiguration bereit.
    /// </summary>
    public class ServerConfigurationLoader
    {
        /// <summary>Pfad zur JSON-Datei relativ zu Resources/.</summary>
        const string k_ResourcePath = "Data/SoF2_Server_Configuration";

        /// <summary>Die geladene Server-Konfiguration.</summary>
        ServerConfiguration m_Configuration;

        /// <summary>
        /// Die aktive Server-Konfiguration (readonly nach dem Laden).
        /// </summary>
        public ServerConfiguration Configuration => m_Configuration;

        /// <summary>
        /// Konstruktor: Laedt die Konfiguration aus der JSON-Datei.
        /// </summary>
        public ServerConfigurationLoader()
        {
            LoadFromResources();
        }

        /// <summary>
        /// Laedt die Server-Konfiguration aus Resources.
        /// </summary>
        void LoadFromResources()
        {
            TextAsset textAsset = Resources.Load<TextAsset>(k_ResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"[ServerConfigurationLoader] Konfiguration nicht gefunden: {k_ResourcePath}");
                m_Configuration = new ServerConfiguration();
                return;
            }

            m_Configuration = JsonUtility.FromJson<ServerConfiguration>(textAsset.text);
            Debug.Log($"[ServerConfigurationLoader] Konfiguration geladen: sv_hostname={m_Configuration.sv_hostname}, g_gametype={m_Configuration.g_gametype}, g_mapname={m_Configuration.g_mapname}");
        }
    }
}
