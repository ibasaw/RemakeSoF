using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tolik.RemakeSoF.Runtime.PrefabManagement
{
    /// <summary>
    /// Manager für das Laden von Prefabs über Addressables.
    /// Bietet einfache async Methoden zum Laden und Instantiieren von Prefabs.
    /// </summary>
    public class PrefabManager : MonoBehaviour
    {
        private PrefabRegistry m_Registry;
        public PrefabRegistry Registry => m_Registry;

        #region Lifecycle

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            m_Registry = new PrefabRegistry();
            Debug.Log("[PrefabManager] Initialized");
        }

        void OnDestroy()
        {
            m_Registry?.ClearCache();
            m_Registry = null;
            Debug.Log("[PrefabManager] Destroyed");
        }

        #endregion
        
        #region Loading
        /// <summary>
        /// Lädt ein Prefab synchron über Addressables und registriert es in der Registry.
        /// WARNUNG: Blockiert den Main Thread - verwende async wenn möglich!
        /// </summary>
        public T LoadPrefab<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[PrefabManager] Prefab key is empty");
                return null;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                var asset = handle.WaitForCompletion();
                if (asset != null)
                {
                    // Optional: Registry/Handle-Management wie bei GameObject
                    Debug.Log($"[PrefabManager] Loaded via Addressables: {key} as {typeof(T).Name}");
                    return asset;
                }
                else
                {
                    Addressables.Release(handle);
                    Debug.LogError($"[PrefabManager] Addressable not found: {key}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PrefabManager] Error loading asset {key}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lädt ein Prefab asynchron über Addressables und registriert es in der Registry.
        /// </summary>
        public async Task<GameObject> LoadPrefabAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[PrefabManager] Prefab key is empty");
                return null;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                var prefab = await handle.Task;

                if (prefab != null)
                {
                    var data = PrefabDataFactory.Create(key, prefab, PrefabSource.System);
                    data.Handle = handle; // Handle speichern für späteres Release

                    m_Registry.RegisterPrefabData(data);
                    Debug.Log($"[PrefabManager] Loaded via Addressables: {key}");
                    return prefab;
                }
                else
                {
                    Addressables.Release(handle); // Release bei Fehler
                    Debug.LogWarning($"[PrefabManager] Addressable not found: {key}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PrefabManager] Error loading prefab {key}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registriert einen benutzerdefinierten Prefab-Loader.
        /// </summary>
        public void RegisterLoader(string key, PrefabRegistry.IPrefabLoader loader)
        {
            if (m_Registry != null)
            {
                m_Registry.RegisterLoader(key, loader);
            }
        }

        /// <summary>
        /// Abonniert Observer-Benachrichtigungen für Registry-Änderungen.
        /// </summary>
        public void SubscribeToRegistry(PrefabRegistry.IPrefabRegistryObserver observer)
        {
            if (m_Registry != null)
            {
                m_Registry.Subscribe(observer);
            }
        }

        /// <summary>
        /// Beendet das Abonnement von Registry-Benachrichtigungen.
        /// </summary>
        public void UnsubscribeFromRegistry(PrefabRegistry.IPrefabRegistryObserver observer)
        {
            if (m_Registry != null)
            {
                m_Registry.Unsubscribe(observer);
            }
        }

        /// <summary>
        /// Löscht den Cache und optional auch die Prefabs aus dem Speicher.
        /// </summary>
        public void ClearCache()
        {
            m_Registry?.ClearCache();
        }

        #endregion

        #region Debug

        /// <summary>
        /// Gibt Statistiken über die PrefabRegistry aus.
        /// </summary>
        public void LogStatistics()
        {
            m_Registry?.LogStatistics();
        }

        #endregion
    }
}
