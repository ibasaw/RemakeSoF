using System.Collections;
using Newtonsoft.Json.Linq;
using Tolik.RemakeSoF.Runtime.AI;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Networked AI-Character. Server-owned (kein Owner-Client).
    /// Server steuert Position/Rotation direkt, Clients interpolieren.
    /// Nutzt dieselbe NetworkedCharacterState fuer Skin, Health, Team etc.
    /// Hat ClientHitboxSystem fuer Bone-basierte Trefferkennung (analog zu Spielern).
    /// Hat ServerCharacterController fuer Damage/Death/Respawn-Logik.
    /// Animation wird server-seitig berechnet und per NetworkVariable synchronisiert.
    /// </summary>
    public class NetworkedAICharacter : NetworkedCharacter, ICharacter
    {
        /// <summary>
        /// Server-seitige AI-Logik-Komponente.
        /// </summary>
        [SerializeField]
        private ServerAICharacter m_ServerAICharacter;

        /// <summary>
        /// Client-seitige Skin/Visual-Komponente (fuer Remote-Rendering).
        /// </summary>
        [SerializeField]
        private ClientCharacterSkinHandler m_SkinHandler;

        /// <summary>
        /// NetworkedCharacterState-Referenz fuer Identity, Health, Team etc.
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        /// <summary>
        /// Server-seitiger Damage/Death/Respawn-Controller (analog zu Spielern).
        /// </summary>
        [SerializeField]
        private ServerCharacterController m_ServerCharacterController;

        /// <summary>
        /// Client-seitiges Hitbox-System fuer Bone-basierte Trefferkennung.
        /// Wird nach Visual-Instanziierung aufgebaut (29 BoxCollider auf Skeleton-Bones).
        /// </summary>
        [SerializeField]
        private ClientHitboxSystem m_HitboxSystem;

        /// <summary>
        /// Client-seitiges Collider-System fuer physische Kollision (SoF2 AABB).
        /// Erzeugt identischen BoxCollider wie bei Spielern (draufspringen, Kollision etc.).
        /// Wird nur auf Clients initialisiert (Server hat BoxCollider via ServerAICharacter).
        /// </summary>
        [SerializeField]
        private ClientColliderSystem m_ColliderSystem;

        /// <summary>
        /// Netzwerksynchronisierter Animations-State. Server schreibt, Clients lesen.
        /// </summary>
        private NetworkVariable<NetworkAnimationState> m_AnimationState = new();

        /// <summary>Animator des instanziierten Character-Visuals.</summary>
        private Animator m_Animator;

        /// <summary>Letzte Attack-Sequenznummer fuer Re-Trigger-Erkennung.</summary>
        private byte m_LastAttackSequence;

        /// <summary>Footstep-Handler fuer Schritt- und Lande-Sounds (auf Animator-GO).</summary>
        private ClientFootstepHandler m_FootstepHandler;

        /// <summary>Root des Visual-Prefabs fuer Y-Offset-Korrektur.</summary>
        private Transform m_VisualInstance;

        /// <summary>skeleton_root Bone fuer XZ-Drift-Reset.</summary>
        private Transform m_SkeletonRoot;

        /// <summary>model_root Bone fuer XZ-Drift-Reset.</summary>
        private Transform m_ModelRoot;

        /// <summary>pelvis Bone fuer XZ-Drift-Reset auf Bind-Pose.</summary>
        private Transform m_PelvisTarget;

        /// <summary>Linker Fuss-Bone fuer Y-Offset-Berechnung.</summary>
        private Transform m_LeftFoot;

        /// <summary>Rechter Fuss-Bone fuer Y-Offset-Berechnung.</summary>
        private Transform m_RightFoot;

        /// <summary>Bind-Pose Referenz fuer Pelvis-XZ (einmalig erfasst).</summary>
        private Vector3 m_PelvisBindLocalPos;

        /// <summary>Flag ob Bind-Pose-Referenz erfasst wurde.</summary>
        private bool m_HasBindPoseReference;

        /// <summary>Lower Lumbar Bone-Transform (unterer Torso).</summary>
        private Transform m_LowerLumbar;

        /// <summary>Upper Lumbar Bone-Transform (oberer Torso).</summary>
        private Transform m_UpperLumbar;

        // ── Pelvis / Legs-Facing State ──────────────────────────

        /// <summary>Gesmoothed Legs-Forward Vektor (Slerp-basiert).</summary>
        private Vector3 m_SmoothedLegsForward = Vector3.forward;

        /// <summary>Index der letzten Bewegungsrichtung (0-7, SoF2 PM_SetMovementDir Logik).</summary>
        private int m_LastMoveDirIndex;

        /// <summary>Legs-Yaw-Offset in Grad (Skeleton-Korrektur bei Bewegung).</summary>
        private const float k_LegsYawOffsetDegrees = 90f;

        /// <summary>Idle-Yaw-Offsets pro Bewegungsrichtung (SoF2 PM_SetMovementDir Index 0-7).</summary>
        private static readonly float[] s_IdleYawByDir = { 112f, 45f, 68f, 68f, 112f, 180f, 180f, 90f };

        /// <summary>Smoothing-Faktor fuer Legs-Rotation bei Bewegung.</summary>
        private const float k_BaseLegsRotationSmooth = 8f;

        /// <summary>Smoothing-Einfluss fuer Torso-Follow im Idle.</summary>
        private const float k_TorsoFollowYawInfluence = 65f;

        // ── Lumbar Rotation State ───────────────────────────────

        /// <summary>Gesmoothed Lean-State (x=Roll, y=Pitch).</summary>
        private Vector2 m_CurrentLeanAngles;

        /// <summary>Gesmoothed Upper Lumbar Yaw Offset.</summary>
        private float m_CurrentUpperLumbarYaw;

        /// <summary>Gesmoothed Lower Lumbar Yaw Offset.</summary>
        private float m_CurrentLowerLumbarYaw;

        /// <summary>Gesmoothed Upper Lumbar Pitch Offset.</summary>
        private float m_CurrentUpperLumbarPitch;

        /// <summary>Gesmoothed Lower Lumbar Pitch Offset.</summary>
        private float m_CurrentLowerLumbarPitch;

        /// <summary>Gesmoothed Idle-Offset basierend auf letzter Bewegungsrichtung.</summary>
        private float m_CurrentMovementIdleOffset;

        /// <summary>Upper Lumbar Yaw-Offset (Grad).</summary>
        private const float k_UpperLumbarYawOffset = -5f;

        /// <summary>Lower Lumbar Yaw-Offset (Grad).</summary>
        private const float k_LowerLumbarYawOffset = 0f;

        /// <summary>Lumbar Yaw Smoothing-Faktor.</summary>
        private const float k_LumbarYawSmooth = 8f;

        /// <summary>Upper Lumbar Pitch-Offset (Grad).</summary>
        private const float k_UpperLumbarPitchOffset = 0f;

        /// <summary>Lower Lumbar Pitch-Offset (Grad).</summary>
        private const float k_LowerLumbarPitchOffset = 0f;

        /// <summary>Lumbar Pitch Smoothing-Faktor.</summary>
        private const float k_LumbarPitchSmooth = 8f;

        /// <summary>Roll-Lean in Grad bei Seitwärtsbewegung.</summary>
        private const float k_RollLeanDegrees = 25f;

        /// <summary>Pitch-Lean in Grad bei Vorwärts-/Rückwärtsbewegung.</summary>
        private const float k_PitchLeanDegrees = 20f;

        /// <summary>Lean Smoothing-Faktor.</summary>
        private const float k_LeanSmooth = 8f;

        /// <summary>Strafe-Yaw-Twist in Grad.</summary>
        private const float k_StrafeYawDegrees = 12f;

        /// <summary>Movement-Idle-Offsets pro Richtung (SoF2 PM_SetMovementDir Index 0-7).</summary>
        private static readonly int[] s_MovementOffsets = { 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>Movement-Idle-Offset Smoothing-Faktor.</summary>
        private const float k_MovementOffsetSmooth = 6f;

        /// <summary>Skeleton-Offset Rotation (90 Grad Y-Korrektur fuer SoF2-Modelle).</summary>
        private static readonly Quaternion s_SkeletonOffset = Quaternion.Euler(0f, 90f, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Client-seitige Sensor-Visualisierung (nur Debug, beeinflusst NN nicht).</summary>
        private SensorArrayGenerator m_ClientDebugSensorArray;

        /// <summary>Client-seitige Pfad-Linie (Cyan): NavMesh-Pfadpunkte.</summary>
        private LineRenderer m_ClientPathLine;

        /// <summary>Client-seitige Steuer-Linie (Gelb): Bot zum aktuellen Wegpunkt.</summary>
        private LineRenderer m_ClientSteerLine;

        /// <summary>Client-seitige Ziel-Linie (Rot): Vertikale Markierung am Ziel-Checkpoint.</summary>
        private LineRenderer m_ClientTargetLine;

        /// <summary>Gecachter Shader fuer Client-seitige Pfad-LineRenderer.</summary>
        private static Shader s_CachedClientLineShader;

        /// <summary>Empfangene Pfadpunkte vom Server (fuer Client-Rendering).</summary>
        private Vector3[] m_ClientPathCorners = System.Array.Empty<Vector3>();

        /// <summary>Empfangene Bot-Position fuer Steuer-Linie.</summary>
        private Vector3 m_ClientSteerStart;

        /// <summary>Empfangener aktueller Wegpunkt fuer Steuer-Linie.</summary>
        private Vector3 m_ClientSteerEnd;

        /// <summary>Empfangene Ziel-Position (Checkpoint) fuer Ziel-Linie.</summary>
        private Vector3 m_ClientTargetPos;

        /// <summary>Ob Ziel-Position gueltig ist.</summary>
        private bool m_ClientHasTarget;
#endif

        /// <summary>Animator-Parameter Hash: IsMoving (bool).</summary>
        private static readonly int s_IsMovingHash = Animator.StringToHash("IsMoving");

        /// <summary>Animator-Parameter Hash: Speed (float).</summary>
        private static readonly int s_SpeedHash = Animator.StringToHash("Speed");

        /// <summary>Animator-Parameter Hash: Horizontal (float).</summary>
        private static readonly int s_HorizontalHash = Animator.StringToHash("Horizontal");

        /// <summary>Animator-Parameter Hash: Vertical (float).</summary>
        private static readonly int s_VerticalHash = Animator.StringToHash("Vertical");

        /// <summary>Animator-Parameter Hash: IsGrounded (bool).</summary>
        private static readonly int s_IsGroundedHash = Animator.StringToHash("IsGrounded");

        /// <summary>Animator-Parameter Hash: IsWalking (bool).</summary>
        private static readonly int s_IsWalkingHash = Animator.StringToHash("IsWalking");

        /// <summary>Animator-Parameter Hash: IsAttacking (bool).</summary>
        private static readonly int s_IsAttackingHash = Animator.StringToHash("IsAttacking");

        /// <summary>Animator-Parameter Hash: IsCrouching (bool).</summary>
        private static readonly int s_IsCrouchingHash = Animator.StringToHash("IsCrouching");

        /// <summary>Animator-Parameter Hash: IsReloading (bool).</summary>
        private static readonly int s_IsReloadingHash = Animator.StringToHash("IsReloading");

        /// <summary>Animator-Parameter Hash: IsAltAttacking (bool).</summary>
        private static readonly int s_IsAltAttackingHash = Animator.StringToHash("IsAltAttacking");

        /// <summary>Animator-Parameter Hash: IsSwapping (bool).</summary>
        private static readonly int s_IsSwappingHash = Animator.StringToHash("IsSwapping");

        /// <summary>Animator-Parameter Hash: CurrentWeapon (int).</summary>
        private static readonly int s_CurrentWeaponHash = Animator.StringToHash("CurrentWeapon");

        /// <summary>Animator-Parameter Hash: Ammo (int).</summary>
        private static readonly int s_AmmoHash = Animator.StringToHash("Ammo");

        /// <summary>Torso-Layer Index im Animator Controller (fuer Attack Re-Trigger).</summary>
        private const int TORSO_LAYER_INDEX = 0;

        /// <summary>
        /// Interpolationsgeschwindigkeit fuer Clients.
        /// </summary>
        private const float k_InterpolationSpeed = 15f;

        /// <summary>
        /// Server: Initialisiert den AI-Character an einem Spawn-Point.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            // Hitboxen aufbauen sobald Visual geladen ist (Server braucht Hitboxes fuer Bone-Tracking)
            SubscribeToVisualInstantiated();

            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.Log("[AI·Spawn] Warte auf ServerPlayerSpawnPoints (Map noch nicht geladen).");
                StartCoroutine(WaitForMapAndPosition());
                return;
            }

            AssignSpawnPosition();
        }

        /// <summary>
        /// Remote-Client: Startet Interpolation und Hitbox-Aufbau nach Visual-Load.
        /// AI-Bots haben keinen Owner-Client, daher gibt es kein OnOwnerSpawn.
        /// </summary>
        protected override void OnRemoteSpawn()
        {
            base.OnRemoteSpawn();

            // Hitboxen aufbauen sobald Visual geladen ist (Client braucht Hitboxes fuer Trefferkennung)
            SubscribeToVisualInstantiated();

            Debug.Log($"[AI·Spawn] Remote-Client: Bot {NetworkObjectId} interpoliert.");
        }

        /// <summary>
        /// Abonniert das OnVisualInstantiated-Event des SkinHandlers.
        /// </summary>
        private void SubscribeToVisualInstantiated()
        {
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated += OnVisualInstantiated;
            }
        }

        /// <summary>
        /// Callback nach Visual-Instanziierung: Baut Hitboxen und Collider auf den Skeleton-Bones auf.
        /// Collider wird auf Server UND Client identisch erstellt (ClientColliderSystem).
        /// </summary>
        private void OnVisualInstantiated(GameObject visualInstance)
        {
            if (m_HitboxSystem != null)
            {
                m_HitboxSystem.BuildHitboxes(visualInstance.transform);
                Debug.Log("[AI·Visual] Hitboxen aufgebaut.");
            }

            // Physik-Collider auf Server UND Client identisch aufbauen (SoF2 AABB)
            if (m_ColliderSystem != null)
            {
                Transform highestPoint = FindDeepChild(visualInstance.transform, "*head_t_0");
                Transform cranium = FindDeepChild(visualInstance.transform, "cranium");
                Transform rightHandBolt = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
                Transform leftHandBolt = FindDeepChild(visualInstance.transform, "lhand_tag_bone");
                Transform rightFoot = FindDeepChild(visualInstance.transform, "rtarsal");
                Transform leftFoot = FindDeepChild(visualInstance.transform, "ltarsal");
                Transform pelvis = FindDeepChild(visualInstance.transform, "pelvis");

                m_ColliderSystem.CalculateAutoCapsuleSize(
                    highestPoint != null ? highestPoint : cranium,
                    pelvis, leftHandBolt, rightHandBolt, leftFoot, rightFoot);
                Debug.Log("[AI·Visual] Collider aufgebaut.");
            }

            // Server: Collider-Referenz an ServerAICharacter uebergeben fuer Physik-Simulation
            if (IsServer && m_ServerAICharacter != null && m_ColliderSystem != null)
            {
                m_ServerAICharacter.SetColliderSystem(m_ColliderSystem);
            }

            // Server: Sensoren am Visual-Root initialisieren (fuer GOAP-Sensor-Input)
            if (IsServer && m_ServerAICharacter != null)
            {
                m_ServerAICharacter.InitializeSensors(visualInstance.transform);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Client: Sensor-Debug-Visualisierung (Raycasts + LineRenderer, rein visuell)
            if (!IsServer)
            {
                SensorArrayGenerator sensorArray = GetComponent<SensorArrayGenerator>();
                if (sensorArray != null)
                {
                    // Sensoren am Character-Root (gleiche Logik wie Server: AIBotController.InitializeSensors)
                    sensorArray.ConfigureFOV(120f, 40f);
                    sensorArray.Generate(transform, 1.83f);
                    m_ClientDebugSensorArray = sensorArray;
                    Debug.Log("[AI·Visual] Client: Debug-Sensoren initialisiert (H=1.83m).");
                }
            }
#endif

            // Animator finden und konfigurieren (Server + Client)
            m_Animator = visualInstance.GetComponentInChildren<Animator>();
            if (m_Animator != null)
            {
                m_Animator.applyRootMotion = false;

                if (IsServer)
                {
                    m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                Debug.Log($"[AI·Visual] Animator gefunden: '{m_Animator.gameObject.name}' | RootMotion=off | Server={IsServer}");
            }

            // Footstep-Handler auf Animator-GO initialisieren (Server + Client = alle hoeren Schritte)
            if (m_Animator != null)
            {
                m_FootstepHandler = m_Animator.gameObject.GetComponent<ClientFootstepHandler>();
                if (m_FootstepHandler == null)
                {
                    m_FootstepHandler = m_Animator.gameObject.AddComponent<ClientFootstepHandler>();
                }
                m_FootstepHandler.Initialize(transform);
            }

            // Bone-Referenzen fuer LateUpdate-Korrekturen speichern
            m_VisualInstance = visualInstance.transform;
            m_SkeletonRoot = FindDeepChild(visualInstance.transform, "skeleton_root");
            m_ModelRoot = FindDeepChild(visualInstance.transform, "model_root");
            m_PelvisTarget = FindDeepChild(visualInstance.transform, "pelvis");
            m_LowerLumbar = FindDeepChild(visualInstance.transform, "lower_lumbar");
            m_UpperLumbar = FindDeepChild(visualInstance.transform, "upper_lumbar");
            m_LeftFoot = FindDeepChild(visualInstance.transform, "ltarsal");
            m_RightFoot = FindDeepChild(visualInstance.transform, "rtarsal");
            m_HasBindPoseReference = false;

            // SmoothedLegsForward initialisieren auf aktuelle Blickrichtung
            Vector3 initialForward = transform.forward;
            initialForward.y = 0f;
            if (initialForward.sqrMagnitude > 0.001f)
            {
                m_SmoothedLegsForward = initialForward.normalized;
            }
        }

        /// <summary>
        /// Sucht rekursiv ein Kind-Transform per Name.
        /// </summary>
        private static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Raeumt Event-Abos auf beim Despawn.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }

            if (m_HitboxSystem != null)
            {
                m_HitboxSystem.ClearHitboxes();
            }
        }

        /// <summary>
        /// Wartet bis die Map geladen ist und ServerPlayerSpawnPoints verfuegbar sind.
        /// </summary>
        private IEnumerator WaitForMapAndPosition()
        {
            yield return new WaitUntil(() => ServerPlayerSpawnPoints.Instance != null);
            AssignSpawnPosition();
        }

        /// <summary>
        /// Weist dem AI-Character einen Spawn-Point zu.
        /// </summary>
        private void AssignSpawnPosition()
        {
            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            m_ServerAICharacter.SetReady();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AI·Spawn] Server: Bot gespawnt bei {position}");
#endif
        }

        /// <summary>
        /// Server-seitiger Respawn auf den naechsten Spawn-Point.
        /// </summary>
        public void RespawnAtNextSpawnPoint()
        {
            if (!IsServer)
            {
                return;
            }

            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.LogWarning("[AI·Spawn] Keine SpawnPoints vorhanden!");
                return;
            }

            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            // Bot wiederbeleben falls tot
            if (m_CharacterState != null && !m_CharacterState.IsAlive)
            {
                m_CharacterState.SetHealth(100);
                m_CharacterState.SetIsAlive(true);
            }

            // Waffen/Ammo zuruecksetzen und Gametype-Startwaffen neu zuweisen
            ResetWeaponsForRound();

            m_ServerAICharacter.SetReady();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AI·Spawn] Server: Bot respawned bei {position}");
#endif
        }

        /// <summary>
        /// Server: Setzt Waffen und Ammo auf Gametype-Startwerte zurueck.
        /// Wird bei jedem Runden-Respawn aufgerufen.
        /// </summary>
        private void ResetWeaponsForRound()
        {
            if (m_CharacterState == null)
            {
                return;
            }

            m_CharacterState.ClearWeaponsAndAmmo();

            GametypeManagement.GametypeManager gametypeManager = ServiceLocator.Get<GametypeManagement.GametypeManager>();
            GametypeManagement.GametypeTeam team = (GametypeManagement.GametypeTeam)m_CharacterState.TeamId;
            string[] weapons = gametypeManager?.GetStartWeapons(team);

            if (weapons != null)
            {
                foreach (string weapon in weapons)
                {
                    (int clip, int reserve, int altClip, int altReserve)? ammoOverride = gametypeManager.GetStartAmmo(weapon);
                    if (ammoOverride.HasValue)
                    {
                        m_CharacterState.PreloadWeaponAmmo(weapon, ammoOverride.Value.clip, ammoOverride.Value.reserve, ammoOverride.Value.altClip, ammoOverride.Value.altReserve);
                    }

                    m_CharacterState.AddWeapon(weapon);
                }
            }
            else
            {
                m_CharacterState.AddWeapon("knife");
            }

            m_CharacterState.SetCurrentWeaponName("knife");
        }

        /// <summary>
        /// LateUpdate: Bone-Position-Korrekturen nach Animator-Evaluation.
        /// Analog zu ClientPlayerCharacter.LateUpdate() — entfernt XZ-Drift
        /// und korrigiert Y-Offset (Fuss-Alignment, Crouch-Hoehe).
        /// </summary>
        private void LateUpdate()
        {
            ResetRootBonePositions();
            AdjustVisualYOffset();
            UpdatePelvisRotation();
            UpdateLumbarRotation();
        }

        /// <summary>
        /// Setzt die lokale XZ-Position der Root-Bones und des Pelvis auf Null.
        /// SoF2-Animationen enthalten Positions-Keyframes auf dem pelvis-Bone
        /// die das Mesh vom Physik-Collider wegbewegen. Da Bewegung komplett
        /// ueber PlayerPhysicsSimulation laeuft (applyRootMotion = false),
        /// muss die XZ-Position nach Animator-Evaluation korrigiert werden.
        /// Y bleibt erhalten (Crouch-Hoehe etc.).
        /// </summary>
        private void ResetRootBonePositions()
        {
            if (m_SkeletonRoot != null)
            {
                Vector3 skPos = m_SkeletonRoot.localPosition;
                m_SkeletonRoot.localPosition = new Vector3(0f, skPos.y, 0f);
            }

            if (m_ModelRoot != null)
            {
                Vector3 mrPos = m_ModelRoot.localPosition;
                m_ModelRoot.localPosition = new Vector3(0f, mrPos.y, 0f);
            }

            if (m_PelvisTarget == null)
            {
                return;
            }

            if (!m_HasBindPoseReference)
            {
                m_PelvisBindLocalPos = m_PelvisTarget.localPosition;
                m_HasBindPoseReference = true;
                return;
            }

            Vector3 pelPos = m_PelvisTarget.localPosition;
            m_PelvisTarget.localPosition = new Vector3(
                m_PelvisBindLocalPos.x,
                pelPos.y,
                m_PelvisBindLocalPos.z
            );
        }

        /// <summary>
        /// Korrigiert die Y-Position des Visual-Prefabs sodass die Fuesse am Boden (Y=0) stehen.
        /// Berechnet den niedrigsten Fuss-Bone und verschiebt das Visual entsprechend.
        /// Funktioniert dynamisch fuer Standing und Crouching.
        /// </summary>
        private void AdjustVisualYOffset()
        {
            if (m_VisualInstance == null || (m_LeftFoot == null && m_RightFoot == null))
            {
                return;
            }

            float lowestFootY = float.MaxValue;
            if (m_LeftFoot != null)
            {
                float y = transform.InverseTransformPoint(m_LeftFoot.position).y;
                lowestFootY = Mathf.Min(lowestFootY, y);
            }
            if (m_RightFoot != null)
            {
                float y = transform.InverseTransformPoint(m_RightFoot.position).y;
                lowestFootY = Mathf.Min(lowestFootY, y);
            }

            Vector3 visPos = m_VisualInstance.localPosition;
            m_VisualInstance.localPosition = new Vector3(visPos.x, visPos.y - lowestFootY, visPos.z);
        }

        /// <summary>
        /// Berechnet den SoF2 PM_SetMovementDir Index (0-7) aus dem Move-Input.
        /// Identisch zur Spieler-Logik in ClientPlayerCharacter.
        /// </summary>
        private static int ComputeMovementDir(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f)
            {
                return 0;
            }

            float forwardmove = move.y;
            float rightmove = move.x;

            if (rightmove == 0 && forwardmove > 0) return 0;
            if (rightmove < 0 && forwardmove > 0) return 1;
            if (rightmove < 0 && forwardmove == 0) return 2;
            if (rightmove < 0 && forwardmove < 0) return 3;
            if (rightmove == 0 && forwardmove < 0) return 4;
            if (rightmove > 0 && forwardmove < 0) return 5;
            if (rightmove > 0 && forwardmove == 0) return 6;
            if (rightmove > 0 && forwardmove > 0) return 7;

            return 0;
        }

        /// <summary>
        /// Pelvis/Legs Rotation analog zu ClientPlayerCharacter.UpdatePelvisRotation().
        /// Verwendet smoothedLegsForward (Slerp), legsYawOffset beim Movement,
        /// idleYawByDir im Idle, und Torso-Follow wenn der Bot stillsteht.
        /// Anstelle der Kamera-Transforms nutzt der Bot transform.forward als Blickrichtung.
        /// </summary>
        private void UpdatePelvisRotation()
        {
            if (m_PelvisTarget == null)
            {
                return;
            }

            // Bot-Forward/Right auf XZ-Ebene (anstelle von Kamera-Yaw-Target)
            Vector3 fwd = transform.forward;
            Vector3 rgt = transform.right;
            fwd.y = 0f;
            rgt.y = 0f;
            fwd.Normalize();
            rgt.Normalize();

            NetworkAnimationState animState = m_AnimationState.Value;
            Vector2 moveInput = new(animState.MoveInputX, animState.MoveInputY);
            bool hasInput = moveInput.sqrMagnitude > 0.0001f;

            if (hasInput)
            {
                // Legs-Richtung aus Input berechnen (SoF2-Stil: bei Rueckwaertsbewegung spiegeln)
                float forwardComp = Mathf.Abs(moveInput.y);
                float effectiveX = (moveInput.y < 0f) ? -moveInput.x : moveInput.x;
                Vector3 inputDir = fwd * forwardComp + rgt * effectiveX;

                if (inputDir.sqrMagnitude > 0.0001f)
                {
                    Vector3 desiredFlat = inputDir;
                    desiredFlat.y = 0f;
                    float t = Mathf.Clamp01(k_BaseLegsRotationSmooth * Time.deltaTime);
                    m_SmoothedLegsForward = Vector3.Slerp(m_SmoothedLegsForward, desiredFlat.normalized, t);
                }

                // Direction-Index aus Raw-Input (SoF2 PM_SetMovementDir)
                m_LastMoveDirIndex = ComputeMovementDir(moveInput);
            }
            else
            {
                // Idle: Legs drehen sich langsam Richtung Bot-Forward (Torso-Follow)
                float yawDelta = Vector3.SignedAngle(m_SmoothedLegsForward, fwd, Vector3.up);
                float torsoDrivenT = Mathf.Clamp01(k_TorsoFollowYawInfluence * Time.deltaTime * (Mathf.Abs(yawDelta) / 90f));
                if (torsoDrivenT > 0f)
                {
                    m_SmoothedLegsForward = Vector3.Slerp(m_SmoothedLegsForward, fwd, torsoDrivenT);
                }
            }

            // legsLook aus gesmoothtem Forward
            Quaternion legsLook = Quaternion.LookRotation(m_SmoothedLegsForward, Vector3.up);

            if (!hasInput)
            {
                // Idle: idleYawByDir Offset anwenden
                int idleDir = Mathf.Clamp(m_LastMoveDirIndex, 0, 7);
                Quaternion idleOffset = Quaternion.Euler(0f, s_IdleYawByDir[idleDir], 0f);
                m_PelvisTarget.rotation = legsLook * idleOffset;
            }
            else
            {
                // Movement: legsYawOffset anwenden (Skeleton-Korrektur)
                Quaternion moveOffset = Quaternion.Euler(0f, k_LegsYawOffsetDegrees, 0f);
                m_PelvisTarget.rotation = legsLook * moveOffset;
            }
        }

        /// <summary>
        /// Lumbar-Rotation analog zu ClientPlayerCharacter.UpdateLumbarRotation().
        /// LowerLumbar und UpperLumbar schauen zum LookAtPoint (aus Yaw + Pitch),
        /// mit Lean (Roll/Pitch bei Bewegung), Strafe-Yaw-Twist, Lumbar-Yaw/Pitch-Offsets,
        /// und Movement-Idle-Offset fuer den oberen Torso.
        /// Anstelle der Kamera-Transforms nutzt der Bot PitchAngle aus NetworkAnimationState.
        /// </summary>
        private void UpdateLumbarRotation()
        {
            if (m_LowerLumbar == null && m_UpperLumbar == null)
            {
                return;
            }

            NetworkAnimationState animState = m_AnimationState.Value;
            Vector2 moveInput = new(animState.MoveInputX, animState.MoveInputY);

            // LookAt-Punkt berechnen: Bot-Position + Blickrichtung (Yaw + Pitch) * 100m
            Quaternion aimRotation = Quaternion.Euler(animState.PitchAngle, transform.eulerAngles.y, 0f);
            Vector3 aimDirection = aimRotation * Vector3.forward;
            Vector3 eyePos = transform.position + new Vector3(0f, 1.83f, 0f);
            Vector3 lookAtPoint = eyePos + aimDirection * 100f;

            bool hasInput = moveInput.sqrMagnitude > 0.0001f;

            // Lean-Winkel berechnen (Roll bei Seitwärtsbewegung, Pitch bei Vorwärts/Rückwärts)
            Vector2 targetLeanAngles = Vector2.zero;
            if (hasInput)
            {
                targetLeanAngles.x = -moveInput.x * k_RollLeanDegrees;
                targetLeanAngles.y = moveInput.y * k_PitchLeanDegrees;
            }

            float leanT = Mathf.Clamp01(k_LeanSmooth * Time.deltaTime);
            m_CurrentLeanAngles = Vector2.Lerp(m_CurrentLeanAngles, targetLeanAngles, leanT);

            // Lumbar Yaw Offsets smoothen
            float yawSmoothT = Mathf.Clamp01(k_LumbarYawSmooth * Time.deltaTime);
            m_CurrentUpperLumbarYaw = Mathf.Lerp(m_CurrentUpperLumbarYaw, k_UpperLumbarYawOffset, yawSmoothT);
            m_CurrentLowerLumbarYaw = Mathf.Lerp(m_CurrentLowerLumbarYaw, k_LowerLumbarYawOffset, yawSmoothT);

            // Lumbar Pitch Offsets smoothen
            float pitchSmoothT = Mathf.Clamp01(k_LumbarPitchSmooth * Time.deltaTime);
            m_CurrentUpperLumbarPitch = Mathf.Lerp(m_CurrentUpperLumbarPitch, k_UpperLumbarPitchOffset, pitchSmoothT);
            m_CurrentLowerLumbarPitch = Mathf.Lerp(m_CurrentLowerLumbarPitch, k_LowerLumbarPitchOffset, pitchSmoothT);

            // Movement-based Idle Offset berechnen (nur im Idle)
            float targetMovementOffset = 0f;
            if (!hasInput && m_LastMoveDirIndex >= 0 && m_LastMoveDirIndex < s_MovementOffsets.Length)
            {
                targetMovementOffset = s_MovementOffsets[m_LastMoveDirIndex];
            }

            float movementSmoothT = Mathf.Clamp01(k_MovementOffsetSmooth * Time.deltaTime);
            m_CurrentMovementIdleOffset = Mathf.Lerp(m_CurrentMovementIdleOffset, targetMovementOffset, movementSmoothT);

            // Lower Lumbar rotieren
            if (m_LowerLumbar != null)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - m_LowerLumbar.position, transform.up);
                float strafeYaw = moveInput.x * k_StrafeYawDegrees;
                Quaternion leanRotation = Quaternion.Euler(
                    m_CurrentLeanAngles.y + m_CurrentLowerLumbarPitch,
                    strafeYaw + m_CurrentLowerLumbarYaw,
                    m_CurrentLeanAngles.x
                );
                m_LowerLumbar.rotation = lookRotation * leanRotation * s_SkeletonOffset;
            }

            // Upper Lumbar rotieren (reduzierter Strafe-Yaw, reduzierter Lean)
            if (m_UpperLumbar != null)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - m_UpperLumbar.position, transform.up);
                float strafeYawUpper = moveInput.x * (k_StrafeYawDegrees * 0.6f);
                float totalYawOffset = strafeYawUpper + m_CurrentUpperLumbarYaw + m_CurrentMovementIdleOffset;
                Quaternion leanRotation = Quaternion.Euler(
                    m_CurrentLeanAngles.y * 0.7f + m_CurrentUpperLumbarPitch,
                    totalYawOffset,
                    m_CurrentLeanAngles.x * 0.7f
                );
                m_UpperLumbar.rotation = lookRotation * leanRotation * s_SkeletonOffset;
            }
        }

        private void Update()
        {
            // Footstep-Handler mit Animations-Daten fuettern (Server + Client)
            if (m_FootstepHandler != null)
            {
                NetworkAnimationState animState = m_AnimationState.Value;
                m_FootstepHandler.IsGrounded = animState.IsGrounded;
                m_FootstepHandler.IsWalking = animState.IsWalking;
                m_FootstepHandler.HorizontalSpeed = animState.Speed;
            }

            if (IsServer)
            {
                // Server: Position direkt aus AI-Logik synchronisieren
                m_ServerPosition.Value = transform.position;
                m_ServerRotation.Value = transform.rotation;

                // Server: Animation wird in WriteAnimationState() direkt angewendet
                return;
            }

            // Client: Interpolation zur Server-Position
            transform.position = Vector3.Lerp(transform.position, m_ServerPosition.Value, Time.deltaTime * k_InterpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, m_ServerRotation.Value, Time.deltaTime * k_InterpolationSpeed);

            // Client: Animation aus NetworkVariable anwenden
            ApplyAnimationToAnimator(m_AnimationState.Value);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Client: Debug-Sensoren ticken (Raycasts + LineRenderer aktualisieren)
            if (m_ClientDebugSensorArray != null)
            {
                m_ClientDebugSensorArray.TickAll();
            }

            // Client: Debug-Pfad-Visualisierung rendern (vom Server per ClientRpc empfangen)
            UpdateClientDebugPath();
#endif
        }

        /// <summary>
        /// Server: Schreibt den Animations-State in die NetworkVariable und wendet ihn lokal an.
        /// Wird von ServerAICharacter nach jedem Physik-Tick aufgerufen.
        /// </summary>
        public void WriteAnimationState(NetworkAnimationState state)
        {
            m_AnimationState.Value = state;
            ApplyAnimationToAnimator(state);
        }

        /// <summary>
        /// Wendet die Animations-Parameter auf den lokalen Animator an.
        /// Identisch zum Spieler-Character (dieselben Parameter-Namen und Logik).
        /// </summary>
        private void ApplyAnimationToAnimator(NetworkAnimationState state)
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(s_IsMovingHash, state.IsMoving);
            m_Animator.SetFloat(s_SpeedHash, state.Speed);
            m_Animator.SetFloat(s_HorizontalHash, state.Horizontal);
            m_Animator.SetFloat(s_VerticalHash, state.Vertical);
            m_Animator.SetBool(s_IsGroundedHash, state.IsGrounded);
            m_Animator.SetBool(s_IsWalkingHash, state.IsWalking);
            m_Animator.SetBool(s_IsAttackingHash, state.IsAttacking);
            m_Animator.SetBool(s_IsCrouchingHash, state.IsCrouching);
            m_Animator.SetBool(s_IsReloadingHash, state.IsReloading);
            m_Animator.SetBool(s_IsAltAttackingHash, state.IsAltAttacking);
            m_Animator.SetBool(s_IsSwappingHash, state.IsSwapping);
            m_Animator.SetInteger(s_CurrentWeaponHash, state.CurrentWeapon);
            m_Animator.SetInteger(s_AmmoHash, state.Ammo);

            // Attack-Animation Re-Trigger: bei neuer AttackSequence die Attack-Animation
            // auf dem Torso-Layer von Frame 0 neu starten (loop=false Animationen).
            if (state.IsAttacking && state.AttackSequence != m_LastAttackSequence)
            {
                AnimatorStateInfo torsoState = m_Animator.GetCurrentAnimatorStateInfo(TORSO_LAYER_INDEX);
                m_Animator.Play(torsoState.fullPathHash, TORSO_LAYER_INDEX, 0f);
            }

            m_LastAttackSequence = state.AttackSequence;
        }

        /// <summary>
        /// Setzt Name, Skin, Team und Waffen auf dem NetworkedCharacterState.
        /// Wird vom AIBotSpawner nach dem Spawn aufgerufen.
        /// Gametype-spezifische Waffen und Ammo-Overrides werden automatisch angewendet.
        /// </summary>
        /// <param name="botName">Name des Bots.</param>
        /// <param name="skinName">Skin-Name (Addressable-Key).</param>
        /// <param name="teamId">Team-ID (0 oder 1).</param>
        public void InitializeBot(string botName, string skinName, uint teamId)
        {
            if (!IsServer)
            {
                return;
            }

            m_CharacterState.SetCharacterName(botName);
            m_CharacterState.SetCurrentSkinName(skinName);
            m_CharacterState.SetTeam(teamId);

            // Gametype-spezifische Waffen zuweisen
            GametypeManagement.GametypeManager gametypeManager = ServiceLocator.Get<GametypeManagement.GametypeManager>();
            GametypeManagement.GametypeTeam team = (GametypeManagement.GametypeTeam)teamId;
            string[] weapons = gametypeManager?.GetStartWeapons(team);

            if (weapons != null)
            {
                foreach (string weapon in weapons)
                {
                    // Ammo-Override VOR AddWeapon im Cache hinterlegen
                    (int clip, int reserve, int altClip, int altReserve)? ammoOverride = gametypeManager.GetStartAmmo(weapon);
                    if (ammoOverride.HasValue)
                    {
                        m_CharacterState.PreloadWeaponAmmo(weapon, ammoOverride.Value.clip, ammoOverride.Value.reserve, ammoOverride.Value.altClip, ammoOverride.Value.altReserve);
                    }

                    m_CharacterState.AddWeapon(weapon);
                }
            }
            else
            {
                m_CharacterState.AddWeapon("knife");
            }

            m_CharacterState.SetCurrentWeaponName("knife");

            Debug.Log($"[AI·Init] Bot: Name='{botName}' | Skin='{skinName}' | Team={teamId} | Weapons={weapons?.Length ?? 1}");
        }

        // ──────────────────────────────────────────────────────────
        //  Sound System: Waffen-Sounds (Server → Alle Clients via RPC)
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Server: Loest den Waffenfeuer-Sound fuer alle Clients aus.
        /// Wird von ServerAICharacter.ProcessAIAttack() aufgerufen.
        /// </summary>
        /// <param name="weaponName">Name der aktuellen Waffe (z.B. "knife").</param>
        public void PlayAttackSound(string weaponName)
        {
            if (!IsServer)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(weaponName);
            if (weapon == null)
            {
                return;
            }

            string fireSoundPath = ResolveWeaponSoundPath(weapon, "fire");
            if (!string.IsNullOrEmpty(fireSoundPath))
            {
                WeaponFireSoundClientRpc(fireSoundPath);
            }
        }

        /// <summary>
        /// Server → Alle Clients: Spielt den Waffenfeuer-Sound am Bot ab.
        /// Identisch zum Spieler-System (MuzzleEffectsClientRpc), aber vereinfacht
        /// da Bots vorerst kein Muzzle-Flash/Shell-Casing benoetigen.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void WeaponFireSoundClientRpc(string fireSoundPath)
        {
            if (string.IsNullOrEmpty(fireSoundPath))
            {
                return;
            }

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                return;
            }

            AudioClip fireClip = soundManager.GetClip(fireSoundPath);
            if (fireClip == null)
            {
                return;
            }

            GameObject soundObj = new("BotWeaponFireFX");
            soundObj.transform.position = transform.position;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = fireClip;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 50f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Destroy(soundObj, fireClip.length + 0.1f);
        }

        /// <summary>
        /// Loest einen Sound-Pfad aus der WeaponDefinition.Sounds-Map auf.
        /// Identisch zur Spieler-Logik (Fallback fire→swing, altFire→toss).
        /// </summary>
        private static string ResolveWeaponSoundPath(WeaponDefinition weapon, string soundKey)
        {
            if (weapon?.Sounds == null)
            {
                return "";
            }

            if (weapon.Sounds.ContainsKey(soundKey))
            {
                return ExtractSoundValue(weapon.Sounds[soundKey]);
            }

            string fallbackKey = soundKey switch
            {
                "fire" => "swing",
                "altFire" => "toss",
                _ => null
            };

            if (fallbackKey != null && weapon.Sounds.ContainsKey(fallbackKey))
            {
                return ExtractSoundValue(weapon.Sounds[fallbackKey]);
            }

            return "";
        }

        /// <summary>
        /// Extrahiert einen Sound-Pfad aus einem object-Wert (String oder JArray mit zufaelliger Auswahl).
        /// </summary>
        private static string ExtractSoundValue(object value)
        {
            if (value is string str)
            {
                return str;
            }

            if (value is JArray arr && arr.Count > 0)
            {
                int index = Random.Range(0, arr.Count);
                return arr[index].ToString();
            }

            return "";
        }

        // ===== Debug Pfad-Visualisierung (Server → Client Sync) =====

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Server: Synchronisiert die aktuelle Pfad-Visualisierung an alle Clients.
        /// Wird von AIBotController.DrawDebugPath() aufgerufen.
        /// </summary>
        public void SyncDebugPath(Vector3[] pathCorners, Vector3 steerStart, Vector3 steerEnd, Vector3 targetPos, bool hasTarget)
        {
            if (!IsServer)
            {
                return;
            }

            SyncDebugPathClientRpc(pathCorners, steerStart, steerEnd, targetPos, hasTarget);
        }

        /// <summary>
        /// Server → Alle Clients: Uebertraegt Pfadpunkte fuer Debug-Visualisierung.
        /// </summary>
        [Rpc(SendTo.NotServer)]
        private void SyncDebugPathClientRpc(Vector3[] pathCorners, Vector3 steerStart, Vector3 steerEnd, Vector3 targetPos, bool hasTarget)
        {
            m_ClientPathCorners = pathCorners ?? System.Array.Empty<Vector3>();
            m_ClientSteerStart = steerStart;
            m_ClientSteerEnd = steerEnd;
            m_ClientTargetPos = targetPos;
            m_ClientHasTarget = hasTarget;
        }

        /// <summary>
        /// Client: Erstellt einen LineRenderer fuer Debug-Pfad-Visualisierung.
        /// </summary>
        private LineRenderer CreateClientPathLineRenderer(string objectName, Color color, float width)
        {
            if (s_CachedClientLineShader == null)
            {
                s_CachedClientLineShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (s_CachedClientLineShader == null)
                {
                    s_CachedClientLineShader = Shader.Find("Sprites/Default");
                }

                if (s_CachedClientLineShader == null)
                {
                    s_CachedClientLineShader = Shader.Find("Unlit/Color");
                }
            }

            if (s_CachedClientLineShader == null)
            {
                return null;
            }

            GameObject go = new(objectName);
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.numCapVertices = 2;

            Material mat = new(s_CachedClientLineShader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.renderQueue = 5000;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = 100;

            return lr;
        }

        /// <summary>
        /// Client: Rendert die vom Server empfangenen Pfaddaten per LineRenderer.
        /// Wird in Update() auf dem Client aufgerufen.
        /// </summary>
        private void UpdateClientDebugPath()
        {
            float offsetY = 0.2f;

            if (m_ClientPathLine == null)
            {
                m_ClientPathLine = CreateClientPathLineRenderer("AI_ClientPathLine", Color.cyan, 0.08f);
            }

            if (m_ClientSteerLine == null)
            {
                m_ClientSteerLine = CreateClientPathLineRenderer("AI_ClientSteerLine", Color.yellow, 0.12f);
            }

            if (m_ClientTargetLine == null)
            {
                m_ClientTargetLine = CreateClientPathLineRenderer("AI_ClientTargetLine", Color.red, 0.06f);
            }

            if (m_ClientPathLine == null || m_ClientSteerLine == null || m_ClientTargetLine == null)
            {
                return;
            }

            // Pfad-Linie (Cyan)
            if (m_ClientPathCorners.Length > 1)
            {
                m_ClientPathLine.positionCount = m_ClientPathCorners.Length;
                for (int i = 0; i < m_ClientPathCorners.Length; i++)
                {
                    m_ClientPathLine.SetPosition(i, m_ClientPathCorners[i] + Vector3.up * offsetY);
                }
            }
            else
            {
                m_ClientPathLine.positionCount = 0;
            }

            // Steuer-Linie (Gelb): Bot → Wegpunkt
            if (m_ClientSteerEnd != Vector3.zero)
            {
                m_ClientSteerLine.positionCount = 2;
                m_ClientSteerLine.SetPosition(0, transform.position + Vector3.up * offsetY);
                m_ClientSteerLine.SetPosition(1, m_ClientSteerEnd + Vector3.up * offsetY);
            }
            else
            {
                m_ClientSteerLine.positionCount = 0;
            }

            // Ziel-Linie (Rot): Vertikale Markierung
            if (m_ClientHasTarget)
            {
                m_ClientTargetLine.positionCount = 2;
                m_ClientTargetLine.SetPosition(0, m_ClientTargetPos + Vector3.up * offsetY);
                m_ClientTargetLine.SetPosition(1, m_ClientTargetPos + Vector3.up * 3f);
            }
            else
            {
                m_ClientTargetLine.positionCount = 0;
            }
        }
#endif
    }
}
