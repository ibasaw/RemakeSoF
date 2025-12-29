using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Lazy Loader für On-Demand Texture Loading.
    /// </summary>
    public class LazyTextureLoader : ITextureLoader
    {
        private TextureRegistry m_Registry;

        public LazyTextureLoader(TextureRegistry registry)
        {
            m_Registry = registry;
        }

        public bool CanLoad(string key)
        {
            return m_Registry.TextureCache.ContainsKey(key);
        }

        public TextureData Load(string key)
        {
            if (!m_Registry.TextureCache.TryGetValue(key, out TextureData textureData))
                return null;

            if (!File.Exists(textureData.FilePath))
                return null;

            if (!textureData.HasTexture())
            {
                Texture2D texture = LoadTextureFromFile(textureData.FilePath);
                if (texture != null)
                {
                    texture.name = key;
                    m_Registry.UpdateTextureData(textureData.Id, texture);
                }
            }
            return textureData;
        }

        /// <summary>
        /// interne Methode lädt eine Texture aus einer Datei.
        /// </summary>
        private Texture2D LoadTextureFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[LazyTextureLoader] File not found: {filePath}");
                return null;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new(2, 2);

                if (texture.LoadImage(fileData))
                {
                    return texture;
                }
                else
                {
                    Debug.LogError($"[TextureManager] Failed to load image data from: {filePath}");
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TextureManager] Error loading texture from {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}