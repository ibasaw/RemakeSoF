using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

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

        /// <summary>AudioMixerGroup fuer SFX-Routing (z.B. MasterMixer/SFX).</summary>
        private AudioMixerGroup m_SfxGroup;

        /// <summary>AudioMixerGroup fuer Music-Routing (z.B. MasterMixer/Music).</summary>
        private AudioMixerGroup m_MusicGroup;

        /// <summary>AudioMixerGroup fuer SFX (Effect-Sounds, Weapon-Sounds etc.).</summary>
        public AudioMixerGroup SfxGroup => m_SfxGroup;

        /// <summary>AudioMixerGroup fuer Music.</summary>
        public AudioMixerGroup MusicGroup => m_MusicGroup;

        /// <summary>
        /// Setzt den AudioMixer und loest SFX/Music-Gruppen automatisch auf.
        /// </summary>
        public void SetMixer(UnityEngine.Audio.AudioMixer mixer)
        {
            if (mixer == null)
            {
                Debug.LogWarning("[SoundManager] No AudioMixer assigned.");
                return;
            }

            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            if (sfxGroups.Length > 0)
            {
                m_SfxGroup = sfxGroups[0];
            }
            else
            {
                Debug.LogWarning("[SoundManager] SFX group not found in mixer.");
            }

            AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups("Music");
            if (musicGroups.Length > 0)
            {
                m_MusicGroup = musicGroups[0];
            }
            else
            {
                Debug.LogWarning("[SoundManager] Music group not found in mixer.");
            }
        }

        /// <summary>Unterstuetzte Audio-Dateiformate.</summary>
        private readonly string[] m_SupportedExtensions = { ".wav", ".mp3" };

        /// <summary>Extension-Mapping fuer SoF2-Kompatibilitaet (.wav ↔ .mp3 Fallback).</summary>
        private static readonly Dictionary<string, string> s_ExtensionFallbacks = new()
        {
            { ".wav", ".mp3" },
            { ".mp3", ".wav" }
        };

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
            // 1. Assets/Art/sound/ (Editor & Build — primaerer Sound-Pfad)
            string artSoundPath = Path.Combine(Application.dataPath, "Art", "sound");
            if (Directory.Exists(artSoundPath))
            {
                string artBasePath = Path.Combine(Application.dataPath, "Art");
                ScanSoundDirectory(artBasePath, artSoundPath);
            }

            // 2. StreamingAssets/sound/ (Build-Pfad, Fallback)
            string streamingSoundPath = Path.Combine(Application.streamingAssetsPath, "sound");
            if (Directory.Exists(streamingSoundPath))
            {
                ScanSoundDirectory(Application.streamingAssetsPath, streamingSoundPath);
            }

            // 3. Externer Pfad (Entwicklung: sof2_extract/base)
            // Externer Pfad ist bereits das base-Verzeichnis, sound/ liegt darunter
            if (!string.IsNullOrEmpty(externalBasePath) && Directory.Exists(externalBasePath))
            {
                string externalSoundPath = Path.Combine(externalBasePath, "sound");
                if (Directory.Exists(externalSoundPath))
                {
                    ScanSoundDirectory(externalBasePath, externalSoundPath);
                }
            }
        }

        /// <summary>
        /// Scannt ein Verzeichnis rekursiv nach Audio-Dateien (.wav, .mp3).
        /// Keys werden als SoF2-relative Pfade gespeichert (z.B. "sound/weapons/frag_grenade/boom01.wav").
        /// Zusaetzlich werden Alias-Keys fuer Extension-Fallback registriert (.wav ↔ .mp3).
        /// keyBasePath bestimmt den Basis-Pfad fuer die Key-Berechnung, scanPath das Scan-Verzeichnis.
        /// </summary>
        private void ScanSoundDirectory(string keyBasePath, string scanPath)
        {
            List<string> allFiles = new();

            foreach (string extension in m_SupportedExtensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(scanPath, "*" + extension, SearchOption.AllDirectories);
                    allFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SoundManager] Error scanning for {extension} files: {ex.Message}");
                }
            }

            foreach (string filePath in allFiles)
            {
                string key = GetRelativePath(keyBasePath, filePath).Replace('\\', '/');

                // Nur registrieren wenn noch nicht vorhanden (StreamingAssets hat Prioritaet)
                if (!m_Registry.SoundCache.ContainsKey(key))
                {
                    SoundData data = new(key, filePath, null);
                    m_Registry.RegisterSoundData(data);
                }

                // SoF2-Alias: .mp3-Datei auch unter .wav-Key registrieren (und umgekehrt)
                // damit JSON-Referenzen mit .wav auf .mp3-Dateien matchen
                string extension = Path.GetExtension(key).ToLowerInvariant();
                if (s_ExtensionFallbacks.TryGetValue(extension, out string altExtension))
                {
                    string aliasKey = Path.ChangeExtension(key, altExtension);
                    if (!m_Registry.SoundCache.ContainsKey(aliasKey))
                    {
                        SoundData aliasData = new(aliasKey, filePath, null);
                        m_Registry.RegisterSoundData(aliasData);
                    }
                }
            }
        }

        /// <summary>
        /// Gibt einen AudioClip nach SoF2-Pfad zurueck (z.B. "sound/weapons/frag_grenade/boom01.wav").
        /// Laedt den Clip lazy bei erstem Zugriff.
        /// Unterstuetzt Keys mit und ohne Extension — probiert automatisch .wav und .mp3 Fallbacks.
        /// </summary>
        public AudioClip GetClip(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            // Direkter Lookup (Key mit Extension)
            SoundData data = m_Registry.GetSoundData(key);
            if (data != null)
            {
                return data.Clip;
            }

            // Fallback: Key ohne Extension → .wav und .mp3 probieren
            if (!Path.HasExtension(key))
            {
                SoundData wavData = m_Registry.GetSoundData(key + ".wav");
                if (wavData != null)
                {
                    return wavData.Clip;
                }

                SoundData mp3Data = m_Registry.GetSoundData(key + ".mp3");
                if (mp3Data != null)
                {
                    return mp3Data.Clip;
                }
            }

            return null;
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
        /// Gibt einen zufaelligen AudioClip fuer einen SoF2-Basispfad mit Nummern-Suffixen zurueck.
        /// SoF2-Konvention: "sound/player/steps/concrete/concrete" → concrete0.wav, concrete1.wav, etc.
        /// Probiert Suffixe 0-7 mit .wav und .mp3 und waehlt zufaellig einen der gefundenen Clips.
        /// </summary>
        public AudioClip GetNumberedClip(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            // Varianten sammeln (max 8 Nummern × 2 Extensions)
            List<string> found = new();
            for (int i = 0; i < 8; i++)
            {
                string wavKey = basePath + i + ".wav";
                if (m_Registry.SoundCache.ContainsKey(wavKey))
                {
                    found.Add(wavKey);
                    continue;
                }

                string mp3Key = basePath + i + ".mp3";
                if (m_Registry.SoundCache.ContainsKey(mp3Key))
                {
                    found.Add(mp3Key);
                }
            }

            if (found.Count == 0)
            {
                // Fallback: manche SoF2-Sounds haben keine Nummern-Suffixe (z.B. Landing-Sounds)
                string wavDirect = basePath + ".wav";
                if (m_Registry.SoundCache.ContainsKey(wavDirect))
                {
                    return GetClip(wavDirect);
                }

                string mp3Direct = basePath + ".mp3";
                if (m_Registry.SoundCache.ContainsKey(mp3Direct))
                {
                    return GetClip(mp3Direct);
                }

                return null;
            }

            string chosen = found[UnityEngine.Random.Range(0, found.Count)];
            return GetClip(chosen);
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
