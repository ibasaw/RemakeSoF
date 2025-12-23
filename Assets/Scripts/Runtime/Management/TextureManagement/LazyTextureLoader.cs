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
        private Dictionary<string, string> m_PathMap;
        private TextureManager m_Manager;

        public LazyTextureLoader(Dictionary<string, string> pathMap, TextureManager manager)
        {
            m_PathMap = pathMap;
            m_Manager = manager;
        }

        public bool CanLoad(string key)
        {
            return m_PathMap.ContainsKey(key);
        }

        public Texture2D Load(string key)
        {
            if (!m_PathMap.TryGetValue(key, out var filePath))
                return null;

            if (!File.Exists(filePath))
                return null;

            Texture2D texture = m_Manager.GetTextureData(filePath).Texture;
            if (texture != null)
                texture.name = key;

            return texture;
        }
    }
}