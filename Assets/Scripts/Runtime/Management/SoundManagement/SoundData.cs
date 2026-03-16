using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.SoundManagement
{
    /// <summary>
    /// Repraesentiert einen einzelnen Sound-Eintrag mit AudioClip und Metadaten.
    /// Analog zu TextureData fuer das Texture-System.
    /// </summary>
    public class SoundData
    {
        /// <summary>Eindeutige ID (SoF2-Pfad, z.B. "sound/weapons/frag_grenade/boom01.wav").</summary>
        public string Id { get; }

        /// <summary>Dateipfad auf Disk (nur fuer Custom-Sounds relevant).</summary>
        public string FilePath { get; }

        /// <summary>Geladener AudioClip (kann null sein bis lazy-geladen).</summary>
        public AudioClip Clip { get; internal set; }

        /// <summary>Zeitpunkt des Ladens.</summary>
        public DateTime LoadedAt { get; }

        /// <summary>
        /// Erstellt eine neue SoundData-Instanz.
        /// </summary>
        public SoundData(string id, string filePath, AudioClip clip)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be empty", nameof(id));
            }

            Id = id;
            FilePath = filePath ?? string.Empty;
            Clip = clip;
            LoadedAt = DateTime.UtcNow;
        }

        /// <summary>Prueft ob die SoundData gueltig ist.</summary>
        public bool IsValid() => !string.IsNullOrEmpty(Id);

        /// <summary>Prueft ob ein AudioClip geladen ist.</summary>
        public bool HasClip() => Clip != null;
    }
}
