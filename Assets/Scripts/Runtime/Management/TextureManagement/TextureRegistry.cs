using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Zentrale Verwaltung für Texture2D-Assets mit Caching und dynamischer Erweiterbarkeit.
    /// Nutzt das Observer Pattern für Benachrichtigungen über Cache-Änderungen.
    /// Default Eager Loading und On-Demand Lazy Loading via Custom Loadern.
    /// TextureRegistry → lädt die eigentlichen Texture2D-Assets und Materialien (Bilder, Materialien)
    /// Beim Anwenden eines Skins/Materials lädt TextureRegistry die Texturen/Materialien on-demand
    /// </summary>
    public class TextureRegistry
    {
        // Caching
        private Dictionary<string, TextureData> m_TextureCache = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ITextureLoader> m_CustomLoaders = new();
        private List<ITextureRegistryObserver> m_Observers = new();

        #region Observer Management

        /// <summary>
        /// Registriert einen Observer für Cache-Benachrichtigungen.
        /// </summary>
        public void Subscribe(ITextureRegistryObserver observer)
        {
            if (observer != null && !m_Observers.Contains(observer))
            {
                m_Observers.Add(observer);
                Debug.Log("[TextureRegistry] Observer subscribed");
            }
        }

        /// <summary>
        /// Entfernt einen Observer.
        /// </summary>
        public void Unsubscribe(ITextureRegistryObserver observer)
        {
            if (observer != null && m_Observers.Remove(observer))
            {
                Debug.Log("[TextureRegistry] Observer unsubscribed");
            }
        }

        private void NotifyTextureRegistered(string key, TextureData textureData)
        {
            foreach (var observer in m_Observers)
            {
                try
                {
                    observer?.OnTextureRegistered(key, textureData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TextureRegistry] Error notifying observer: {ex.Message}");
                }
            }
        }

        private void NotifyTextureUnregistered(string key)
        {
            foreach (var observer in m_Observers)
            {
                try
                {
                    observer?.OnTextureUnregistered(key);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TextureRegistry] Error notifying observer: {ex.Message}");
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
                    Debug.LogError($"[TextureRegistry] Error notifying observer: {ex.Message}");
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
                    Debug.LogError($"[TextureRegistry] Error notifying observer: {ex.Message}");
                }
            }
        }

        #endregion

        #region Registration & Loading

        /// <summary>
        /// Registriert einen benutzerdefinierten Texture-Loader für bestimmte Keys oder Muster.
        /// </summary>
        /// <param name="key">Eindeutiger Schlüssel für den Loader</param>
        /// <param name="loader">Loader-Implementierung</param>
        public void RegisterLoader(string key, ITextureLoader loader)
        {
            if (string.IsNullOrEmpty(key) || loader == null)
            {
                Debug.LogWarning("[TextureRegistry] Invalid loader registration");
                return;
            }

            m_CustomLoaders[key] = loader;
            Debug.Log($"[TextureRegistry] Registered custom loader for: {key}");
            NotifyLoaderRegistered(key);
        }

        /// <summary>
        /// Vorlädt und registriert eine Texture mit einem bestimmten Key.
        /// </summary>
        public void RegisterTexture(string key, Texture2D texture, Material material, TextureSource source = TextureSource.System)
        {
            if (string.IsNullOrEmpty(key) || texture == null)
            {
                Debug.LogWarning("[TextureRegistry] Cannot register null texture");
                return;
            }

            var data = TextureDataFactory.Create(key, key, texture, material, source);
            RegisterTextureData(data);
        }

        /// <summary>
        /// Registriert bereits erstellte TextureData.
        /// </summary>
        public void RegisterTextureData(TextureData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id) || data.Texture == null)
            {
                Debug.LogWarning("[TextureRegistry] Cannot register invalid TextureData");
                return;
            }

            m_TextureCache[data.Id] = data;
            NotifyTextureRegistered(data.Id, data);
        }

        /// <summary>
        /// Gibt TextureData nach Key zurück. Nutzt Cache oder lädt aus registrierten Custom Loadern.
        /// </summary>
        public TextureData GetTextureData(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (m_TextureCache.TryGetValue(key, out var cached))
                return cached;

            if (TryLoadFromCustomLoaders(key, out var customData))
            {
                m_TextureCache[key] = customData;
                NotifyTextureRegistered(key, customData);
                return customData;
            }

            Debug.LogWarning($"[TextureRegistry] Texture not found: {key}");
            return null;
        }

        /// <summary>
        /// Gibt nur die Texture2D zurück (Legacy).
        /// </summary>
        public Texture2D GetTexture(string key)
        {
            return GetTextureData(key)?.Texture;
        }

        /// <summary>
        /// Prüft, ob eine Texture existiert (im Cache oder ladbar).
        /// </summary>
        /// <param name="key">Der Texture-Schlüssel</param>
        /// <returns>True, wenn die Texture verfügbar ist</returns>
        public bool HasTexture(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return m_TextureCache.ContainsKey(key) || CanLoad(key);
        }

        /// <summary>
        /// Entfernt eine Texture aus dem Cache und optional auch aus dem Speicher.
        /// </summary>
        /// <param name="key">Der Texture-Schlüssel</param>
        /// <param name="destroy">Wenn true, wird die Texture auch zerstört</param>
        public void UnregisterTexture(string key, bool destroy = false)
        {
            if (m_TextureCache.TryGetValue(key, out var data))
            {
                m_TextureCache.Remove(key);

                if (destroy && data.Texture != null)
                {
                    UnityEngine.Object.Destroy(data.Texture);
                }

                NotifyTextureUnregistered(key);
            }
        }

        /// <summary>
        /// Löscht alle gecachten Texturen.
        /// </summary>
        /// <param name="destroy">Wenn true, werden die Texturen auch aus dem Speicher gelöscht</param>
        public void ClearCache(bool destroy = false)
        {
            if (destroy)
            {
                foreach (var textureData in m_TextureCache.Values)
                {
                    if (textureData?.Texture != null)
                        UnityEngine.Object.Destroy(textureData.Texture);
                }
            }

            m_TextureCache.Clear();
            Debug.Log("[TextureRegistry] Cache cleared");
            NotifyCacheCleared();
        }

        #endregion

        #region Internal Loading

        private void RegisterDefaultLoaders()
        {
            // Hier können Standard-Loader registriert werden
            // Beispiel: RegisterLoader("ui", new UITextureLoader());
        }

        private bool TryLoadFromCustomLoaders(string key, out TextureData textureData)
        {
            textureData = null;

            foreach (var loader in m_CustomLoaders.Values)
            {
                if (loader.CanLoad(key))
                {
                    var texture = loader.Load(key);
                    if (texture != null)
                    {
                        textureData = TextureDataFactory.CreateCustomTexture(key, key, texture, null);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanLoad(string key)
        {
            // Prüfung, ob ein Custom Loader diese Texture laden kann
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
        /// Anzahl der gecachten Texturen.
        /// </summary>
        public int CachedTextureCount => m_TextureCache.Count;

        /// <summary>
        /// Anzahl der registrierten Custom Loader.
        /// </summary>
        public int RegisteredLoaderCount => m_CustomLoaders.Count;

        /// <summary>
        /// Anzahl der registrierten Observer.
        /// </summary>
        public int RegisteredObserverCount => m_Observers.Count;

        /// <summary>
        /// Gibt Statistiken über die TextureRegistry aus.
        /// </summary>
        public void LogStatistics()
        {
            Debug.Log($"[TextureRegistry] Statistics:\n" +
                      $"  Cached Textures: {CachedTextureCount}\n" +
                      $"  Custom Loaders: {RegisteredLoaderCount}\n" +
                      $"  Observers: {RegisteredObserverCount}");
        }

        #endregion
    }
}
