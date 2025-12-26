using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Interface für benutzerdefinierte Texture-Loader.
    /// Implementieren Sie dieses Interface, um eigene Lade-Strategien zu definieren.
    /// </summary>
    public interface ITextureLoader
    {
        /// <summary>
        /// Prüft, ob dieser Loader den angegebenen Key laden kann.
        /// </summary>
        /// <param name="key">Der zu prüfende Texture-Schlüssel</param>
        /// <returns>True, wenn dieser Loader den Key laden kann</returns>
        bool CanLoad(string key);

        /// <summary>
        /// Lädt die Texture für den angegebenen Key.
        /// </summary>
        /// <param name="key">Der Texture-Schlüssel</param>
        /// <param name="source">Die Quelle der Texture</param>
        /// <returns>Die geladene Texture oder null bei Fehler</returns>
        TextureData Load(string key);
    }
}
