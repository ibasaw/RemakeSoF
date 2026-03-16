using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.SoundManagement
{
    /// <summary>
    /// Interface fuer benutzerdefinierte Sound-Loader.
    /// Analog zu ITextureLoader — implementiert Lade-Strategien fuer AudioClips.
    /// </summary>
    public interface ISoundLoader
    {
        /// <summary>
        /// Prueft ob dieser Loader den angegebenen Key laden kann.
        /// </summary>
        bool CanLoad(string key);

        /// <summary>
        /// Laedt den Sound fuer den angegebenen Key.
        /// </summary>
        SoundData Load(string key);
    }
}
