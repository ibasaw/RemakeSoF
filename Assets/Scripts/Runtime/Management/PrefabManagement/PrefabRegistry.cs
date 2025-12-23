using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tolik.RemakeSoF.Runtime.PrefabManagement
{
    /// <summary>
    /// Zentrale Verwaltung für Prefab-Assets mit Caching und dynamischer Erweiterbarkeit.
    /// Nutzt das Observer Pattern für Benachrichtigungen über Cache-Änderungen.
    /// Analog zur TextureRegistry für konsistentes Design Pattern.
    /// </summary>
    public class PrefabRegistry
    {
        // Caching
        private Dictionary<string, PrefabData> m_PrefabCache = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, IPrefabLoader> m_CustomLoaders = new();
        private List<IPrefabRegistryObserver> m_Observers = new();

        #region Observer Interface

        /// <summary>
        /// Observer Interface für Cache-Änderungen.
        /// </summary>
        public interface IPrefabRegistryObserver
        {
            /// <summary>
            /// Aufgerufen wenn ein Prefab registriert wurde.
            /// </summary>
            void OnPrefabRegistered(string key, PrefabData prefabData);

            /// <summary>
            /// Aufgerufen wenn ein Prefab entfernt wurde.
            /// </summary>
            void OnPrefabUnregistered(string key);

            /// <summary>
            /// Aufgerufen wenn der Cache geleert wurde.
            /// </summary>
            void OnCacheCleared();

            /// <summary>
            /// Aufgerufen wenn ein Loader registriert wurde.
            /// </summary>
            void OnLoaderRegistered(string key);
        }

        #endregion

        #region Loader Interface

        /// <summary>
        /// Interface für benutzerdefinierte Prefab-Loader.
        /// </summary>
        public interface IPrefabLoader
        {
            /// <summary>
            /// Prüft, ob dieser Loader das Prefab laden kann.
            /// </summary>
            bool CanLoad(string key);

            /// <summary>
            /// Lädt das Prefab mit dem gegebenen Key.
            /// </summary>
            GameObject Load(string key);
        }

        #endregion

        #region Observer Management

        /// <summary>
        /// Registriert einen Observer für Cache-Benachrichtigungen.
        /// </summary>
        public void Subscribe(IPrefabRegistryObserver observer)
        {
            if (observer != null && !m_Observers.Contains(observer))
            {
                m_Observers.Add(observer);
                Debug.Log("[PrefabRegistry] Observer subscribed");
            }
        }

        /// <summary>
        /// Entfernt einen Observer.
        /// </summary>
        public void Unsubscribe(IPrefabRegistryObserver observer)
        {
            if (observer != null && m_Observers.Remove(observer))
            {
                Debug.Log("[PrefabRegistry] Observer unsubscribed");
            }
        }

        private void NotifyPrefabRegistered(string key, PrefabData prefabData)
        {
            foreach (var observer in m_Observers)
            {
                try
                {
                    observer?.OnPrefabRegistered(key, prefabData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PrefabRegistry] Error notifying observer: {ex.Message}");
                }
            }
        }

        private void NotifyPrefabUnregistered(string key)
        {
            foreach (var observer in m_Observers)
            {
                try
                {
                    observer?.OnPrefabUnregistered(key);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PrefabRegistry] Error notifying observer: {ex.Message}");
                }
            }
        }

        private void NotifyCacheCleared()
        {
            foreach (var observer in m_Observers)
            {
                try
                {
                    observer?.OnCacheCleared();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PrefabRegistry] Error notifying observer: {ex.Message}");
                }
            }
        }

        private void NotifyLoaderRegistered(string key)
        {
            foreach (var observer in m_Observers)
            {
                try
                {
                    observer?.OnLoaderRegistered(key);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PrefabRegistry] Error notifying observer: {ex.Message}");
                }
            }
        }

        #endregion

        #region Registration & Loading

        /// <summary>
        /// Registriert einen benutzerdefinierten Prefab-Loader für bestimmte Keys oder Muster.
        /// </summary>
        public void RegisterLoader(string key, IPrefabLoader loader)
        {
            if (string.IsNullOrEmpty(key) || loader == null)
            {
                Debug.LogWarning("[PrefabRegistry] Invalid loader registration");
                return;
            }

            m_CustomLoaders[key] = loader;
            Debug.Log($"[PrefabRegistry] Registered custom loader for: {key}");
            NotifyLoaderRegistered(key);
        }

        /// <summary>
        /// Vorlädt und registriert ein Prefab mit einem bestimmten Key.
        /// </summary>
        public void RegisterPrefab(string key, GameObject prefab, PrefabSource source = PrefabSource.System)
        {
            if (string.IsNullOrEmpty(key) || prefab == null)
            {
                Debug.LogWarning("[PrefabRegistry] Cannot register null prefab or empty key");
                return;
            }

            var data = PrefabDataFactory.Create(key, prefab, source);
            RegisterPrefabData(data);
        }

        /// <summary>
        /// Registriert bereits erstellte PrefabData.
        /// </summary>
        public void RegisterPrefabData(PrefabData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id) || data.Prefab == null)
            {
                Debug.LogWarning("[PrefabRegistry] Cannot register invalid PrefabData");
                return;
            }

            m_PrefabCache[data.Id] = data;
            NotifyPrefabRegistered(data.Id, data);
        }

        /// <summary>
        /// Gibt PrefabData nach Key zurück. Nutzt Cache oder lädt aus registrierten Custom Loadern.
        /// </summary>
        public PrefabData GetPrefabData(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (m_PrefabCache.TryGetValue(key, out var cached))
                return cached;

            if (TryLoadFromCustomLoaders(key, out var customData))
            {
                m_PrefabCache[key] = customData;
                NotifyPrefabRegistered(key, customData);
                return customData;
            }

            Debug.LogWarning($"[PrefabRegistry] Prefab not found: {key}");
            return null;
        }

        /// <summary>
        /// Gibt nur das Prefab GameObject zurück (Legacy).
        /// </summary>
        public GameObject GetPrefab(string key)
        {
            return GetPrefabData(key)?.Prefab;
        }

        /// <summary>
        /// Prüft, ob ein Prefab existiert (im Cache oder ladbar).
        /// </summary>
        public bool HasPrefab(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return m_PrefabCache.ContainsKey(key) || CanLoad(key);
        }

        /// <summary>
        /// Entfernt ein Prefab aus dem Cache und released das Addressables-Handle.
        /// </summary>
        public void UnregisterPrefab(string key)
        {
            if (m_PrefabCache.TryGetValue(key, out var data))
            {
                m_PrefabCache.Remove(key);

                // Release Addressables handle falls vorhanden
                if (data.Handle.IsValid()) // ✅ Prüfe ob Handle valide ist
                {
                    Addressables.Release(data.Handle);
                    Debug.Log($"[PrefabRegistry] Released Addressables handle for: {key}");
                }

                NotifyPrefabUnregistered(key);
            }
        }

        /// <summary>
        /// Löscht alle gecachten Prefab-Referenzen und released Addressables-Handles.
        /// </summary>
        public void ClearCache()
        {
            int releasedCount = 0;
            foreach (var data in m_PrefabCache.Values)
            {
                if (data.Handle.IsValid()) // ✅ Prüfe ob Handle valide ist
                {
                    Addressables.Release(data.Handle);
                    releasedCount++;
                }
            }

            m_PrefabCache.Clear();
            Debug.Log($"[PrefabRegistry] Cache cleared - released {releasedCount} Addressables handles");
            NotifyCacheCleared();
        }

        #endregion

        #region Internal Loading

        private bool TryLoadFromCustomLoaders(string key, out PrefabData prefabData)
        {
            prefabData = null;

            foreach (var loader in m_CustomLoaders.Values)
            {
                if (loader.CanLoad(key))
                {
                    var prefab = loader.Load(key);
                    if (prefab != null)
                    {
                        prefabData = PrefabDataFactory.CreateFromLoader(key, prefab);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanLoad(string key)
        {
            foreach (var loader in m_CustomLoaders.Values)
            {
                if (loader.CanLoad(key))
                    return true;
            }

            return false;
        }

        #endregion

        #region Statistics & Debug

        /// <summary>
        /// Anzahl der gecachten Prefabs.
        /// </summary>
        public int CachedPrefabCount => m_PrefabCache.Count;

        /// <summary>
        /// Anzahl der registrierten Custom Loader.
        /// </summary>
        public int RegisteredLoaderCount => m_CustomLoaders.Count;

        /// <summary>
        /// Anzahl der registrierten Observer.
        /// </summary>
        public int RegisteredObserverCount => m_Observers.Count;

        /// <summary>
        /// Gibt Statistiken über die PrefabRegistry aus.
        /// </summary>
        public void LogStatistics()
        {
            Debug.Log($"[PrefabRegistry] Statistics:\n" +
                      $"  Cached Prefabs: {CachedPrefabCount}\n" +
                      $"  Custom Loaders: {RegisteredLoaderCount}\n" +
                      $"  Observers: {RegisteredObserverCount}\n" +
                      $"  Cached Keys: {string.Join(", ", m_PrefabCache.Keys)}");
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Datenstruktur für gespeicherte Prefab-Informationen.
    /// </summary>
    public class PrefabData
    {
        public string Id { get; set; }
        public GameObject Prefab { get; set; }
        public PrefabSource Source { get; set; }
        public DateTime LoadedAt { get; set; }

        /// <summary>
        /// Addressables Handle für Memory Management.
        /// Non-generic AsyncOperationHandle kann alle Asset-Typen halten.
        /// </summary>
        public AsyncOperationHandle Handle { get; set; }
    }

    /// <summary>
    /// Source, von wo das Prefab geladen wurde.
    /// </summary>
    public enum PrefabSource
    {
        System,      // Aus Editor/Resources geladen
        Custom,      // Von Custom Loader
        Manual       // Manuell registriert
    }

    /// <summary>
    /// Factory für PrefabData-Erstellung.
    /// </summary>
    public static class PrefabDataFactory
    {
        public static PrefabData Create(string key, GameObject prefab, PrefabSource source = PrefabSource.System)
        {
            return new PrefabData
            {
                Id = key,
                Prefab = prefab,
                Source = source,
                LoadedAt = DateTime.Now
            };
        }

        public static PrefabData CreateFromLoader(string key, GameObject prefab)
        {
            return Create(key, prefab, PrefabSource.Custom);
        }
    }

    #endregion
}
