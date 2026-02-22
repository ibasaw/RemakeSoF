using System.Threading.Tasks;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Lädt Map-Prefabs über den PrefabManager und instanziiert sie in der aktiven Szene.
    /// Wird clientseitig verwendet.
    /// </summary>
    public class MapLoader
    {
        /// <summary>
        /// Die aktuell geladene Map-Instanz.
        /// </summary>
        private GameObject m_CurrentMapInstance;

        /// <summary>
        /// Lädt eine Map anhand ihres Namens über den PrefabManager.
        /// </summary>
        /// <param name="mapName">Der Addressable-Key Name der Map.</param>
        /// <returns>Die instanziierte Map oder null bei Fehler.</returns>
        public async Task<GameObject> LoadMapAsync(string mapName)
        {
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
            }

            GameObject mapPrefab = await prefabManager.LoadPrefabAsync<GameObject>(mapName);
            if (mapPrefab == null)
            {
                Debug.LogError($"[MapLoader] Failed to load map prefab: {mapName}");
                return null;
            }

            m_CurrentMapInstance = Object.Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
            m_CurrentMapInstance.name = $"Map_{mapName}";

            // Sicherstellen, dass die Map in der aktiven Szene landet (nicht in DontDestroyOnLoad)
            Scene activeScene = SceneManager.GetActiveScene();
            if (m_CurrentMapInstance.scene != activeScene)
            {
                SceneManager.MoveGameObjectToScene(m_CurrentMapInstance, activeScene);
            }

            Debug.Log($"[MapLoader] Map loaded and instantiated: {mapName} in scene: {activeScene.name} " +
                      $"| Position: {m_CurrentMapInstance.transform.position} " +
                      $"| Active: {m_CurrentMapInstance.activeSelf} " +
                      $"| Children: {m_CurrentMapInstance.transform.childCount}");
            return m_CurrentMapInstance;
        }

        /// <summary>
        /// Gibt die aktuelle Map-Instanz zurück.
        /// </summary>
        public GameObject CurrentMapInstance => m_CurrentMapInstance;
    }
}