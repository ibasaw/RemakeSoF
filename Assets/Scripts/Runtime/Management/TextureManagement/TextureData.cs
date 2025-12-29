using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Enum für die Quelle einer Texture.
    /// </summary>
    public enum TextureSource
    {
        System,   // Standard-Texturen aus Art/Assets
        Custom    // Vom Spieler hochgeladen oder zur Laufzeit erstellt
    }

    /// <summary>
    /// Repräsentiert eine einzelne Texture mit Metadata und Referenzzähler.
    /// Immutable für Thread-Safety.
    /// </summary>
    public class TextureData
    {
        // Properties
        public string Id { get; }
        public string FilePath { get; }
        public Texture2D Texture { get; internal set; }
        public Material Material { get; internal set; } // Optional zugehöriges Material
        public TextureSource Source { get; }
        public DateTime LoadedAt { get; }
        public int ReferenceCount { get; private set; }

        /// <summary>
        /// Alternative Schlüssel (Aliases) für diese Texture, z.B. verschiedene Namen oder Legacy-IDs.
        /// </summary>
        public System.Collections.Generic.List<string> AliasKeys { get; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Erstellt neue TextureData.
        /// </summary>
        /// <param name="id">Eindeutige ID der Texture</param>
        /// <param name="name">Display Name</param>
        /// <param name="texture">Die Texture2D</param>
        /// <param name="source">Quelle der Texture</param>
        public TextureData(string id, string filePath, Texture2D texture, Material material, TextureSource source)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id cannot be empty", nameof(id));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("FilePath cannot be empty", nameof(filePath));

            Id = id;
            FilePath = filePath;
            Texture = texture;
            Source = source;
            LoadedAt = DateTime.UtcNow;
            ReferenceCount = 0;
            Material = material;
        }

        /// <summary>
        /// Prüft, ob die TextureData gültig ist.
        /// </summary>
        public bool IsValid() => !string.IsNullOrEmpty(Id);

        public bool HasTexture() => Texture != null;

        /// <summary>
        /// Erhöht den Referenzzähler.
        /// </summary>
        public void IncrementReference()
        {
            ReferenceCount++;
        }

        /// <summary>
        /// Verringert den Referenzzähler.
        /// </summary>
        public void DecrementReference()
        {
            ReferenceCount = Mathf.Max(0, ReferenceCount - 1);
        }

        /// <summary>
        /// Entlädt die Texture und gibt Speicher frei.
        /// </summary>
        public void Unload()
        {
            if (Texture != null)
            {
                UnityEngine.Object.Destroy(Texture);
                Texture = null;
            }
            Debug.Log($"[TextureData] Unloaded texture: {Id}");
        }

        /// <summary>
        /// Gibt eine String-Repräsentation zurück.
        /// </summary>
        public override string ToString() => $"TextureData({Id}, {FilePath}, {Source}, Refs: {ReferenceCount})";
    }

    /// <summary>
    /// Factory für TextureData-Erstellung mit Validierung.
    /// </summary>
    public static class TextureDataFactory
    {
        /// <summary>
        /// Erstellt eine TextureData basierend auf Quelle.
        /// </summary>
        public static TextureData Create(string id, string name, Texture2D texture, Material material, TextureSource source)
        {
            return new TextureData(id, name, texture, material, source);
        }
    }
}
