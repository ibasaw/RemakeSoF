using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Zeigt den Ladefortschritt der Map an.
    /// Wird in der Game Scene angezeigt, bis die Map vollständig geladen ist.
    /// </summary>
    internal class MapLoadingView : View<GameApplication>
    {
        /// <summary>
        /// Status-Label zeigt die aktuelle Ladephase an.
        /// </summary>
        Label m_StatusLabel;

        /// <summary>
        /// Fortschrittsbalken-Füllung (Breite wird prozentual gesetzt).
        /// </summary>
        VisualElement m_ProgressBarFill;

        /// <summary>
        /// UIDocument für diese View.
        /// </summary>
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;
            m_StatusLabel = root.Q<Label>("statusLabel");
            m_ProgressBarFill = root.Q<VisualElement>("progressBarFill");
        }

        /// <summary>
        /// Aktualisiert die Fortschrittsanzeige basierend auf der aktuellen Ladephase.
        /// </summary>
        /// <param name="phase">Die aktuelle Map-Ladephase.</param>
        internal void OnProgressChanged(MapLoadPhase phase)
        {
            float progress = GetProgressForPhase(phase);
            string status = GetStatusText(phase);

            m_StatusLabel.text = status;
            m_ProgressBarFill.style.width = Length.Percent(progress * 100f);
        }

        /// <summary>
        /// Gibt den normalisierten Fortschritt (0-1) für eine Ladephase zurück.
        /// </summary>
        private float GetProgressForPhase(MapLoadPhase phase)
        {
            return phase switch
            {
                MapLoadPhase.Started => 0.1f,
                MapLoadPhase.PrefabLoaded => 0.4f,
                MapLoadPhase.Instantiated => 0.6f,
                MapLoadPhase.CollidersApplied => 0.75f,
                MapLoadPhase.TexturesApplied => 0.85f,
                MapLoadPhase.SkyboxApplied => 0.9f,
                MapLoadPhase.SpawnPointsReady => 0.95f,
                MapLoadPhase.Complete => 1f,
                MapLoadPhase.Failed => 0f,
                _ => 0f
            };
        }

        /// <summary>
        /// Gibt den Statustext für eine Ladephase zurück.
        /// </summary>
        private string GetStatusText(MapLoadPhase phase)
        {
            return phase switch
            {
                MapLoadPhase.Started => "Loading map data...",
                MapLoadPhase.PrefabLoaded => "Map data loaded",
                MapLoadPhase.Instantiated => "Building map...",
                MapLoadPhase.CollidersApplied => "Creating collision...",
                MapLoadPhase.TexturesApplied => "Applying textures...",
                MapLoadPhase.SkyboxApplied => "Creating skybox...",
                MapLoadPhase.SpawnPointsReady => "Preparing spawn points...",
                MapLoadPhase.Complete => "Ready!",
                MapLoadPhase.Failed => "Failed to load map!",
                _ => ""
            };
        }
    }
}
