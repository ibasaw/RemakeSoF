using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Effects;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Spielt Footstep- und Landing-Sounds basierend auf dem Surface-Typ unter dem Spieler ab.
    /// Wird auf dem Animator-GameObject platziert damit AnimationEvents direkt empfangen werden.
    /// Surface-Erkennung per Raycast nach unten vom Player-Root; Sound-Auswahl per SoF2_data_per_surface.json.
    /// SoF2-Sound-Konvention: Basispfad + Nummer (z.B. "sound/player/steps/concrete/concrete" → concrete0.wav).
    /// </summary>
    public class ClientFootstepHandler : MonoBehaviour
    {
        /// <summary>Raycast-Distanz fuer Surface-Erkennung unter dem Spieler.</summary>
        private const float SURFACE_RAYCAST_DIST = 2f;

        /// <summary>Footstep-Interval bei Run-Geschwindigkeit (Sekunden pro Schritt).</summary>
        private const float RUN_STEP_INTERVAL = 0.35f;

        /// <summary>Footstep-Interval bei Walk-Geschwindigkeit (Sekunden pro Schritt).</summary>
        private const float WALK_STEP_INTERVAL = 0.55f;

        /// <summary>Minimale horizontale Geschwindigkeit fuer Footstep-Ausloesung.</summary>
        private const float MIN_SPEED_FOR_FOOTSTEP = 0.5f;

        /// <summary>Layer-Maske fuer Boden-Raycast (alles ausser Hitbox).</summary>
        private int m_GroundLayerMask;

        /// <summary>Player-Root-Transform fuer Raycast-Ursprung und Sound-Position.</summary>
        private Transform m_PlayerRoot;

        /// <summary>Gecachte Referenzen fuer Performance.</summary>
        private SoundManager m_SoundManager;
        private SurfaceImpactDataLoader m_SurfaceLoader;
        private EffectFactory m_EffectFactory;
        private AudioMixerGroup m_SfxGroup;

        /// <summary>Alterniert links/rechts fuer Footstep-Decals.</summary>
        private bool m_IsLeftFoot;

        /// <summary>Zuletzt getroffener Collider fuer Surface-Cache.</summary>
        private Collider m_LastSurfaceCollider;

        /// <summary>Gecachter SurfaceTypeMarker des letzten Colliders.</summary>
        private SurfaceTypeMarker m_LastSurfaceMarker;

        /// <summary>Timer fuer code-basierte Footstep-Ausloesung.</summary>
        private float m_FootstepTimer;

        /// <summary>Walk-Taste gedrueckt — wird vom ClientPlayerCharacter gesetzt.</summary>
        public bool IsWalking { get; set; }

        /// <summary>Aktuelle Fallhoehe (FullFallHeight) — wird vom ClientPlayerCharacter gesetzt.</summary>
        public float FallHeight { get; set; }

        /// <summary>Ob der Spieler am Boden ist — wird vom ClientPlayerCharacter gesetzt.</summary>
        public bool IsGrounded { get; set; }

        /// <summary>Horizontale Geschwindigkeit des Spielers — wird vom ClientPlayerCharacter gesetzt.</summary>
        public float HorizontalSpeed { get; set; }

        /// <summary>
        /// Initialisiert den Handler mit Referenz zum Player-Root.
        /// Muss nach AddComponent aufgerufen werden.
        /// </summary>
        public void Initialize(Transform playerRoot)
        {
            m_PlayerRoot = playerRoot;
            int hitboxLayer = LayerMask.GetMask("Hitbox");
            m_GroundLayerMask = ~(hitboxLayer | LayerMask.GetMask("BrushCollision"));
        }

        /// <summary>
        /// Cacht Service-Referenzen (lazy, da Services erst nach Awake verfuegbar sein koennen).
        /// </summary>
        private void EnsureServices()
        {
            m_SoundManager ??= ServiceLocator.Get<SoundManager>();
            m_SurfaceLoader ??= ServiceLocator.Get<SurfaceImpactDataLoader>();
            m_EffectFactory ??= ServiceLocator.Get<EffectFactory>();

            if (m_SfxGroup == null && m_SoundManager != null)
            {
                m_SfxGroup = m_SoundManager.SfxGroup;
            }
        }

        /// <summary>
        /// Code-basierte Footstep-Ausloesung per Timer.
        /// Ersetzt AnimationEvents, da die FBX-Animationen keine Events definiert haben.
        /// Interval wird dynamisch angepasst: Run = schneller, Walk = langsamer.
        /// </summary>
        private void Update()
        {
            if (!IsGrounded || HorizontalSpeed < MIN_SPEED_FOR_FOOTSTEP)
            {
                m_FootstepTimer = 0f;
                return;
            }

            float interval = IsWalking ? WALK_STEP_INTERVAL : RUN_STEP_INTERVAL;
            m_FootstepTimer += Time.deltaTime;

            if (m_FootstepTimer >= interval)
            {
                m_FootstepTimer -= interval;
                PlayFootstep(IsWalking);
            }
        }

        /// <summary>
        /// AnimationEvent: Footstep — wird von Walk/Run-Animationen auf dem Animator-GO ausgeloest.
        /// </summary>
        private void OnFootstep(AnimationEvent animationEvent)
        {
            PlayFootstep(IsWalking);
        }

        /// <summary>
        /// AnimationEvent: Land — wird von der Landing-Animation auf dem Animator-GO ausgeloest.
        /// Nutzt FallHeight fuer die Unterscheidung land/land_pain/land_death.
        /// </summary>
        private void OnLand(AnimationEvent animationEvent)
        {
            if (FallHeight > 10f)
            {
                PlayLanding("land_death");
            }
            else if (FallHeight > 3f)
            {
                PlayLanding("land_pain");
            }
            else
            {
                PlayLanding("land");
            }
        }

        /// <summary>
        /// Spielt einen Footstep-Sound ab.
        /// isWalking: true fuer footstepStealth (Walk), false fuer footstep (Run).
        /// </summary>
        public void PlayFootstep(bool isWalking)
        {
            EnsureServices();
            if (m_SoundManager == null || m_SurfaceLoader == null)
            {
                return;
            }

            string surfaceType = DetectSurfaceBelow(out RaycastHit surfaceHit);
            string fieldName = isWalking ? "footstepStealth" : "footstep";
            string basePath = m_SurfaceLoader.GetSurfaceSoundPath(surfaceType, fieldName);

            if (string.IsNullOrEmpty(basePath))
            {
                return;
            }

            AudioClip clip = m_SoundManager.GetNumberedClip(basePath);
            PlayClipAtFeet(clip);

            // Footstep-Decal auf dem Boden spawnen
            if (m_EffectFactory != null && surfaceHit.collider != null)
            {
                string decalPath = m_SurfaceLoader.GetSurfaceDecalPath(surfaceType, fieldName);
                if (!string.IsNullOrEmpty(decalPath))
                {
                    float yaw = m_PlayerRoot != null ? m_PlayerRoot.eulerAngles.y : 0f;
                    m_EffectFactory.SpawnFootstepDecal(surfaceHit.point, surfaceHit.normal, decalPath, yaw, m_IsLeftFoot);
                    m_IsLeftFoot = !m_IsLeftFoot;
                }
            }
        }

        /// <summary>
        /// Spielt einen Landing-Sound ab.
        /// landType: "land" (leicht), "land_pain" (mittel), "land_death" (toedlich).
        /// </summary>
        public void PlayLanding(string landType)
        {
            EnsureServices();
            if (m_SoundManager == null || m_SurfaceLoader == null)
            {
                return;
            }

            string surfaceType = DetectSurfaceBelow(out RaycastHit surfaceHit);
            string basePath = m_SurfaceLoader.GetSurfaceSoundPath(surfaceType, landType);

            if (string.IsNullOrEmpty(basePath))
            {
                return;
            }

            AudioClip clip = m_SoundManager.GetNumberedClip(basePath);
            PlayClipAtFeet(clip);

            // Schwere Landung hinterlaesst ebenfalls ein Decal
            if (m_EffectFactory != null && surfaceHit.collider != null)
            {
                string decalField = landType == "land_pain" || landType == "land_death" ? "footstep" : "";
                string decalPath = !string.IsNullOrEmpty(decalField)
                    ? m_SurfaceLoader.GetSurfaceDecalPath(surfaceType, decalField)
                    : "";
                if (!string.IsNullOrEmpty(decalPath))
                {
                    float yaw = m_PlayerRoot != null ? m_PlayerRoot.eulerAngles.y : 0f;
                    m_EffectFactory.SpawnFootstepDecal(surfaceHit.point, surfaceHit.normal, decalPath, yaw, m_IsLeftFoot);
                    m_IsLeftFoot = !m_IsLeftFoot;
                }
            }
        }

        /// <summary>
        /// Erkennt den Surface-Typ unter dem Spieler per Raycast vom Player-Root.
        /// Liest SurfaceTypeMarker vom getroffenen Collider.
        /// Gibt zusaetzlich den RaycastHit fuer Decal-Platzierung zurueck.
        /// </summary>
        private string DetectSurfaceBelow(out RaycastHit surfaceHit)
        {
            Vector3 origin = m_PlayerRoot != null ? m_PlayerRoot.position : transform.position;

            if (Physics.Raycast(origin + Vector3.up * 0.1f, Vector3.down,
                out surfaceHit, SURFACE_RAYCAST_DIST, m_GroundLayerMask))
            {
                if (surfaceHit.collider != m_LastSurfaceCollider)
                {
                    m_LastSurfaceCollider = surfaceHit.collider;
                    m_LastSurfaceMarker = surfaceHit.collider.GetComponentInParent<SurfaceTypeMarker>();
                }

                if (m_LastSurfaceMarker != null)
                {
                    return m_LastSurfaceMarker.SurfaceType;
                }
            }

            return "default";
        }

        /// <summary>
        /// Spielt einen AudioClip als 3D-Sound an den Fuessen des Spielers ab.
        /// Routet ueber die SFX MixerGroup.
        /// </summary>
        private void PlayClipAtFeet(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            Vector3 feetPos = m_PlayerRoot != null ? m_PlayerRoot.position : transform.position;

            GameObject soundObj = new("FootstepSound");
            soundObj.transform.position = feetPos;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (m_SfxGroup != null)
            {
                source.outputAudioMixerGroup = m_SfxGroup;
            }

            source.Play();
            Destroy(soundObj, clip.length + 0.1f);
        }
    }
}
