using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.SoundManagement
{
    /// <summary>
    /// Manager fuer SoF2-Sounds. Scannt das Sound-Verzeichnis, registriert .wav-Dateien
    /// im SoundRegistry und stellt AudioClips per SoF2-Pfad bereit.
    /// Analog zu TextureManager — Cache-first, Lifecycle via ServiceLocator.
    /// Sucht Sounds in StreamingAssets/sound/ (Build) und optional in einem externen Pfad (Dev).
    /// </summary>
    public class SoundManager
    {
        private readonly SoundRegistry m_Registry;

        /// <summary>Unterstuetzte Audio-Dateiformate.</summary>
        private readonly string[] m_SupportedExtensions = { ".wav" };

        /// <summary>
        /// Erstellt einen neuen SoundManager und scannt die Sound-Verzeichnisse.
        /// </summary>
        /// <param name="externalBasePath">Optionaler externer Pfad zum SoF2 base-Verzeichnis
        /// (z.B. "D:/sof2_extract/base"). Kann null sein.</param>
        public SoundManager(string externalBasePath = null)
        {
            m_Registry = new SoundRegistry();
            Initialize(externalBasePath);
            Debug.Log($"[SoundManager] Initialized. Registered {m_Registry.SoundCache.Count} sounds.");
        }

        /// <summary>
        /// Scannt Sound-Verzeichnisse und registriert alle .wav-Dateien.
        /// Priorisierung: StreamingAssets zuerst, dann externer Pfad.
        /// </summary>
        private void Initialize(string externalBasePath)
        {
            // 1. StreamingAssets/sound/ (Build-Pfad)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "sound");
            if (Directory.Exists(streamingPath))
            {
                ScanSoundDirectory(streamingPath);
            }

            // 2. Externer Pfad (Entwicklung: sof2_extract/base)
            if (!string.IsNullOrEmpty(externalBasePath) && Directory.Exists(externalBasePath))
            {
                ScanSoundDirectory(externalBasePath);
            }
        }

        /// <summary>
        /// Scannt ein Verzeichnis rekursiv nach .wav-Dateien.
        /// Keys werden als SoF2-relative Pfade gespeichert (z.B. "sound/weapons/frag_grenade/boom01.wav").
        /// </summary>
        private void ScanSoundDirectory(string basePath)
        {
            List<string> allFiles = new();

            foreach (string extension in m_SupportedExtensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(basePath, "*" + extension, SearchOption.AllDirectories);
                    allFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SoundManager] Error scanning for {extension} files: {ex.Message}");
                }
            }

            foreach (string filePath in allFiles)
            {
                string key = GetRelativePath(basePath, filePath).Replace('\\', '/');

                // Nur registrieren wenn noch nicht vorhanden (StreamingAssets hat Prioritaet)
                if (!m_Registry.SoundCache.ContainsKey(key))
                {
                    SoundData data = new(key, filePath, null);
                    m_Registry.RegisterSoundData(data);
                }
            }
        }

        /// <summary>
        /// Gibt einen AudioClip nach SoF2-Pfad zurueck (z.B. "sound/weapons/frag_grenade/boom01.wav").
        /// Laedt den Clip lazy bei erstem Zugriff.
        /// </summary>
        public AudioClip GetClip(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            SoundData data = m_Registry.GetSoundData(key);
            return data?.Clip;
        }

        /// <summary>
        /// Prueft ob ein Sound fuer den Key verfuegbar ist (ggf. noch nicht geladen).
        /// </summary>
        public bool HasSound(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return m_Registry.SoundCache.ContainsKey(key);
        }

        /// <summary>
        /// Loescht den Cache und alle geladenen AudioClips.
        /// </summary>
        public void ClearCache()
        {
            m_Registry?.ClearCache();
        }

        /// <summary>
        /// Berechnet den relativen Pfad von einer Datei zum Basis-Verzeichnis.
        /// </summary>
        private static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri fullUri = new(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString());
        }
    }
}
