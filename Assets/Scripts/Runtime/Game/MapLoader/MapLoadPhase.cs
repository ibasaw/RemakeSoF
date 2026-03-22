namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Phasen des Map-Ladeprozesses für Fortschrittsanzeige.
    /// </summary>
    public enum MapLoadPhase
    {
        /// <summary>
        /// Map-Laden gestartet, Prefab wird über Addressables geladen.
        /// </summary>
        Started,

        /// <summary>
        /// Prefab erfolgreich aus Addressables geladen.
        /// </summary>
        PrefabLoaded,

        /// <summary>
        /// Prefab in der Szene instanziiert.
        /// </summary>
        Instantiated,

        /// <summary>
        /// Collider auf allen Map-Elementen erstellt.
        /// </summary>
        CollidersApplied,

        /// <summary>
        /// Texturen auf Map-Elemente angewendet.
        /// </summary>
        TexturesApplied,

        /// <summary>
        /// Skybox aus skyParms-Daten erstellt und zugewiesen.
        /// </summary>
        SkyboxApplied,

        /// <summary>
        /// Map-Lichter (Point/Spot) aus light-Entities erstellt (nur Client).
        /// </summary>
        LightsApplied,

        /// <summary>
        /// Spawn-Points eingerichtet (nur Server).
        /// </summary>
        SpawnPointsReady,

        /// <summary>
        /// Map vollständig geladen und bereit.
        /// </summary>
        Complete,

        /// <summary>
        /// Map-Laden fehlgeschlagen.
        /// </summary>
        Failed
    }
}
