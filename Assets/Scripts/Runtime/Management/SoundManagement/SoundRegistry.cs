using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.SoundManagement
{
    /// <summary>
    /// Zentrale Verwaltung fuer AudioClip-Assets mit Caching und dynamischer Erweiterbarkeit.
    /// Analog zu TextureRegistry — Cache-first mit Custom-Loader-Support.
    /// SoundRegistry laedt die eigentlichen AudioClip-Assets on-demand.
    /// </summary>
    public class SoundRegistry
    {
        private readonly Dictionary<string, SoundData> m_SoundCache = new(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>Zugriff auf den Sound-Cache (fuer Loader).</summary>
        public Dictionary<string, SoundData> SoundCache => m_SoundCache;

        private readonly Dictionary<string, ISoundLoader> m_CustomLoaders = new();

        /// <summary>
        /// Erstellt eine neue SoundRegistry-Instanz und registriert Default-Loader.
        /// </summary>
        public SoundRegistry()
        {
            RegisterDefaultLoaders();
        }

        /// <summary>
        /// Registriert einen benutzerdefinierten Sound-Loader fuer bestimmte Keys.
        /// </summary>
        public void RegisterLoader(string key, ISoundLoader loader)
        {
            if (string.IsNullOrEmpty(key) || loader == null)
            {
                Debug.LogWarning("[SoundRegistry] Invalid loader registration");
                return;
            }

            m_CustomLoaders[key] = loader;
        }

        /// <summary>
        /// Registriert eine SoundData im Cache.
        /// </summary>
        public void RegisterSoundData(SoundData data)
        {
            if (data == null || !data.IsValid())
            {
                Debug.LogWarning("[SoundRegistry] Invalid SoundData registration");
                return;
            }

            m_SoundCache[data.Id] = data;
        }

        /// <summary>
        /// Gibt SoundData nach Key zurueck. Nutzt Cache oder laedt aus registrierten Loadern.
        /// </summary>
        public SoundData GetSoundData(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (m_SoundCache.TryGetValue(key, out SoundData cached) && cached.IsValid() && cached.HasClip())
            {
                return cached;
            }

            if (TryLoadFromLoaders(key, out SoundData loaded))
            {
                return loaded;
            }

            return null;
        }

        /// <summary>
        /// Aktualisiert den AudioClip fuer einen bestehenden Cache-Eintrag.
        /// </summary>
        public void UpdateSoundData(string key, AudioClip clip)
        {
            if (m_SoundCache.TryGetValue(key, out SoundData data))
            {
                data.Clip = clip;
            }
        }

        /// <summary>
        /// Loescht alle gecachten Sounds und zerstoert AudioClips.
        /// </summary>
        public void ClearCache()
        {
            int destroyed = 0;
            foreach (SoundData data in m_SoundCache.Values)
            {
                if (data?.Clip != null)
                {
                    Object.Destroy(data.Clip);
                    destroyed++;
                }
            }

            Debug.Log($"[SoundRegistry] Cache {m_SoundCache.Count} cleared, destroyed {destroyed} clips");
            m_SoundCache.Clear();
        }

        private void RegisterDefaultLoaders()
        {
            RegisterLoader("lazy", new LazySoundLoader(this));
        }

        private bool TryLoadFromLoaders(string key, out SoundData soundData)
        {
            soundData = null;

            foreach (ISoundLoader loader in m_CustomLoaders.Values)
            {
                if (loader.CanLoad(key))
                {
                    SoundData loaded = loader.Load(key);
                    if (loaded != null)
                    {
                        soundData = loaded;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
