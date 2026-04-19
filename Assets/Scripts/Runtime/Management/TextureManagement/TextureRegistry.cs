using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Zentrale Verwaltung für Texture2D-Assets mit Caching und dynamischer Erweiterbarkeit.
    /// Default Lazy Loading via Custom Loadern.
    /// TextureRegistry → lädt die eigentlichen Texture2D-Assets und Materialien (Bilder, Materialien)
    /// Beim Anwenden eines Skins/Materials lädt TextureRegistry die Texturen/Materialien on-demand
    /// </summary>
    public class TextureRegistry
    {
        // Caching
        private readonly Dictionary<string, TextureData> m_TextureCache = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TextureData> TextureCache => m_TextureCache;
        private readonly Dictionary<string, ITextureLoader> m_CustomLoaders = new();

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
        }

        public void UpdateTextureData(string key, Texture2D texture)
        {
            if (m_TextureCache.TryGetValue(key, out TextureData data))
            {
                data.Texture = texture;
                //Debug.Log($"[TextureRegistry] set new texture for key: {key}");
            }
            else
            {
                Debug.LogWarning($"[TextureRegistry] Cannot set texture, key not found: {key}");
            }
        }

        public void UpdateTextureData(string key, Material material)
        {
            if (m_TextureCache.TryGetValue(key, out TextureData data))
            {
                data.Material = material;
                //Debug.Log($"[TextureRegistry] set new material for key: {key}");
            }
            else
            {
                Debug.LogWarning($"[TextureRegistry] Cannot set material, key not found: {key}");
            }
        }

        public void UpdateTextureWithAlias(string aliasKey, string targetKey, Material material)
        {
            if (m_TextureCache.TryGetValue(aliasKey, out TextureData targetData))
            {
                targetData.AliasKeys.Add(targetKey);
                targetData.Material = material;
                //Debug.Log($"[TextureRegistry] Created alias '{targetKey}' for alias key: {aliasKey} and set material");
            }
            else
            {
                Debug.LogWarning($"[TextureRegistry] Cannot create alias too, target key not found: {aliasKey}");
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
            {
                //Debug.Log($"[TextureRegistry] Returning cached texture: {key}");
                return cached;
            }
                

            if (TryLoadFromLoaders(key, out TextureData customData))
            {
                return customData;
            }

            return null;
        }

        public TextureData GetTextureDataByAlias(string aliasKey)
        {
            if (string.IsNullOrEmpty(aliasKey))
                return null;

            foreach (var data in m_TextureCache.Values)
            {
                if (data.AliasKeys.Contains(aliasKey) && data.IsValid() && data.HasTexture())
                {
                    return data;
                }
            }

            Debug.LogWarning($"[TextureRegistry] TextureData not found by alias: {aliasKey}");
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
            }
        }

        /// <summary>
        /// Löscht alle gecachten Texturen.
        /// </summary>
        public void ClearCache()
        {
            int updated = 0;
            foreach (var textureData in m_TextureCache.Values)
            {
                if (textureData?.Texture != null)
                {
                    UnityEngine.Object.Destroy(textureData.Texture);
                    updated++;
                }

                if (textureData?.Material != null)
                {
                    UnityEngine.Object.Destroy(textureData.Material);
                    updated++;
                }
            }
            Debug.Log($"[TextureRegistry] Texture Cache {m_TextureCache.Count} cleared, destroyed {updated} textures/materials");
            m_TextureCache.Clear();
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
