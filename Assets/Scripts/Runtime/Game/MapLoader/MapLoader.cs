using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
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
    /// Server: Richtet team-basierte SpawnPoints aus SoF2_Maps.json ein.
    /// Client: Zeigt Map-Visuals an.
    /// </summary>
    public class MapLoader
    {
        /// <summary>
        /// Die aktuell geladene Map-Instanz.
        /// </summary>
        private GameObject m_CurrentMapInstance;

        /// <summary>
        /// Name der aktuell geladenen Map (Idempotenz-Guard).
        /// </summary>
        private string m_LoadedMapName;

        /// <summary>
        /// Interner Service für das Erstellen von Collidern auf Map-Elementen.
        /// Wird auf Server UND Client ausgeführt.
        /// </summary>
        private readonly MapColliderApplier m_ColliderApplier = new();

        /// <summary>
        /// Interner Service für das Anwenden von Texturen auf Map-Elemente.
        /// Wird nur auf dem Client ausgeführt (benötigt TextureManager).
        /// </summary>
        private readonly MapTextureApplier m_TextureApplier = new();

        /// <summary>
        /// Interner Service für das Erstellen der Skybox aus SoF2 skyParms-Daten.
        /// Wird nur auf dem Client ausgeführt (benötigt TextureManager).
        /// </summary>
        private readonly MapSkyboxApplier m_SkyboxApplier = new();

        /// <summary>
        /// Event das bei jeder Ladephase gefeuert wird.
        /// </summary>
        internal event Action<MapLoadPhase> OnProgress;

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
                UnityEngine.Object.Destroy(m_CurrentMapInstance);
                m_CurrentMapInstance = null;
                m_LoadedMapName = null;
            }

            OnProgress?.Invoke(MapLoadPhase.Started);

            GameObject mapPrefab = await prefabManager.LoadPrefabAsync<GameObject>(mapName);
            if (mapPrefab == null)
            {
                Debug.LogError($"[MapLoader] Failed to load map prefab: {mapName}");
                OnProgress?.Invoke(MapLoadPhase.Failed);
                return null;
            }

            OnProgress?.Invoke(MapLoadPhase.PrefabLoaded);
            await Task.Yield();

            m_CurrentMapInstance = UnityEngine.Object.Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
            m_CurrentMapInstance.name = $"Map_{mapName}";
            m_LoadedMapName = mapName;

            // Sicherstellen, dass die Map in der aktiven Szene landet (nicht in DontDestroyOnLoad)
            Scene activeScene = SceneManager.GetActiveScene();
            if (m_CurrentMapInstance.scene != activeScene)
            {
                SceneManager.MoveGameObjectToScene(m_CurrentMapInstance, activeScene);
            }

            OnProgress?.Invoke(MapLoadPhase.Instantiated);
            await Task.Yield();

            // Collider auf Map-Elemente anwenden (Server + Client)
            m_ColliderApplier.ApplyColliders(m_CurrentMapInstance);

            OnProgress?.Invoke(MapLoadPhase.CollidersApplied);
            await Task.Yield();

            // Texturen auf Map-Elemente anwenden (nur Client, benötigt TextureManager)
            if(!NetworkManager.Singleton.IsServer)
                m_TextureApplier.ApplyTextures(m_CurrentMapInstance);

            OnProgress?.Invoke(MapLoadPhase.TexturesApplied);
            await Task.Yield();

            // Skybox aus skyParms-Daten erstellen (nur Client, benötigt TextureManager)
            if(!NetworkManager.Singleton.IsServer)
                m_SkyboxApplier.ApplySkybox(m_CurrentMapInstance);

            OnProgress?.Invoke(MapLoadPhase.SkyboxApplied);
            await Task.Yield();

            // Server: Spawn-Points aus SoF2_Maps.json laden
            SetupServerSpawnPoints(mapName);

            OnProgress?.Invoke(MapLoadPhase.SpawnPointsReady);
            OnProgress?.Invoke(MapLoadPhase.Complete);

            Debug.Log($"[MapLoader] Map loaded and instantiated: {mapName} in scene: {activeScene.name} " +
                      $"| Position: {m_CurrentMapInstance.transform.position} " +
                      $"| Active: {m_CurrentMapInstance.activeSelf} " +
                      $"| Children: {m_CurrentMapInstance.transform.childCount}");
            return m_CurrentMapInstance;
        }

        /// <summary>
        /// Laedt team-basierte Spawn-Points aus SoF2_Maps.json via MapDataLoader
        /// und erstellt das ServerPlayerSpawnPoints-Component (nur auf dem Server).
        /// </summary>
        /// <param name="mapName">Die Map-ID fuer das Lookup in SoF2_Maps.json.</param>
        private void SetupServerSpawnPoints(string mapName)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            MapDataLoader mapDataLoader = ServiceLocator.Get<MapDataLoader>();
            if (mapDataLoader == null)
            {
                Debug.LogError("[MapLoader] MapDataLoader not available via ServiceLocator.");
                return;
            }

            (List<Vector3> teamOne, List<Vector3> teamTwo) = mapDataLoader.GetSpawnPoints(mapName);

            GameObject spawnPointsGO = new("ServerSpawnPoints");
            spawnPointsGO.transform.SetParent(m_CurrentMapInstance.transform);

            ServerPlayerSpawnPoints spawnPoints = spawnPointsGO.AddComponent<ServerPlayerSpawnPoints>();
            spawnPoints.SetSpawnPoints(teamOne, teamTwo);

            Debug.Log($"[MapLoader] ServerPlayerSpawnPoints erstellt: " +
                      $"TeamOne={teamOne.Count}, TeamTwo={teamTwo.Count}");
        }

        /// <summary>
        /// Gibt die aktuelle Map-Instanz zurück.
        /// </summary>
        public GameObject CurrentMapInstance => m_CurrentMapInstance;
    }
}