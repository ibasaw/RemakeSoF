using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Zentrale Verwaltung für Texture2D-Assets mit Caching und dynamischer Erweiterbarkeit.
    /// Nutzt das Observer Pattern für Benachrichtigungen über Cache-Änderungen.
    /// Default Lazy Loading via Custom Loadern.
    /// TextureRegistry → lädt die eigentlichen Texture2D-Assets und Materialien (Bilder, Materialien)
    /// Beim Anwenden eines Skins/Materials lädt TextureRegistry die Texturen/Materialien on-demand
    /// </summary>
    public class TextureRegistry
    {
        // Caching
        private Dictionary<string, TextureData> m_TextureCache = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TextureData> TextureCache => m_TextureCache;
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
        /// registriert eine TextureData mit einem bestimmten Key.
        /// </summary>
        public void RegisterTextureData(TextureData data)
        {
            bool isOverride = m_TextureCache.ContainsKey(data.Id) && data.Source == TextureSource.Custom;
            if (data == null || !data.IsValid())
            {
                Debug.LogWarning("[TextureRegistry] Invalid TextureData registration");
                return;
            }
            if (m_TextureCache.ContainsKey(data.Id))
            {
                Debug.LogWarning($"[TextureRegistry] Duplicate texture key detected, overriding: {data.Id} => {data.Source}");
                //UnregisterTextureData(data.Id, true);
            }
            if (isOverride)
            {
                Debug.Log($"[TextureRegistry] Custom texture will override system texture for key: {data.Id} => {data.Source}");
            }
            m_TextureCache[data.Id] = data;
            NotifyTextureRegistered(data.Id, data);
        }

        public void UpdateTextureData(string key, Texture2D texture)
        {
            if (m_TextureCache.TryGetValue(key, out TextureData data))
            {
                data.Texture = texture;
                Debug.Log($"[TextureRegistry] set new texture for key: {key}");
            }
            else
            {
                Debug.LogWarning($"[TextureRegistry] Cannot set texture, key not found: {key}");
            }
        }

        /// <summary>
        /// Gibt TextureData nach Key zurück. Nutzt Cache oder lädt aus registrierten Custom Loadern.
        /// </summary>
        public TextureData GetTextureData(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (m_TextureCache.TryGetValue(key, out TextureData cached) && cached.IsValid() && cached.HasTexture())
                return cached;

            if (TryLoadFromLoaders(key, out TextureData customData))
            {
                return customData;
            }

            Debug.LogWarning($"[TextureRegistry] Texture not found: {key}");
            return null;
        }

        /// <summary>
        /// Entfernt eine Texture aus dem Cache und optional auch aus dem Speicher.
        /// </summary>
        /// <param name="key">Der Texture-Schlüssel</param>
        /// <param name="destroy">Wenn true, wird die Texture auch zerstört</param>
        public void UnregisterTextureData(string key, bool destroy = false)
        {
            if (m_TextureCache.TryGetValue(key, out TextureData data))
            {
                m_TextureCache.Remove(key);

                if (destroy && data.Texture != null)
                {
                    UnityEngine.Object.Destroy(data.Texture);
                }
                if (destroy && data.Material != null)
                {
                    UnityEngine.Object.Destroy(data.Material);
                }
                Debug.Log($"[TextureRegistry] Unregistered old texture: {key}");
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
                    if (textureData?.Material != null)
                        UnityEngine.Object.Destroy(textureData.Material);
                }
            }

            m_TextureCache.Clear();
            Debug.Log("[TextureRegistry] Texture Cache cleared");
            NotifyCacheCleared();
        }

        #endregion

        #region Internal Loading

        public TextureRegistry()
        {
            RegisterDefaultLoaders();
        }

        private void RegisterDefaultLoaders()
        {
            // Hier können Standard-Loader registriert werden
            RegisterLoader("lazy", new LazyTextureLoader(this));
        }

        private bool TryLoadFromLoaders(string key, out TextureData textureData)
        {
            textureData = null;

            foreach (var loader in m_CustomLoaders.Values)
            {
                if (loader.CanLoad(key))
                {
                    TextureData texture = loader.Load(key);
                    if (texture != null)
                    {
                        textureData = texture;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
