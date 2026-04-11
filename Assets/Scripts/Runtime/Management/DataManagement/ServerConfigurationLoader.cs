using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Laedt die SoF2-Server-Konfiguration aus Resources/Data/SoF2_Server_Configuration.json.
    /// Pure Service, registriert im ServiceLocator. Stellt die aktive ServerConfiguration bereit.
    /// </summary>
    public class ServerConfigurationLoader
    {
        /// <summary>Pfad zur JSON-Datei relativ zu StreamingAssets/.</summary>
        const string k_DataPath = "Data/SoF2_Server_Configuration.json";

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
            string filePath = Path.Combine(Application.streamingAssetsPath, k_DataPath);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[ServerConfigurationLoader] Konfiguration nicht gefunden: {filePath}");
                m_Configuration = new ServerConfiguration();
                return;
            }

            string json = File.ReadAllText(filePath);
            m_Configuration = JsonUtility.FromJson<ServerConfiguration>(json);
            Debug.Log($"[ServerConfigurationLoader] Konfiguration geladen: sv_hostname={m_Configuration.sv_hostname}, g_gametype={m_Configuration.g_gametype}, g_mapname={m_Configuration.g_mapname}");
        }
    }
}
