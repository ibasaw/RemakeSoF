namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Observer Interface für Cache-Änderungen.
    /// </summary>
    public interface ITextureRegistryObserver
    {
        /// <summary>
        /// Aufgerufen wenn eine Texture registriert wurde.
        /// </summary>
        void OnTextureRegistered(string key, TextureData textureData);

        /// <summary>
        /// Aufgerufen wenn eine Texture entfernt wurde.
        /// </summary>
        void OnTextureUnregistered(string key);

        /// <summary>
        /// Aufgerufen wenn der Cache geleert wurde.
        /// </summary>
        void OnCacheCleared();

        /// <summary>
        /// Aufgerufen wenn ein Loader registriert wurde.
        /// </summary>
        void OnLoaderRegistered(string key);
    }
}