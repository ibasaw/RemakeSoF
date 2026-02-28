using System.Threading.Tasks;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Lädt Map-Prefabs über den PrefabManager und instanziiert sie in der aktiven Szene.
    /// Wird auf Server UND Clients verwendet.
    /// Server: Richtet SpawnPoints ein.
    /// Client: Zeigt Map-Visuals an.
    /// </summary>
    public class MapLoader
    {
        /// <summary>
        /// Name des GameObjects in der Map, das die Spawn-Points als Kinder enthält.
        /// </summary>
        private const string k_SpawnPointsObjectName = "PlayerSpawnPoints";

        /// <summary>
        /// Die aktuell geladene Map-Instanz.
        /// </summary>
        private GameObject m_CurrentMapInstance;

        /// <summary>
        /// Name der aktuell geladenen Map (Idempotenz-Guard).
        /// </summary>
        private string m_LoadedMapName;

        /// <summary>
        /// Lädt eine Map anhand ihres Namens über den PrefabManager.
        /// Idempotent: Gleiche Map wird nicht doppelt geladen.
        /// </summary>
        /// <param name="mapName">Der Addressable-Key Name der Map.</param>
        /// <returns>Die instanziierte Map oder null bei Fehler.</returns>
        public async Task<GameObject> LoadMapAsync(string mapName)
        {
            // Idempotenz: Gleiche Map nicht doppelt laden
            if (m_LoadedMapName == mapName && m_CurrentMapInstance != null)
            {
                Debug.Log($"[MapLoader] Map '{mapName}' ist bereits geladen. Überspringe.");
                return m_CurrentMapInstance;
            }

            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogError("[MapLoader] PrefabManager not available via ServiceLocator.");
                return null;
            }

            if (m_CurrentMapInstance != null)
            {
                Debug.Log($"[MapLoader] Destroying previous map: {m_CurrentMapInstance.name}");
                Object.Destroy(m_CurrentMapInstance);
                m_CurrentMapInstance = null;
                m_LoadedMapName = null;
            }

            GameObject mapPrefab = await prefabManager.LoadPrefabAsync<GameObject>(mapName);
            if (mapPrefab == null)
            {
                Debug.LogError($"[MapLoader] Failed to load map prefab: {mapName}");
                return null;
            }

            m_CurrentMapInstance = Object.Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
            m_CurrentMapInstance.name = $"Map_{mapName}";
            m_LoadedMapName = mapName;

            // Sicherstellen, dass die Map in der aktiven Szene landet (nicht in DontDestroyOnLoad)
            Scene activeScene = SceneManager.GetActiveScene();
            if (m_CurrentMapInstance.scene != activeScene)
            {
                SceneManager.MoveGameObjectToScene(m_CurrentMapInstance, activeScene);
            }

            // Server: SpawnPoints einrichten
            SetupServerSpawnPoints();

            Debug.Log($"[MapLoader] Map loaded and instantiated: {mapName} in scene: {activeScene.name} " +
                      $"| Position: {m_CurrentMapInstance.transform.position} " +
                      $"| Active: {m_CurrentMapInstance.activeSelf} " +
                      $"| Children: {m_CurrentMapInstance.transform.childCount}");
            return m_CurrentMapInstance;
        }

        /// <summary>
        /// Sucht das "PlayerSpawnPoints"-GameObject in der Map und
        /// hängt ServerPlayerSpawnPoints an (nur auf dem Server).
        /// </summary>
        private void SetupServerSpawnPoints()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            Transform spawnPointsTransform = m_CurrentMapInstance.transform.Find(k_SpawnPointsObjectName);
            if (spawnPointsTransform == null)
            {
                Debug.LogWarning($"[MapLoader] Kein '{k_SpawnPointsObjectName}'-GameObject in der Map gefunden!");
                return;
            }

            // ServerPlayerSpawnPoints anhängen (falls nicht bereits vorhanden)
            ServerPlayerSpawnPoints existing = spawnPointsTransform.GetComponent<ServerPlayerSpawnPoints>();
            if (existing == null)
            {
                spawnPointsTransform.gameObject.AddComponent<ServerPlayerSpawnPoints>();
                Debug.Log($"[MapLoader] ServerPlayerSpawnPoints an '{k_SpawnPointsObjectName}' angehängt " +
                          $"({spawnPointsTransform.childCount} Spawn-Points)");
            }
        }

        /// <summary>
        /// Gibt die aktuelle Map-Instanz zurück.
        /// </summary>
        public GameObject CurrentMapInstance => m_CurrentMapInstance;
    }
}