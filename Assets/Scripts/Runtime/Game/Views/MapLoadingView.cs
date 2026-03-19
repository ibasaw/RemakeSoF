using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Zeigt den Ladefortschritt der Map an.
    /// Wird in der Game Scene angezeigt, bis die Map vollständig geladen ist.
    /// Unterstuetzt LevelShot-Hintergrund und sanfte Fortschrittsbalken-Animation.
    /// Phasen werden in eine Queue eingereiht und nacheinander smooth animiert.
    /// </summary>
    internal class MapLoadingView : View<GameApplication>
    {
        /// <summary>Status-Label zeigt die aktuelle Ladephase an.</summary>
        Label m_StatusLabel;

        /// <summary>Titel-Label zeigt den Map-Namen an.</summary>
        Label m_TitleLabel;

        /// <summary>Fortschrittsbalken-Füllung (Breite wird prozentual gesetzt).</summary>
        VisualElement m_ProgressBarFill;

        /// <summary>Hintergrund-Element fuer das LevelShot-Bild.</summary>
        VisualElement m_Background;

        /// <summary>UIDocument für diese View.</summary>
        UIDocument m_UIDocument;

        /// <summary>Aktueller Fortschrittswert (0-1) fuer sanfte Animation.</summary>
        float m_CurrentProgress;

        /// <summary>Ziel-Fortschrittswert des aktuellen Segments (0-1).</summary>
        float m_SegmentTarget;

        /// <summary>Queue der eingegangenen Phasen die noch animiert werden muessen.</summary>
        readonly Queue<MapLoadPhase> m_PhaseQueue = new();

        /// <summary>Geschwindigkeit der Fortschrittsbalken-Animation (Einheiten pro Sekunde).</summary>
        const float k_ProgressSpeed = 0.55f;

        /// <summary>Minimale Anzeigedauer des Ladescreens in Sekunden.</summary>
        const float k_MinDisplayTime = 3f;

        /// <summary>Zeitpunkt zu dem der Ladescreen angezeigt wurde (fuer Mindestanzeigedauer).</summary>
        float m_ShowTimestamp;

        /// <summary>Ob das Laden abgeschlossen ist und nur noch auf Mindestzeit gewartet wird.</summary>
        bool m_LoadComplete;

        /// <summary>Callback wenn der Ladescreen nach Mindestzeit geschlossen werden kann.</summary>
        internal event Action OnReadyToHide;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;
            m_StatusLabel = root.Q<Label>("statusLabel");
            m_TitleLabel = root.Q<Label>("titleLabel");
            m_ProgressBarFill = root.Q<VisualElement>("progressBarFill");
            m_Background = root.Q<VisualElement>("mapLoading");
        }

        void Update()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            // Zum aktuellen Segment-Ziel animieren
            if (m_CurrentProgress < m_SegmentTarget)
            {
                m_CurrentProgress = Mathf.MoveTowards(m_CurrentProgress, m_SegmentTarget, Time.deltaTime * k_ProgressSpeed);
                m_ProgressBarFill.style.width = Length.Percent(m_CurrentProgress * 100f);
            }

            // Segment erreicht — naechste Phase aus der Queue holen
            if (m_CurrentProgress >= m_SegmentTarget - 0.001f && m_PhaseQueue.Count > 0)
            {
                MapLoadPhase nextPhase = m_PhaseQueue.Dequeue();
                m_SegmentTarget = GetProgressForPhase(nextPhase);
                m_StatusLabel.text = GetStatusText(nextPhase);

                if (nextPhase == MapLoadPhase.Complete)
                {
                    m_LoadComplete = true;
                }
            }

            // Wenn Laden fertig: warten bis Bar voll UND Mindestzeit erreicht
            if (m_LoadComplete && m_CurrentProgress >= 0.999f && Time.time - m_ShowTimestamp >= k_MinDisplayTime)
            {
                m_LoadComplete = false;
                m_CurrentProgress = 1f;
                m_ProgressBarFill.style.width = Length.Percent(100f);
                OnReadyToHide?.Invoke();
            }
        }

        /// <summary>
        /// Aktualisiert die Fortschrittsanzeige basierend auf der aktuellen Ladephase.
        /// Der Balken animiert sanft zum neuen Wert.
        /// </summary>
        /// <param name="phase">Die aktuelle Map-Ladephase.</param>
        internal void OnProgressChanged(MapLoadPhase phase)
        {
            // Erste Phase direkt als Segment-Ziel setzen, weitere in Queue einreihen
            if (m_PhaseQueue.Count == 0 && m_CurrentProgress >= m_SegmentTarget - 0.001f)
            {
                m_SegmentTarget = GetProgressForPhase(phase);
                m_StatusLabel.text = GetStatusText(phase);

                if (phase == MapLoadPhase.Complete)
                {
                    m_LoadComplete = true;
                }
            }
            else
            {
                m_PhaseQueue.Enqueue(phase);
            }
        }

        /// <summary>
        /// Setzt den LevelShot-Hintergrund und den Map-Namen fuer den Ladescreen.
        /// </summary>
        /// <param name="mapDef">Die MapDefinition mit LevelShot-Pfad und Map-Name.</param>
        internal void SetMapInfo(MapDefinition mapDef)
        {
            if (mapDef == null)
            {
                return;
            }

            // Map-Name als Titel setzen
            if (m_TitleLabel != null)
            {
                m_TitleLabel.text = $"Loading {mapDef.mapName}";
            }

            // LevelShot-Hintergrund laden
            if (m_Background != null && !string.IsNullOrEmpty(mapDef.levelShotBackgroundTexturePath))
            {
                TextureManager textureManager = ServiceLocator.Get<TextureManager>();
                if (textureManager != null)
                {
                    TextureData textureData = textureManager.GetTextureData(mapDef.levelShotBackgroundTexturePath);
                    if (textureData?.Texture != null)
                    {
                        m_Background.style.backgroundImage = new StyleBackground(textureData.Texture);
                    }
                }
            }

            // Fortschritt zuruecksetzen fuer neue Map
            m_CurrentProgress = 0f;
            m_SegmentTarget = 0f;
            m_LoadComplete = false;
            m_ShowTimestamp = Time.time;
            m_PhaseQueue.Clear();
            if (m_ProgressBarFill != null)
            {
                m_ProgressBarFill.style.width = Length.Percent(0f);
            }
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
