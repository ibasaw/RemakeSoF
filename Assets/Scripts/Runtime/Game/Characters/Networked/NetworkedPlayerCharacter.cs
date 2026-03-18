using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using Tolik.RemakeSoF.Runtime.Game.Effects;
using Tolik.RemakeSoF.Runtime.Game.Projectiles;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Server Authority + Client-Side Prediction nach SoF2-Vorbild.
    /// Owner: Baut PlayerCommand aus Input, führt lokale Prediction aus, sendet Command an Server.
    /// Server: Führt identische SoF2-Physik-Simulation aus, sendet Acknowledgement zurück.
    /// Remote: Interpoliert von Server-Position.
    /// Treibt Animator-Parameter auf allen Clients via NetworkVariable.
    /// </summary>
    public class NetworkedPlayerCharacter : NetworkedCharacter, ICharacter
    {
        // ===== Server-Side Processing =====

        /// <summary>
        /// ServerPlayerCharacter-Referenz für server-seitige Physik-Verarbeitung.
        /// Existiert auf dem gleichen Prefab.
        /// </summary>
        [SerializeField]
        private ServerPlayerCharacter m_ServerPlayerCharacter;

        /// <summary>
        /// Event: Server-Acknowledgement empfangen.
        /// ClientPlayerCharacter abonniert dies für Reconciliation.
        /// </summary>
        public event Action<ServerMovementAck> OnMovementAcknowledged;

        // ===== Animation Sync =====

        /// <summary>
        /// SkinHandler-Referenz für OnVisualInstantiated-Event.
        /// Wird benötigt um nach Visual-Instanziierung den Animator zu finden.
        /// </summary>
        [SerializeField]
        private ClientCharacterSkinHandler m_SkinHandler;

        /// <summary>
        /// Animator-Parameter synchronisiert vom Owner an alle Clients.
        /// Owner schreibt direkt (kein RPC nötig), Remotes lesen und treiben ihren Animator.
        /// </summary>
        private NetworkVariable<NetworkAnimationState> m_AnimationState = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        /// <summary>
        /// Referenz auf den Animator des instanziierten Skin-Visuals.
        /// Wird zur Laufzeit nach Visual-Instanziierung gesetzt.
        /// </summary>
        private Animator m_Animator;

        // Gecachte Hash-IDs für Animator-Parameter (Performance: kein String-Lookup pro Frame).
        private static readonly int s_IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int s_SpeedHash = Animator.StringToHash("Speed");
        private static readonly int s_HorizontalHash = Animator.StringToHash("Horizontal");
        private static readonly int s_VerticalHash = Animator.StringToHash("Vertical");
        private static readonly int s_IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int s_IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int s_IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int s_IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int s_IsReloadingHash = Animator.StringToHash("IsReloading");
        private static readonly int s_IsAltAttackingHash = Animator.StringToHash("IsAltAttacking");
        private static readonly int s_IsSwappingHash = Animator.StringToHash("IsSwapping");
        private static readonly int s_JumpHash = Animator.StringToHash("Jump");
        private static readonly int s_CurrentWeaponHash = Animator.StringToHash("CurrentWeapon");
        private static readonly int s_AmmoHash = Animator.StringToHash("Ammo");

        // Swap-Animation State Hashes (Torso Layer)
        private static readonly int s_KnifeDropHash = Animator.StringToHash("TORSO_DROP_KNIFE");
        private static readonly int s_DropOneHandedHash = Animator.StringToHash("TORSO_DROP_ONEHANDED");
        private static readonly int s_DropTwoHandedHash = Animator.StringToHash("TORSO_DROP");
        private static readonly int s_KnifeReadyHash = Animator.StringToHash("TORSO_RAISE_KNIFE");
        private static readonly int s_ReadyOneHandedHash = Animator.StringToHash("TORSO_RAISE_ONEHANDED");
        private static readonly int s_ReadyTwoHandedHash = Animator.StringToHash("TORSO_RAISE");
        private static readonly int s_SwapSpeedHash = Animator.StringToHash("SwapSpeed");
        private const int TORSO_LAYER_INDEX = 0;

        /// <summary>
        /// Clip-Dauer aller Swap-Animationen in Sekunden (6 Frames bei 20fps FBX-Samplerate).
        /// </summary>
        private const float SWAP_CLIP_DURATION = 6f / 20f;

        /// <summary>
        /// Aktueller synchronisierter Animation-State (für Remote-Bone-Rotation).
        /// </summary>
        public NetworkAnimationState CurrentAnimationState => m_AnimationState.Value;

        /// <summary>
        /// Letzte bekannte AttackSequence fuer Re-Trigger-Erkennung.
        /// Wenn sich die Sequenznummer aendert, wird die Attack-Animation
        /// auf dem Torso-Layer von Frame 0 neu gestartet.
        /// </summary>
        private byte m_LastAttackSequence;

        // ===== Server-Side Attack Gating (SoF2: weaponTime in playerState_t) =====

        /// <summary>SoF2-Unit → Unity-Meter Konvertierungsfaktor (1 QU = 1 Inch = 0.0254m).</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>Physics Layer Name fuer Hitbox-Collider.</summary>
        private const string HITBOX_LAYER_NAME = "Hitbox";

        [Header("Debug Tracer")]
        /// <summary>Zeigt Debug-Tracer-Linien bei jedem Schuss (Game-View + Scene-View). Linie vom ejectBone zum HitPoint.</summary>
        [SerializeField]
        private bool m_ShowDebugTracers;

        /// <summary>Dauer der sichtbaren Tracer-Linie in Sekunden.</summary>
        private const float TRACER_DURATION = 2.0f;

        /// <summary>Laufende Projektil-ID fuer Visual-Cleanup (Server-only).</summary>
        private uint m_NextProjectileId = 1;

        /// <summary>
        /// NetworkedCharacterState-Referenz fuer Waffen-Lookup (CurrentWeaponName).
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        /// <summary>Verbleibende Attack-Frames auf dem Server (autoritativ, nicht manipulierbar).</summary>
        private int m_ServerAttackFramesRemaining;

        /// <summary>Frame-Akkumulator auf dem Server fuer frame-diskretes Timing.</summary>
        private float m_ServerAttackFrameAccumulator;

        /// <summary>Aktuelle Attack-Frame-Anzahl basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ServerAttackFrames = 6;

        /// <summary>Aktuelle Attack-FPS basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ServerAttackFps = 20;

        /// <summary>Akkumulierte Inaccuracy durch Dauerfeuer (steigt pro Schuss Richtung MaxInaccuracy, faellt bei Pause zurueck).</summary>
        private float m_ServerAccumulatedInaccuracy;

        /// <summary>Zeitpunkt des letzten Schusses fuer Inaccuracy-Decay (Server-seitig).</summary>
        private float m_ServerLastShotTime;

        /// <summary>Verbleibende Reload-Frames auf dem Server (autoritativ).</summary>
        private int m_ServerReloadFramesRemaining;

        /// <summary>Frame-Akkumulator auf dem Server fuer frame-diskretes Reload-Timing.</summary>
        private float m_ServerReloadFrameAccumulator;

        /// <summary>Aktuelle Reload-Frame-Anzahl basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ServerReloadFrames;

        /// <summary>Aktuelle Reload-FPS basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ServerReloadFps = 20;

        /// <summary>Ob die aktuelle Waffe Shell-by-Shell nachladet (M590, MM1).</summary>
        private bool m_ServerIsShellReload;

        /// <summary>Frame-Anzahl fuer ReloadStart-Animation (Shell-Reload).</summary>
        private int m_ServerReloadStartFrames;

        /// <summary>Frame-Anzahl fuer einzelne Shell-Lade-Animation (Shell-Reload).</summary>
        private int m_ServerReloadShellFrames;

        /// <summary>Frame-Anzahl fuer ReloadEnd-Animation (Shell-Reload).</summary>
        private int m_ServerReloadEndFrames;

        /// <summary>Aktuelle Phase beim Shell-Reload (Start, Shell, End).</summary>
        private ShellReloadPhase m_ServerShellReloadPhase;

        /// <summary>Verbleibende Shells die noch geladen werden muessen (Shell-Reload).</summary>
        private int m_ServerShellsRemaining;

        /// <summary>Verbleibende Frames in der aktuellen Shell-Reload-Phase.</summary>
        private int m_ServerShellPhaseFramesRemaining;

        // ===== Server-Side AltAttack Gating =====

        /// <summary>Verbleibende AltAttack-Frames auf dem Server (autoritativ).</summary>
        private int m_ServerAltAttackFramesRemaining;

        /// <summary>Frame-Akkumulator auf dem Server fuer frame-diskretes AltAttack-Timing.</summary>
        private float m_ServerAltAttackFrameAccumulator;

        /// <summary>Aktuelle AltAttack-Frame-Anzahl basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ServerAltAttackFrames;

        /// <summary>Aktuelle AltAttack-FPS basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ServerAltAttackFps = 20;

        /// <summary>Cooldown-Zeitstempel fuer Empty-Sound (verhindert Spam bei gehaltener Feuertaste).</summary>
        private float m_ServerNextEmptySoundTime;

        // ===== Server-Side Weapon Swap Gating =====

        /// <summary>Ob der Server gerade einen Waffenwechsel verarbeitet (Drop/Raise).</summary>
        private bool m_ServerIsSwapping;

        /// <summary>Aktuelle Phase beim Waffenwechsel (Drop/Raise).</summary>
        private WeaponSwapPhase m_ServerSwapPhase;

        /// <summary>Verbleibende Frames in der aktuellen Swap-Phase.</summary>
        private int m_ServerSwapFramesRemaining;

        /// <summary>Frame-Akkumulator fuer frame-diskretes Swap-Timing.</summary>
        private float m_ServerSwapFrameAccumulator;

        /// <summary>FPS der aktuellen Swap-Phase.</summary>
        private int m_ServerSwapFps = 10;

        /// <summary>Raise-Frame-Anzahl der Zielwaffe (fuer nach Drop).</summary>
        private int m_ServerSwapRaiseFrames;

        /// <summary>Raise-FPS der Zielwaffe.</summary>
        private int m_ServerSwapRaiseFps = 10;

        /// <summary>Name der Zielwaffe beim Waffenwechsel.</summary>
        private string m_ServerSwapTargetWeapon;

        // ===== Server-Side Grenade Cook/Throw (SoF2-authentic hold-to-cook) =====

        /// <summary>Ob gerade eine Granate gekocht wird (WEAPON_CHARGING Phase).</summary>
        private bool m_ServerIsGrenadeCooking;

        /// <summary>Verbleibende Cook-Frames (GRENADE_START Animation, nur fuer Animation-Blocking).</summary>
        private int m_ServerGrenadeCookFramesRemaining;

        /// <summary>Frame-Akkumulator fuer Grenade-Cook-Animation-Timing.</summary>
        private float m_ServerGrenadeCookFrameAccumulator;

        /// <summary>Cook-FPS (aus mp_attack Animation der Granate).</summary>
        private int m_ServerGrenadeCookFps = 20;

        /// <summary>
        /// SoF2 grenadeTimer: Zaehlt in Echtzeit herunter waehrend Granate gehalten wird.
        /// Startet bei projDef.Timer (z.B. 3.0s). Verbleibender Wert wird als Projektil-Timer genutzt.
        /// SoF2 bg_pmove.c:2867-2874: grenadeTimer -= pml.msec; if (grenadeTimer <= 0) grenadeTimer = 1;
        /// </summary>
        private float m_ServerGrenadeTimer;

        /// <summary>Ob die GRENADE_START Animation abgeschlossen ist (Spieler haelt Granate bereit).</summary>
        private bool m_ServerGrenadeAnimComplete;

        /// <summary>Gespeicherter PlayerCommand.PitchAngle (wird bei Wurf aktualisiert).</summary>
        private float m_ServerGrenadePitchAngle;

        /// <summary>Gespeicherter PlayerCommand.YawAngle (wird bei Wurf aktualisiert).</summary>
        private float m_ServerGrenadeYawAngle;

        /// <summary>Ob AltAttack-Granate (langsamerer Wurf, weniger Bounce).</summary>
        private bool m_ServerGrenadeIsAlt;

        /// <summary>Verbleibende Frames fuer die Throw-Follow-Through-Animation (mp_attackEnd).</summary>
        private int m_ServerGrenadeThrowFramesRemaining;

        /// <summary>Frame-Akkumulator fuer Grenade-Throw-Timing.</summary>
        private float m_ServerGrenadeThrowFrameAccumulator;

        /// <summary>Throw-FPS (aus mp_attackEnd Animation).</summary>
        private int m_ServerGrenadeThrowFps = 20;

        /// <summary>Minimaler grenadeTimer-Wert bevor Zwangs-Detonation (SoF2: 50ms).</summary>
        private const float GRENADE_MIN_TIMER = 0.05f;

        // ===== Movement Sync =====



        /// <summary>
        /// Wird auf dem Server aufgerufen: Spawn-Point zuweisen und Server-Position setzen.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            // Server-seitige Physik + BoxCollider initialisieren
            m_ServerPlayerCharacter.InitializeServer();

            // Weapon-Swap-Event abonnieren (Server verarbeitet Swap-Timing)
            m_CharacterState.OnWeaponSwapRequested += OnServerWeaponSwapRequested;

            // Spawn-Point vom Server zuweisen — ggf. warten bis Map geladen ist
            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.Log("[NetworkedPlayerCharacter] ServerPlayerSpawnPoints noch nicht verfügbar — warte auf Map-Laden.");
                StartCoroutine(WaitForMapAndPosition());
                return;
            }

            AssignSpawnPosition();
        }

        /// <summary>
        /// Wartet bis die Map geladen ist und ServerPlayerSpawnPoints verfügbar sind,
        /// weist dann die Spawn-Position zu.
        /// </summary>
        private IEnumerator WaitForMapAndPosition()
        {
            yield return new WaitUntil(() => ServerPlayerSpawnPoints.Instance != null);
            AssignSpawnPosition();
        }

        /// <summary>
        /// Weist dem Spieler einen Spawn-Point zu und aktiviert die Server-Physik.
        /// </summary>
        private void AssignSpawnPosition()
        {
            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            m_ServerPlayerCharacter.SetReady();
            Debug.Log($"[NetworkedPlayerCharacter] Server: Spieler gespawnt bei {position}");
        }

        /// <summary>
        /// Owner-Client: Client-Side Prediction starten.
        /// SoF2-Physik wird durch ClientPlayerCharacter aktiviert.
        /// Reconciliation erfolgt ausschliesslich ueber CorrectionClientRpc (explizite Server-Ablehnung),
        /// nicht ueber OnValueChanged — da die NetworkVariable-Aenderung erst nach Netzwerk-Roundtrip
        /// ankommt und der Client sich bis dahin schon weiter bewegt hat (stale ack).
        /// </summary>
        protected override void OnOwnerSpawn()
        {
            base.OnOwnerSpawn();

            // Animator-Referenz nach Visual-Instanziierung setzen
            SubscribeToVisualInstantiated();

            Debug.Log("[NetworkedPlayerCharacter] Owner: Client-Side Prediction aktiv");
        }

        /// <summary>
        /// Remote-Client: Nur Interpolation, kein Input.
        /// </summary>
        protected override void OnRemoteSpawn()
        {
            base.OnRemoteSpawn();

            // Animator-Referenz nach Visual-Instanziierung setzen
            SubscribeToVisualInstantiated();

            Debug.Log($"[NetworkedPlayerCharacter] Remote: Client {OwnerClientId} - Interpolation aktiv");
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeFromVisualInstantiated();

            if (IsServer)
            {
                m_CharacterState.OnWeaponSwapRequested -= OnServerWeaponSwapRequested;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            // Remote-Clients: Interpolation zur Server-Position + Animator treiben
            if (!IsOwner && !IsServer)
            {
                InterpolateRemotePosition();
                ApplyAnimationToAnimator(m_AnimationState.Value);
            }
        }

        /// <summary>
        /// Owner-Client: Sendet einen PlayerCommand an den Server zur autoritativen Verarbeitung.
        /// Wird von ClientPlayerCharacter aufgerufen nachdem Input lokal angewendet wurde.
        /// In Host-Mode: Physik läuft direkt, nur Server-Position aktualisieren.
        /// </summary>
        /// <param name="cmd">Der PlayerCommand mit Input-Daten und Sequenznummer.</param>
        public void SendPlayerCommand(PlayerCommand cmd)
        {
            if (!IsOwner)
            {
                return;
            }

            if (IsServer)
            {
                // Host-Mode: Physik läuft schon lokal, Server-Position direkt aktualisieren
                m_ServerPosition.Value = transform.position;
                m_ServerRotation.Value = transform.rotation;
                return;
            }

            SubmitCommandServerRpc(cmd);
        }

        /// <summary>
        /// Owner-Client: Sendet Capsule-Dimensionen an den Server (nach Bone-Berechnung).
        /// Server benötigt diese für identische Physik-Simulation.
        /// </summary>
        public void SendCapsuleDimensions(float height, float radius, Vector3 center)
        {
            if (!IsOwner)
            {
                return;
            }

            if (IsServer)
            {
                // Host-Mode: ServerPlayerCharacter direkt setzen
                m_ServerPlayerCharacter.SetCapsuleDimensions(height, radius, center);
                return;
            }

            SubmitCapsuleDimensionsServerRpc(height, radius, center);
        }

        /// <summary>
        /// Server: Empfängt einen PlayerCommand vom Client und führt identische Physik aus.
        /// Verarbeitet auch Button-Inputs (Attack, Use, etc.) wie SoF2 usercmd_t.
        /// Sendet Acknowledgement mit autoritativer Position zurück.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SubmitCommandServerRpc(PlayerCommand cmd)
        {
            // Server-seitige Physik-Simulation ausführen
            ServerMovementAck ack = m_ServerPlayerCharacter.ProcessCommand(cmd);

            // Server-Position als Source of Truth aktualisieren
            m_ServerPosition.Value = ack.Position;
            m_ServerRotation.Value = Quaternion.Euler(0f, cmd.YawAngle, 0f);

            // Server-seitiges Attack-Frame-Counting herunterzaehlen (wie SoF2 weaponTime)
            if (m_ServerAttackFramesRemaining > 0)
            {
                m_ServerAttackFrameAccumulator += cmd.DeltaTime;
                float frameInterval = 1f / m_ServerAttackFps;
                while (m_ServerAttackFrameAccumulator >= frameInterval && m_ServerAttackFramesRemaining > 0)
                {
                    m_ServerAttackFrameAccumulator -= frameInterval;
                    m_ServerAttackFramesRemaining--;
                }
            }

            // Server-seitiges Reload-Frame-Counting herunterzaehlen
            if (m_ServerReloadFramesRemaining > 0)
            {
                if (m_ServerIsShellReload && m_ServerShellReloadPhase != ShellReloadPhase.None)
                {
                    TickServerShellReload(cmd.DeltaTime);
                }
                else
                {
                    m_ServerReloadFrameAccumulator += cmd.DeltaTime;
                    float reloadFrameInterval = 1f / m_ServerReloadFps;
                    while (m_ServerReloadFrameAccumulator >= reloadFrameInterval && m_ServerReloadFramesRemaining > 0)
                    {
                        m_ServerReloadFrameAccumulator -= reloadFrameInterval;
                        m_ServerReloadFramesRemaining--;
                    }

                    // Reload abgeschlossen: Munition transferieren
                    if (m_ServerReloadFramesRemaining <= 0)
                    {
                        m_CharacterState.CompleteReload();
                    }
                }
            }

            // Server-seitiges AltAttack-Frame-Counting herunterzaehlen
            if (m_ServerAltAttackFramesRemaining > 0)
            {
                m_ServerAltAttackFrameAccumulator += cmd.DeltaTime;
                float altAttackFrameInterval = 1f / m_ServerAltAttackFps;
                while (m_ServerAltAttackFrameAccumulator >= altAttackFrameInterval && m_ServerAltAttackFramesRemaining > 0)
                {
                    m_ServerAltAttackFrameAccumulator -= altAttackFrameInterval;
                    m_ServerAltAttackFramesRemaining--;
                }
            }

            // Server-seitiges Weapon-Swap-Frame-Counting herunterzaehlen (Drop → Raise)
            if (m_ServerIsSwapping)
            {
                TickServerWeaponSwap(cmd.DeltaTime);
            }

            // Server-seitiges Grenade-Cook/Throw herunterzaehlen
            if (m_ServerIsGrenadeCooking)
            {
                TickServerGrenadeCook(cmd);
            }
            else if (m_ServerGrenadeThrowFramesRemaining > 0)
            {
                TickServerGrenadeThrow(cmd.DeltaTime);
            }

            // Button-Inputs verarbeiten (SoF2: FireWeapon aus usercmd_t.buttons)
            // Server gated: Attack nur starten wenn keine Attacke, kein Reload, kein AltAttack, kein Swap und keine Granate laeuft (Anti-Cheat)
            bool noActionRunning = m_ServerAttackFramesRemaining <= 0
                && m_ServerReloadFramesRemaining <= 0
                && m_ServerAltAttackFramesRemaining <= 0
                && !m_ServerIsSwapping
                && !m_ServerIsGrenadeCooking
                && m_ServerGrenadeThrowFramesRemaining <= 0;

            if (cmd.HasButton(CommandButtons.Attack) && noActionRunning)
            {
                ProcessAttack(cmd);
            }

            // Server gated: AltAttack nur starten wenn keine Action laeuft
            if (cmd.HasButton(CommandButtons.AltAttack) && noActionRunning)
            {
                ProcessAltAttack(cmd);
            }

            // Server gated: Reload nur starten wenn keine Action laeuft und Reload moeglich
            if (cmd.HasButton(CommandButtons.Reload) && noActionRunning)
            {
                ProcessReload();
            }

            // Acknowledgement an Owner-Client senden (für Reconciliation)
            MovementAckClientRpc(ack);
        }

        /// <summary>
        /// Server: Empfängt Capsule-Dimensionen vom Client und setzt sie auf der Server-Simulation.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SubmitCapsuleDimensionsServerRpc(float height, float radius, Vector3 center)
        {
            m_ServerPlayerCharacter.SetCapsuleDimensions(height, radius, center);
        }

        /// <summary>
        /// Server → Owner-Client: Acknowledgement mit autoritativer Position und State.
        /// Client nutzt dies für Prediction-Reconciliation (Vergleich + ggf. Replay).
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void MovementAckClientRpc(ServerMovementAck ack)
        {
            OnMovementAcknowledged?.Invoke(ack);
        }

        /// <summary>
        /// Server → Owner-Client: Hard-Correction (Respawn, Teleport, Anti-Cheat).
        /// Überschreibt Client-Position ohne Reconciliation.
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void CorrectionClientRpc(Vector3 correctPosition, Quaternion correctRotation)
        {
            // Owner-Client: Server hat die Position korrigiert → Prediction überschreiben
            transform.SetPositionAndRotation(correctPosition, correctRotation);
            Debug.LogWarning($"[NetworkedPlayerCharacter] Owner: Position vom Server korrigiert auf {correctPosition}");
        }

        /// <summary>
        /// Remote-Client: Smooth Interpolation zur Server-Position.
        /// </summary>
        private void InterpolateRemotePosition()
        {
            
            transform.SetPositionAndRotation(Vector3.Lerp(
                transform.position,
                m_ServerPosition.Value,
                Time.deltaTime * k_RemoteInterpolationSpeed
            ), Quaternion.Lerp(
                transform.rotation,
                m_ServerRotation.Value,
                Time.deltaTime * k_RemoteInterpolationSpeed
            ));

        }

        /// <summary>
        /// Server: Verarbeitet einen Attack aus dem PlayerCommand.
        /// Wie SoF2 FireWeapon() in g_weapon.c — wird aus dem usercmd_t gelesen,
        /// nicht als separater RPC gesendet. Position + Blickrichtung sind exakt synchron.
        /// Unterstuetzt Multi-Pellet (Schrotflinten), Inaccuracy-Buildup bei Dauerfeuer,
        /// und Projektil-Waffen (RPG7, MM1, F1 Grenade).
        /// </summary>
        private void ProcessAttack(PlayerCommand cmd)
        {
            // Munition verbrauchen (Server-autoritativ)
            if (!m_CharacterState.TryConsumeAmmo())
            {
                // Empty-Sound abspielen (Dry-Fire Click — SoF2: leeres Magazin beim Feuern)
                if (Time.time >= m_ServerNextEmptySoundTime)
                {
                    WeaponDataLoader emptyLoader = ServiceLocator.Get<WeaponDataLoader>();
                    WeaponDefinition emptyWeapon = emptyLoader?.GetById(m_CharacterState.CurrentWeaponName);
                    string emptySoundPath = ResolveWeaponSoundPath(emptyWeapon, "empty");
                    if (!string.IsNullOrEmpty(emptySoundPath))
                    {
                        WeaponEmptySoundClientRpc(emptySoundPath);
                        m_ServerNextEmptySoundTime = Time.time + 0.4f;
                    }
                }

                return;
            }

            // Attack-Parameter von aktueller Waffe laden
            UpdateServerAttackParameters();

            // Waffen-Definition laden
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
            WeaponAttackDefinition attackDef = weapon?.Attack;

            if (attackDef == null)
            {
                Debug.LogWarning($"[NetworkedPlayerCharacter] Server: No attack definition for weapon '{m_CharacterState.CurrentWeaponName}'");
                return;
            }

            // Projektil-Waffen: Granaten starten Cook/Throw, RPG/MM1 spawnen sofort
            if (attackDef.Projectile != null)
            {
                ProcessProjectileAttack(cmd, attackDef, weapon, false);
                return;
            }

            // Server startet Attack-Cooldown (frame-basiert, wie SoF2 weaponTime)
            m_ServerAttackFramesRemaining = m_ServerAttackFrames;
            m_ServerAttackFrameAccumulator = 0f;

            // Eye-Position und Blickrichtung vom Server berechnen
            Vector3 eyePos = m_ServerPlayerCharacter.GetEyePosition();
            Quaternion aimRotation = Quaternion.Euler(cmd.PitchAngle, cmd.YawAngle, 0f);

            // Inaccuracy-Buildup: Streuung steigt bei Dauerfeuer von Inaccuracy → MaxInaccuracy
            // Decay: 0.5s ohne Schuss → resettet auf Basis-Inaccuracy
            float currentTime = Time.time;
            float timeSinceLastShot = currentTime - m_ServerLastShotTime;
            const float INACCURACY_DECAY_TIME = 0.5f;
            const float INACCURACY_BUILDUP_STEP = 0.3f;

            if (timeSinceLastShot > INACCURACY_DECAY_TIME)
            {
                m_ServerAccumulatedInaccuracy = attackDef.Inaccuracy;
            }
            else
            {
                m_ServerAccumulatedInaccuracy = Mathf.Min(
                    m_ServerAccumulatedInaccuracy + INACCURACY_BUILDUP_STEP,
                    attackDef.MaxInaccuracy > 0f ? attackDef.MaxInaccuracy : attackDef.Inaccuracy
                );
            }

            m_ServerLastShotTime = currentTime;
            float spread = m_ServerAccumulatedInaccuracy;

            // Reichweite: SoF2-Units → Unity-Meter (1 QU = 0.0254m)
            float rangeMeters = attackDef.Range * SOF2_UNIT_SCALE;

            // Eigenen Collider deaktivieren fuer Self-Hit-Vermeidung
            m_ServerPlayerCharacter.SetPhysicsColliderEnabled(false);

            int hitboxLayerMask = LayerMask.GetMask(HITBOX_LAYER_NAME);

            // Pellet-Anzahl: Schrotflinten feuern mehrere Pellets pro Schuss (z.B. M590: 8)
            int pelletCount = attackDef.Pellets > 0 ? attackDef.Pellets : 1;

            // SoF2 bg_weapons.c:278-281: spread ist Fallback fuer inaccuracy wenn 0,
            // UND wird als zusaetzlicher Pellet-Spread-Radius bei Schrotflinten verwendet.
            float pelletSpread = attackDef.Spread;

            // Munitionstyp fuer Impact-Effekt-Lookup
            string ammoType = weapon?.Ammo?.Type ?? "";

            // Aim-Vektoren fuer SoF2-konforme Streuung (BG_CalculateBulletEndpoint)
            Vector3 aimForward = aimRotation * Vector3.forward;
            Vector3 aimRight = aimRotation * Vector3.right;
            Vector3 aimUp = aimRotation * Vector3.up;

            // Shellsound: Surface unter dem Schuetzen bestimmt Huelsen-Aufprall-Sound
            string shellsoundPath = "";
            SurfaceImpactDataLoader shellSurfaceLoader = ServiceLocator.Get<SurfaceImpactDataLoader>();
            if (shellSurfaceLoader != null)
            {
                string shooterSurface = DetectSurfaceAtPosition(transform.position);
                shellsoundPath = shellSurfaceLoader.GetShellsoundPath(shooterSurface, ammoType);
            }

            // Fire-Sound aus Waffen-Definition aufloesen (SoF2: flashSound)
            string fireSoundPath = ResolveWeaponSoundPath(weapon, "fire");

            // Muzzle-Effekte (Flash, Smoke, Shell, Shellsound, Fire-Sound) einmal pro Schuss an alle Clients
            MuzzleEffectsClientRpc(
                attackDef.MuzzleFlash ?? "",
                attackDef.MuzzleSmoke ?? "",
                attackDef.ShellCasingEject ?? "",
                attackDef.EjectBone ?? "",
                shellsoundPath,
                fireSoundPath,
                attackDef.Volume > 0f ? attackDef.Volume : 1f
            );

            for (int i = 0; i < pelletCount; i++)
            {
                // SoF2-konforme Streuung: inaccuracy + pelletSpread (Schrotflinten-Kegel)
                float totalSpread = spread + pelletSpread;
                Vector3 aimDirection = ApplyInaccuracySoF2(aimForward, aimRight, aimUp, totalSpread);

                // Server-seitiger Hitscan-Raycast auf Hitbox-Layer
                bool didHit = Physics.Raycast(eyePos, aimDirection, out RaycastHit hit, rangeMeters, hitboxLayerMask);

                // Welt-Geometrie-Raycast fuer Impact-Effekte (alles ausser Hitboxes)
                int worldLayerMask = ~hitboxLayerMask;
                bool didHitWorld = Physics.Raycast(eyePos, aimDirection, out RaycastHit worldHit, rangeMeters, worldLayerMask);

                // Endpunkt: naechster Treffer (Hitbox oder Welt) oder Max-Range
                float hitboxDist = didHit ? hit.distance : float.MaxValue;
                float worldDist = didHitWorld ? worldHit.distance : float.MaxValue;

                Vector3 hitPoint;
                Vector3 impactNormal = Vector3.zero;
                string impactEffectId = "";
                string debrisEffectId = "";
                string impactSoundPath = "";

                if (hitboxDist <= worldDist && didHit)
                {
                    // Hitbox naeher: Spieler getroffen
                    hitPoint = hit.point;
                }
                else if (didHitWorld)
                {
                    // Welt-Geometrie naeher: Wand/Boden getroffen
                    hitPoint = worldHit.point;
                    impactNormal = worldHit.normal;

                    // Surface-Typ vom Collider lesen
                    SurfaceTypeMarker surfaceMarker = worldHit.collider.GetComponentInParent<SurfaceTypeMarker>();
                    string surfaceType = surfaceMarker != null ? surfaceMarker.SurfaceType : "default";

                    // Impact-Effect-ID via SurfaceImpactDataLoader ermitteln
                    SurfaceImpactDataLoader surfaceLoader = ServiceLocator.Get<SurfaceImpactDataLoader>();
                    if (surfaceLoader != null)
                    {
                        impactEffectId = surfaceLoader.GetImpactEffectId(surfaceType, ammoType);
                        debrisEffectId = surfaceLoader.GetDebrisEffectId(surfaceType, ammoType);
                        impactSoundPath = surfaceLoader.GetImpactSoundPath(surfaceType, ammoType);
                    }
                }
                else
                {
                    hitPoint = eyePos + aimDirection * rangeMeters;
                }

                if (didHit && hitboxDist <= worldDist)
                {
                    HitboxCollider hitbox = hit.collider.GetComponent<HitboxCollider>();
                    if (hitbox != null)
                    {
                        // Damage berechnen: Basis-Damage × Region-Multiplikator (pro Pellet)
                        int finalDamage = Mathf.RoundToInt(attackDef.Damage * hitbox.DamageMultiplier);

                        // Getroffenen Spieler finden und Schaden anwenden
                        NetworkedCharacterState targetState = hit.collider.GetComponentInParent<NetworkedCharacterState>();
                        if (targetState != null && targetState != m_CharacterState)
                        {
                            int newHealth = Mathf.Max(0, targetState.Health - finalDamage);
                            targetState.SetHealth(newHealth);

                            Debug.Log($"[NetworkedPlayerCharacter] Server: HIT! Client {OwnerClientId} → {targetState.CharacterName} | Pellet={i + 1}/{pelletCount} | Region={hitbox.HitRegion} | Damage={finalDamage} (Base={attackDef.Damage} × {hitbox.DamageMultiplier:F2}) | Health={newHealth}");
                        }
                    }
                }

                // Tracer + Impact pro Pellet an alle Clients senden
                string tracerEffectId = attackDef.TracerEffect ?? "";
                TracerClientRpc(eyePos, hitPoint, impactNormal, tracerEffectId, impactEffectId, debrisEffectId, impactSoundPath);
            }

            // Eigenen Collider wieder aktivieren
            m_ServerPlayerCharacter.SetPhysicsColliderEnabled(true);

            // KickAngles: Rueckstoss an Owner-Client senden (SoF2 AddViewKick)
            // Format: [minPitch, maxPitch, minYaw, maxYaw]
            // SoF2 Skalierung: kickPitch += value * 500, extract as /1000 → effektiv ×0.5
            if (attackDef.KickAngles != null && attackDef.KickAngles.Count >= 4)
            {
                float pitchKick = UnityEngine.Random.Range(attackDef.KickAngles[0], attackDef.KickAngles[1]) * 0.5f;
                float yawKick = UnityEngine.Random.Range(attackDef.KickAngles[2], attackDef.KickAngles[3]) * 0.5f;
                ApplyKickAnglesClientRpc(pitchKick, yawKick);
            }
        }

        /// <summary>
        /// Aktualisiert Attack-Frames und FPS basierend auf der aktuellen Waffe des Characters.
        /// Liest mp_attack aus dem WeaponDataLoader.
        /// </summary>
        private void UpdateServerAttackParameters()
        {
            if (m_CharacterState == null)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(m_CharacterState.CurrentWeaponName);
            if (weapon?.Animations == null)
            {
                return;
            }

            if (weapon.Animations.TryGetValue("mp_attack", out WeaponAnimationEntry attackAnim))
            {
                m_ServerAttackFrames = attackAnim.Duration;
                m_ServerAttackFps = attackAnim.Fps;
            }

            // SoF2 fireDelay (Millisekunden): Minimale Zeit zwischen Schuessen.
            // Wenn fireDelay laenger als die Animation dauert, wird es als Cooldown verwendet.
            WeaponAttackDefinition attackDef = weapon.Attack;
            if (attackDef != null && attackDef.FireDelay > 0 && m_ServerAttackFps > 0)
            {
                int fireDelayFrames = Mathf.CeilToInt((attackDef.FireDelay / 1000f) * m_ServerAttackFps);
                m_ServerAttackFrames = Mathf.Max(m_ServerAttackFrames, fireDelayFrames);
            }
        }

        /// <summary>
        /// SoF2-konforme Streuungsberechnung basierend auf BG_CalculateBulletEndpoint (bg_weapons.c).
        /// Verwendet 0.05 * inaccuracy als Abweichung auf right/up-Vektoren mit Gausscher Verteilung.
        /// Die Gaussian-Verteilung (Summe zweier Gleichverteilungen → Dreiecksverteilung) sorgt fuer
        /// SoF2-typische Streuung: Pellets konzentrieren sich zur Mitte, wenige aussen.
        /// </summary>
        private Vector3 ApplyInaccuracySoF2(Vector3 forward, Vector3 right, Vector3 up, float inaccuracy)
        {
            if (inaccuracy <= 0f)
            {
                return forward;
            }

            // SoF2 Gaussian: Summe zweier Gleichverteilungen [0,1) - 0.5 → Dreiecksverteilung [-1, 1]
            // Rejection Sampling: nur Punkte innerhalb des Einheitskreises (wie SoF2 bg_weapons.c:1237-1250)
            float gaussianX;
            float gaussianY;
            do
            {
                float f1 = UnityEngine.Random.value;
                float f2 = UnityEngine.Random.value;
                gaussianX = (f1 - 0.5f) + (f2 - 0.5f);

                f1 = UnityEngine.Random.value;
                f2 = UnityEngine.Random.value;
                gaussianY = (f1 - 0.5f) + (f2 - 0.5f);
            }
            while (gaussianX * gaussianX + gaussianY * gaussianY >= 1f);

            // SoF2: 0.05f * inaccuracy * gaussian als Offset auf normalisierte right/up-Vektoren
            float spreadFactor = 0.05f * inaccuracy;
            Vector3 direction = forward + spreadFactor * gaussianX * right + spreadFactor * gaussianY * up;

            return direction.normalized;
        }

        // ===== Projectile Weapons (RPG7, MM1, F1 Grenade) =====

        /// <summary>
        /// Server: Verarbeitet einen Projektil-Angriff.
        /// Fuer Timer-Granaten (F1): Startet SoF2-authentische Hold-to-Cook-Phase.
        /// Spieler haelt Button → grenadeTimer zaehlt in Echtzeit herunter.
        /// Button losgelassen → Wurf mit verbleibendem Timer.
        /// Fuer Impact-Projektile (RPG7, MM1): Spawnt sofort ein ServerProjectile.
        /// </summary>
        private void ProcessProjectileAttack(PlayerCommand cmd, WeaponAttackDefinition attackDef, WeaponDefinition weapon, bool isAlt)
        {
            WeaponProjectileDefinition projDef = attackDef.Projectile;

            if (projDef.Detonation == "timer")
            {
                // SoF2 bg_pmove.c:3289-3303: grenadeTimer = attackData->projectileLifetime
                // Starte WEAPON_CHARGING Phase — Timer zaehlt in Echtzeit herunter
                string attackAnimKey = isAlt ? "mp_altAttack" : "mp_attack";
                if (weapon.Animations != null && weapon.Animations.TryGetValue(attackAnimKey, out WeaponAnimationEntry cookAnim))
                {
                    m_ServerGrenadeCookFramesRemaining = cookAnim.Duration;
                    m_ServerGrenadeCookFps = cookAnim.Fps;
                }
                else
                {
                    m_ServerGrenadeCookFramesRemaining = 23;
                    m_ServerGrenadeCookFps = 20;
                }

                m_ServerIsGrenadeCooking = true;
                m_ServerGrenadeCookFrameAccumulator = 0f;
                m_ServerGrenadePitchAngle = cmd.PitchAngle;
                m_ServerGrenadeYawAngle = cmd.YawAngle;
                m_ServerGrenadeIsAlt = isAlt;
                m_ServerGrenadeTimer = projDef.Timer;
                m_ServerGrenadeAnimComplete = false;

                // Attack-Cooldown: Cook-Frames blockieren weitere Aktionen
                // (wird von TickServerGrenadeCook zurueckgesetzt sobald Button losgelassen)
                m_ServerAttackFramesRemaining = 9999;
                m_ServerAttackFrameAccumulator = 0f;
                m_ServerAttackFps = m_ServerGrenadeCookFps;

                Debug.Log($"[NetworkedPlayerCharacter] Server: Grenade cook started for client {OwnerClientId} — Timer={projDef.Timer:F2}s (isAlt={isAlt})");

                // SoF2 Grenade-Sequenz: pinRattle + pinPull beim Cook-Start
                string pinRattlePath = ResolveWeaponSoundPath(weapon, "pinRattle");
                string pinPullPath = ResolveWeaponSoundPath(weapon, "pinPull");
                GrenadeSoundClientRpc(pinRattlePath);
                GrenadeSoundClientRpc(pinPullPath);

                return;
            }

            // Impact/Sticky-Projektile (RPG7, MM1, Knife-Throw): Sofort spawnen
            // AltAttack benutzt eigene Frames (mp_altAttack), Attack benutzt mp_attack
            if (isAlt)
            {
                m_ServerAltAttackFramesRemaining = m_ServerAltAttackFrames;
                m_ServerAltAttackFrameAccumulator = 0f;
            }
            else
            {
                m_ServerAttackFramesRemaining = m_ServerAttackFrames;
                m_ServerAttackFrameAccumulator = 0f;
            }

            Vector3 eyePos = m_ServerPlayerCharacter.GetEyePosition();
            Vector3 aimDirection = Quaternion.Euler(cmd.PitchAngle, cmd.YawAngle, 0f) * Vector3.forward;

            // Fire-Sound aus Waffen-Definition (SoF2: flashSound)
            string projFireSoundPath = ResolveWeaponSoundPath(weapon, isAlt ? "altFire" : "fire");

            // Muzzle-Effekte fuer Sofort-Projektile (RPG, MM1) — nicht fuer gekochte Granaten
            MuzzleEffectsClientRpc(
                attackDef.MuzzleFlash ?? "",
                attackDef.MuzzleSmoke ?? "",
                attackDef.ShellCasingEject ?? "",
                attackDef.EjectBone ?? "",
                "",
                projFireSoundPath,
                attackDef.Volume > 0f ? attackDef.Volume : 1f
            );

            SpawnProjectile(eyePos, aimDirection, attackDef, projDef);

            // KickAngles: Rueckstoss an Owner-Client senden
            // SoF2 Skalierung: ×0.5 (kickPitch += value*500, extract /1000)
            if (attackDef.KickAngles != null && attackDef.KickAngles.Count >= 4)
            {
                float pitchKick = UnityEngine.Random.Range(attackDef.KickAngles[0], attackDef.KickAngles[1]) * 0.5f;
                float yawKick = UnityEngine.Random.Range(attackDef.KickAngles[2], attackDef.KickAngles[3]) * 0.5f;
                ApplyKickAnglesClientRpc(pitchKick, yawKick);
            }

            Debug.Log($"[NetworkedPlayerCharacter] Server: Projectile ({projDef.Detonation}) spawned for client {OwnerClientId} — Speed={projDef.Speed} Gravity={projDef.Gravity}");
        }

        /// <summary>
        /// Server: Spawnt ein ServerProjectile-GameObject mit den angegebenen Parametern.
        /// Das Projektil simuliert sich selbst (Flugbahn, Kollision, Detonation, Explosions-Damage).
        /// </summary>
        private void SpawnProjectile(Vector3 spawnPosition, Vector3 direction, WeaponAttackDefinition attackDef, WeaponProjectileDefinition projDef)
        {
            uint projectileId = m_NextProjectileId++;
            GameObject projectileObj = new($"Projectile_{m_CharacterState.CurrentWeaponName}_{OwnerClientId}");
            ServerProjectile projectile = projectileObj.AddComponent<ServerProjectile>();

            float timer = projDef.Timer;
            // Fuer gekochte Granaten: Timer wurde waehrend Cook reduziert
            // (wird von TickServerGrenadeCook uebergeben, hier nur Default)

            projectile.Initialize(
                spawnPosition,
                direction,
                projDef.Speed,
                projDef.Gravity,
                projDef.Bounce,
                projDef.Detonation,
                timer,
                attackDef.Damage,
                attackDef.Radius,
                attackDef.Knockback,
                OwnerClientId,
                m_CharacterState.CurrentWeaponName,
                projectileId
            );

            // Sticky-Pickup: Callback registrieren fuer Visual-Cleanup
            if (projDef.Detonation == "sticky")
            {
                projectile.OnPickedUp += OnStickyProjectilePickedUp;
            }

            // Visual-RPC an alle Clients fuer Projektil-Visualisierung
            string effectPath = projDef.Effect ?? "";
            string explosionEffectPath = projDef.ExplosionEffect ?? "";
            string modelKey = projDef.Model ?? "";
            string loopSoundPath = projDef.LoopSound ?? "";
            ProjectileSpawnClientRpc(spawnPosition, direction, projDef.Speed, projDef.Gravity, projDef.Bounce, projDef.Detonation ?? "impact", timer, projectileId, effectPath, explosionEffectPath, modelKey, loopSoundPath);
        }

        /// <summary>
        /// Server: Tickt die Grenade-Cook-Phase (GRENADE_START Animation).
        /// SoF2-authentisch: grenadeTimer zaehlt in Echtzeit herunter waehrend Button gehalten.
        /// Button losgelassen → Wurf mit verbleibendem Timer.
        /// Timer abgelaufen → Zwangs-Wurf (explodiert fast sofort).
        /// SoF2 bg_pmove.c:2866-2874: grenadeTimer -= pml.msec
        /// SoF2 g_weapon.c:724-736: projectileLifetime = grenadeTimer; if (< 50) → explode
        /// </summary>
        private void TickServerGrenadeCook(PlayerCommand cmd)
        {
            // GRENADE_START Animation-Frames herunterzaehlen (nur fuer Animation-Blocking)
            if (m_ServerGrenadeCookFramesRemaining > 0)
            {
                m_ServerGrenadeCookFrameAccumulator += cmd.DeltaTime;
                float frameInterval = 1f / m_ServerGrenadeCookFps;

                while (m_ServerGrenadeCookFrameAccumulator >= frameInterval && m_ServerGrenadeCookFramesRemaining > 0)
                {
                    m_ServerGrenadeCookFrameAccumulator -= frameInterval;
                    m_ServerGrenadeCookFramesRemaining--;
                }

                if (m_ServerGrenadeCookFramesRemaining <= 0)
                {
                    m_ServerGrenadeAnimComplete = true;
                }
            }

            // SoF2: grenadeTimer zaehlt in Echtzeit herunter waehrend gehalten
            m_ServerGrenadeTimer -= cmd.DeltaTime;

            // Blickrichtung aktualisieren (Spieler kann sich waehrend Cook drehen)
            m_ServerGrenadePitchAngle = cmd.PitchAngle;
            m_ServerGrenadeYawAngle = cmd.YawAngle;

            // SoF2 g_weapon.c:734-736: Timer abgelaufen → Explosion in der Hand
            // Spieler MUSS manuell loslassen — kein Auto-Throw bei Timer-Ablauf.
            bool timerExpired = m_ServerGrenadeTimer <= GRENADE_MIN_TIMER;

            // Pruefe ob Button losgelassen wurde (SoF2: !(attackButtons & BUTTON_ATTACK))
            bool buttonHeld = m_ServerGrenadeIsAlt
                ? cmd.HasButton(CommandButtons.AltAttack)
                : cmd.HasButton(CommandButtons.Attack);

            bool shouldThrow = !buttonHeld && m_ServerGrenadeAnimComplete;

            // Timer abgelaufen und Button noch gehalten → Granate explodiert in der Hand
            if (timerExpired && buttonHeld)
            {
                shouldThrow = true;
            }
            // Timer abgelaufen und Button gerade losgelassen → sofortige Detonation
            else if (timerExpired)
            {
                shouldThrow = true;
            }

            if (!shouldThrow)
            {
                return;
            }

            // Granate werfen
            m_ServerIsGrenadeCooking = false;
            m_ServerAttackFramesRemaining = 0;

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
            WeaponAttackDefinition attackDef = m_ServerGrenadeIsAlt ? weapon?.AltAttack : weapon?.Attack;
            WeaponProjectileDefinition projDef = attackDef?.Projectile;

            if (attackDef == null || projDef == null)
            {
                Debug.LogWarning("[NetworkedPlayerCharacter] Server: Grenade cook finished but no projectile definition found");
                return;
            }

            // SoF2 Grenade-Sequenz: handleRelease + throw/toss beim Wurf (nur wenn nicht in Hand explodiert)
            if (!timerExpired)
            {
                string handleReleasePath = ResolveWeaponSoundPath(weapon, "handleRelease");
                string throwSoundPath = ResolveWeaponSoundPath(weapon, m_ServerGrenadeIsAlt ? "toss" : "throw");
                GrenadeSoundClientRpc(handleReleasePath);
                GrenadeSoundClientRpc(throwSoundPath);
            }

            // Spawn-Position und Richtung zum Zeitpunkt des Wurfs
            Vector3 eyePos = m_ServerPlayerCharacter.GetEyePosition();
            Vector3 aimDirection = Quaternion.Euler(m_ServerGrenadePitchAngle, m_ServerGrenadeYawAngle, 0f) * Vector3.forward;

            // Timer abgelaufen → Granate explodiert sofort in der Hand (Timer=0, Speed=0, keine Gravitation)
            // "timer" mit 0 Sekunden detoniert im ersten Frame; "impact" wuerde Raycast-Kollision brauchen
            string detonation = timerExpired ? "timer" : projDef.Detonation;
            float spawnSpeed = timerExpired ? 0f : projDef.Speed;
            float spawnGravity = timerExpired ? 0f : projDef.Gravity;
            float spawnBounce = timerExpired ? 0f : projDef.Bounce;
            float remainingTimer = timerExpired ? 0f : Mathf.Max(m_ServerGrenadeTimer, GRENADE_MIN_TIMER);

            // Projektil spawnen mit verbleibendem Timer
            uint projectileId = m_NextProjectileId++;
            GameObject projectileObj = new($"Grenade_{m_CharacterState.CurrentWeaponName}_{OwnerClientId}");
            ServerProjectile projectile = projectileObj.AddComponent<ServerProjectile>();

            projectile.Initialize(
                eyePos,
                aimDirection,
                spawnSpeed,
                spawnGravity,
                spawnBounce,
                detonation,
                remainingTimer,
                attackDef.Damage,
                attackDef.Radius,
                attackDef.Knockback,
                OwnerClientId,
                m_CharacterState.CurrentWeaponName,
                projectileId
            );

            // Visual-RPC an alle Clients
            string grenadeEffectPath = projDef.Effect ?? "";
            string grenadeExplosionEffectPath = projDef.ExplosionEffect ?? "";
            string grenadeModelKey = projDef.Model ?? "";
            string grenadeLoopSoundPath = projDef.LoopSound ?? "";
            ProjectileSpawnClientRpc(eyePos, aimDirection, spawnSpeed, spawnGravity, spawnBounce, detonation ?? "timer", remainingTimer, projectileId, grenadeEffectPath, grenadeExplosionEffectPath, grenadeModelKey, grenadeLoopSoundPath);

            // Throw-Follow-Through-Phase starten (mp_attackEnd = GRENADE_END)
            // Bei In-Hand-Explosion: kuerzere Recovery, keine Wurf-Animation
            if (!timerExpired)
            {
                if (weapon?.Animations != null && weapon.Animations.TryGetValue("mp_attackEnd", out WeaponAnimationEntry throwAnim))
                {
                    m_ServerGrenadeThrowFramesRemaining = throwAnim.Duration;
                    m_ServerGrenadeThrowFps = throwAnim.Fps;
                }
                else
                {
                    m_ServerGrenadeThrowFramesRemaining = 18;
                    m_ServerGrenadeThrowFps = 20;
                }
            }
            else
            {
                // In-Hand-Explosion: minimale Recovery
                m_ServerGrenadeThrowFramesRemaining = 5;
                m_ServerGrenadeThrowFps = 20;
            }

            m_ServerGrenadeThrowFrameAccumulator = 0f;

            Debug.Log($"[NetworkedPlayerCharacter] Server: Grenade {(timerExpired ? "EXPLODED IN HAND" : "thrown")} for client {OwnerClientId} — RemainingTimer={remainingTimer:F2}s, TimerExpired={timerExpired}, ThrowFrames={m_ServerGrenadeThrowFramesRemaining}");
        }

        /// <summary>
        /// Server: Tickt die Grenade-Throw-Follow-Through-Phase (GRENADE_END Animation).
        /// Blockiert weitere Aktionen bis die Throw-Animation abgespielt ist.
        /// </summary>
        private void TickServerGrenadeThrow(float deltaTime)
        {
            m_ServerGrenadeThrowFrameAccumulator += deltaTime;
            float frameInterval = 1f / m_ServerGrenadeThrowFps;

            while (m_ServerGrenadeThrowFrameAccumulator >= frameInterval && m_ServerGrenadeThrowFramesRemaining > 0)
            {
                m_ServerGrenadeThrowFrameAccumulator -= frameInterval;
                m_ServerGrenadeThrowFramesRemaining--;
            }

            // Throw-Follow-Through fertig: Automatisch nachladen (Granaten haben kein mp_reload)
            if (m_ServerGrenadeThrowFramesRemaining <= 0 && m_CharacterState.CanReload())
            {
                m_CharacterState.CompleteReload();
                Debug.Log($"[NetworkedPlayerCharacter] Server: Grenade auto-reload after throw for client {OwnerClientId}");
            }
        }

        /// <summary>
        /// Server → Alle Clients: Projektil-Spawn fuer Client-seitige Visualisierung.
        /// Erstellt ein ClientProjectileVisual mit datengetriebenem Trail-Effekt auf allen Clients.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void ProjectileSpawnClientRpc(Vector3 spawnPosition, Vector3 direction, float speed, float gravity, float bounce, string detonation, float timer, uint projectileId, string effectId, string explosionEffectId, string modelKey, string loopSoundPath)
        {
            GameObject visualObj = new($"ProjectileVisual_{OwnerClientId}");
            ClientProjectileVisual visual = visualObj.AddComponent<ClientProjectileVisual>();
            visual.Initialize(spawnPosition, direction, speed, gravity, bounce, detonation, timer, projectileId, effectId, explosionEffectId, modelKey, loopSoundPath);
        }

        /// <summary>
        /// Server-Callback: Sticky-Projektil wurde aufgehoben.
        /// Sendet Destroy-RPC an alle Clients fuer Visual-Cleanup.
        /// </summary>
        private void OnStickyProjectilePickedUp(uint projectileId)
        {
            DestroyProjectileVisualClientRpc(projectileId);
        }

        /// <summary>
        /// Server → Alle Clients: Zerstoert das Client-Visual eines aufgehobenen Sticky-Projektils.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void DestroyProjectileVisualClientRpc(uint projectileId)
        {
            ClientProjectileVisual.DestroyById(projectileId);
        }

        /// <summary>
        /// Server → Alle Clients: Tracer-Visualisierung + Impact-Effekt + Debris fuer Hitscan-Waffen.
        /// Spawnt einen datengetriebenen TracerVisual (TrailRenderer) der vom ejectBone
        /// der aktuellen Waffe zum HitPoint fliegt. Bei Welt-Treffer wird zusaetzlich ein
        /// Impact-Effekt (Staub, Funken, Einschussloch) und Surface-Debris an der Einschlagstelle gespawnt.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void TracerClientRpc(Vector3 serverStart, Vector3 end, Vector3 hitNormal,
            string tracerEffectId, string impactEffectId, string debrisEffectId, string impactSoundPath)
        {
            // EjectBone der aktuellen Waffe als Tracer-Startpunkt suchen
            Vector3 tracerStart = serverStart;
            if (m_Animator != null)
            {
                WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
                WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
                string ejectBoneName = weapon?.Attack?.EjectBone;

                if (!string.IsNullOrEmpty(ejectBoneName))
                {
                    Transform ejectBone = FindDeepChild(m_Animator.transform, ejectBoneName);
                    if (ejectBone != null)
                    {
                        tracerStart = ejectBone.position;
                    }
                }
            }

            // Datengetriebener Tracer (SoF2 tracerEffect → TracerVisual mit TrailRenderer)
            if (!string.IsNullOrEmpty(tracerEffectId))
            {
                TracerVisual.Create(tracerStart, end, tracerEffectId);
            }

            // Impact-Effekt an der Einschlagstelle (Staub, Funken, Decal)
            if (!string.IsNullOrEmpty(impactEffectId) && hitNormal.sqrMagnitude > 0f)
            {
                EffectFactory effectFactory = ServiceLocator.Get<EffectFactory>();
                effectFactory?.SpawnImpactEffect(end, hitNormal, impactEffectId);

                // Surface-Debris an der Einschlagstelle (3D-Chunks mit Physik)
                if (!string.IsNullOrEmpty(debrisEffectId))
                {
                    Quaternion impactRotation = Quaternion.LookRotation(hitNormal);
                    effectFactory?.SpawnDebris(end, impactRotation, debrisEffectId);
                }

                // Impact-Sound an der Einschlagstelle abspielen (z.B. blunt/melee Munitionstypen)
                if (!string.IsNullOrEmpty(impactSoundPath))
                {
                    SoundManager soundManager = ServiceLocator.Get<SoundManager>();
                    if (soundManager != null)
                    {
                        AudioClip impactClip = soundManager.GetNumberedClip(impactSoundPath);
                        if (impactClip != null)
                        {
                            PlayImpactSoundAtPosition(impactClip, end, soundManager);
                        }
                    }
                }

                // Debug-HUD: Surface-Typ aus impactEffectId extrahieren (nur fuer lokalen Spieler)
                if (IsOwner)
                {
                    string surfaceDebug = impactEffectId;
                    const string impactPrefix = "effects/impact_";
                    if (surfaceDebug.StartsWith(impactPrefix))
                    {
                        surfaceDebug = surfaceDebug.Substring(impactPrefix.Length);
                    }
                    MatchView matchView = FindAnyObjectByType<MatchView>();
                    matchView?.UpdateLastSurfaceType(surfaceDebug);
                }
            }

            // Debug-Linien (optional, fuer Entwicklung — sichtbar in Game + Scene View)
            if (m_ShowDebugTracers)
            {
                Debug.DrawLine(tracerStart, end, Color.red, TRACER_DURATION);
                CreateDebugTracerLine(tracerStart, end);
            }
        }

        /// <summary>
        /// Spielt einen Impact-Sound als 3D-Sound an der angegebenen Position.
        /// Fuer Munitionstypen mit dediziertem Impact-Sound (z.B. "blunt": Pistolenschlag).
        /// Routet ueber die SFX MixerGroup des SoundManagers.
        /// </summary>
        private static void PlayImpactSoundAtPosition(AudioClip clip, Vector3 position, SoundManager soundManager)
        {
            GameObject soundObj = new("ImpactSoundFX");
            soundObj.transform.position = position;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 1f;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.minDistance = 3f;
            source.maxDistance = 40f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Destroy(soundObj, clip.length + 0.1f);
        }

        /// <summary>
        /// Erkennt den Oberflaechen-Typ unter einer gegebenen Position per Raycast nach unten.
        /// Gibt den SurfaceType-String zurueck oder "default" falls kein SurfaceTypeMarker gefunden.
        /// </summary>
        private static string DetectSurfaceAtPosition(Vector3 position)
        {
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 3f))
            {
                SurfaceTypeMarker marker = hit.collider.GetComponentInParent<SurfaceTypeMarker>();
                if (marker != null)
                {
                    return marker.SurfaceType;
                }
            }
            return "default";
        }

        /// <summary>
        /// Erstellt eine temporaere sichtbare Debug-Linie im Game View (LineRenderer).
        /// Wird nur bei aktiviertem m_ShowDebugTracers angezeigt.
        /// </summary>
        private static void CreateDebugTracerLine(Vector3 start, Vector3 end)
        {
            GameObject lineObj = new("DebugTracer");
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.useWorldSpace = true;
            Destroy(lineObj, TRACER_DURATION);
        }

        /// <summary>
        /// Server → Alle Clients: Muzzle-Flash, Muzzle-Smoke und Shell-Casing-Ejektion.
        /// Wird einmal pro Schuss gesendet (nicht pro Pellet).
        /// SoF2 CG_FireWeapon/CG_EjectBrass: Flash am Muzzle-Bone (flash_X), Huelse am Eject-Bone (ejection_X).
        /// Flash-Bone wird vom Eject-Bone abgeleitet: ejection_X → flash_X.
        /// Sonderfaelle: OICW (flashtop_oicw), Granaten (ejectBone=gun → flashBone=flash).
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void MuzzleEffectsClientRpc(string muzzleFlashId, string muzzleSmokeId,
            string shellCasingId, string ejectBoneName, string shellsoundPath, string fireSoundPath, float soundVolume)
        {
            if (m_Animator == null)
            {
                return;
            }

            Transform ejectBone = null;
            if (!string.IsNullOrEmpty(ejectBoneName))
            {
                ejectBone = FindDeepChild(m_Animator.transform, ejectBoneName);
            }

            // Fire-Sound abspielen (SoF2: CG_FireWeapon → flashSound auf CHAN_WEAPON)
            // Unabhaengig von ejectBone — altFire-Waffen (M4 M203, AK-74 Bayonet) haben oft keinen eigenen ejectBone.
            if (!string.IsNullOrEmpty(fireSoundPath))
            {
                SoundManager fireSoundManager = ServiceLocator.Get<SoundManager>();
                if (fireSoundManager != null)
                {
                    AudioClip fireClip = fireSoundManager.GetClip(fireSoundPath);
                    if (fireClip != null)
                    {
                        Vector3 firePos = ejectBone != null ? ejectBone.position : transform.position;
                        GameObject soundObj = new("WeaponFireFX");
                        soundObj.transform.position = firePos;
                        AudioSource source = soundObj.AddComponent<AudioSource>();
                        source.clip = fireClip;
                        source.spatialBlend = 1f;
                        source.playOnAwake = false;
                        source.maxDistance = 50f;
                        source.rolloffMode = AudioRolloffMode.Linear;

                        if (fireSoundManager.SfxGroup != null)
                        {
                            source.outputAudioMixerGroup = fireSoundManager.SfxGroup;
                        }

                        source.volume = Mathf.Clamp01(soundVolume);
                        source.Play();
                        Destroy(soundObj, fireClip.length + 0.1f);
                    }
                }
            }

            // Visuelle Effekte (Muzzle-Flash, Smoke, Shell) benoetigen einen ejectBone
            if (ejectBone == null)
            {
                return;
            }

            EffectFactory effectFactory = ServiceLocator.Get<EffectFactory>();
            if (effectFactory == null)
            {
                return;
            }

            // Muzzle-Flash und -Smoke am flash_-Bone spawnen (SoF2: Lauf-Ende)
            if (!string.IsNullOrEmpty(muzzleFlashId) || !string.IsNullOrEmpty(muzzleSmokeId))
            {
                string flashBoneName = DeriveFlashBoneName(ejectBoneName);
                Transform flashBone = FindDeepChild(m_Animator.transform, flashBoneName);

                // Fallback auf ejectBone wenn flash-Bone nicht existiert
                if (flashBone == null)
                {
                    flashBone = ejectBone;
                }

                Vector3 flashPos = flashBone.position;
                Quaternion flashRot = flashBone.rotation;

                if (!string.IsNullOrEmpty(muzzleFlashId))
                {
                    effectFactory.SpawnMuzzleEffect(flashPos, flashRot, muzzleFlashId);
                }

                if (!string.IsNullOrEmpty(muzzleSmokeId))
                {
                    effectFactory.SpawnMuzzleEffect(flashPos, flashRot, muzzleSmokeId);
                }
            }

            // Shell-Casing am ejection_-Bone spawnen (SoF2: Kammer)
            if (!string.IsNullOrEmpty(shellCasingId))
            {
                effectFactory.SpawnShellCasing(ejectBone.position, ejectBone.rotation, shellCasingId);
            }

            // Shellsound nahe Schuetze abspielen (Huelse trifft Boden)
            if (!string.IsNullOrEmpty(shellsoundPath))
            {
                SoundManager soundManager = ServiceLocator.Get<SoundManager>();
                if (soundManager != null)
                {
                    AudioClip shellClip = soundManager.GetNumberedClip(shellsoundPath);
                    if (shellClip != null)
                    {
                        Vector3 shellPos = ejectBone.position;
                        GameObject soundObj = new("ShellsoundFX");
                        soundObj.transform.position = shellPos;
                        AudioSource source = soundObj.AddComponent<AudioSource>();
                        source.clip = shellClip;
                        source.spatialBlend = 1f;
                        source.playOnAwake = false;
                        source.maxDistance = 20f;
                        source.rolloffMode = AudioRolloffMode.Linear;

                        if (soundManager.SfxGroup != null)
                        {
                            source.outputAudioMixerGroup = soundManager.SfxGroup;
                        }

                        source.Play();
                        Destroy(soundObj, shellClip.length + 0.1f);
                    }
                }
            }
        }

        /// <summary>
        /// Leitet den MuzzleFlash-Bone-Namen vom EjectBone-Namen ab.
        /// SoF2 Konvention: ejection_m4 → flash_m4, ejection_oicw → flashtop_oicw.
        /// Fallback fuer generische Bones (gun, etc.): "flash".
        /// </summary>
        private static string DeriveFlashBoneName(string ejectBoneName)
        {
            if (string.IsNullOrEmpty(ejectBoneName))
            {
                return "flash";
            }

            // OICW Sonderfall: ejection_oicw → flashtop_oicw
            if (ejectBoneName == "ejection_oicw")
            {
                return "flashtop_oicw";
            }

            // Standard: ejection_X → flash_X
            if (ejectBoneName.StartsWith("ejection_"))
            {
                return "flash_" + ejectBoneName.Substring("ejection_".Length);
            }

            // Generische Bones (gun, etc.) → flash
            return "flash";
        }

        /// <summary>
        /// Löst einen Sound-Pfad aus der WeaponDefinition.Sounds-Map auf.
        /// Behandelt sowohl einzelne Strings als auch JArray (mehrere Varianten → zufällige Auswahl).
        /// SoF2-Referenz: CG_RegisterWeapon → flashSound[0..2], random Selection in CG_FireWeapon.
        /// Fallback-Keys: "fire"→"swing" (Knife), "altFire"→"toss" (Knife-Throw).
        /// </summary>
        private static string ResolveWeaponSoundPath(WeaponDefinition weapon, string soundKey)
        {
            if (weapon?.Sounds == null)
            {
                return "";
            }

            // Primaerer Key
            if (weapon.Sounds.ContainsKey(soundKey))
            {
                return ExtractSoundValue(weapon.Sounds[soundKey]);
            }

            // Fallback-Keys fuer Waffen mit abweichender Benennung (z.B. Knife: swing statt fire)
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
                int index = UnityEngine.Random.Range(0, arr.Count);
                return arr[index].ToString();
            }

            return "";
        }

        /// <summary>
        /// Server → Alle Clients: Spielt den Ready-Sound einer Waffe nach abgeschlossenem Waffenwechsel.
        /// SoF2-Referenz: Weapon ready click/rack nach Raise-Animation.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void WeaponReadySoundClientRpc(string readySoundPath)
        {
            if (string.IsNullOrEmpty(readySoundPath))
            {
                return;
            }

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                return;
            }

            AudioClip readyClip = soundManager.GetClip(readySoundPath);
            if (readyClip == null)
            {
                return;
            }

            Vector3 soundPos = transform.position;
            GameObject soundObj = new("WeaponReadyFX");
            soundObj.transform.position = soundPos;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = readyClip;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Destroy(soundObj, readyClip.length + 0.1f);
        }

        /// <summary>
        /// Server → Alle Clients: Spielt den Empty/Dry-Fire-Sound wenn Waffe leer ist.
        /// SoF2-Referenz: Leeres Magazin-Klicken bei gehaltenem Feuer ohne Munition.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void WeaponEmptySoundClientRpc(string emptySoundPath)
        {
            if (string.IsNullOrEmpty(emptySoundPath))
            {
                return;
            }

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                return;
            }

            AudioClip emptyClip = soundManager.GetClip(emptySoundPath);
            if (emptyClip == null)
            {
                return;
            }

            Vector3 soundPos = transform.position;
            GameObject soundObj = new("WeaponEmptyFX");
            soundObj.transform.position = soundPos;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = emptyClip;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 20f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Destroy(soundObj, emptyClip.length + 0.1f);
        }

        /// <summary>
        /// Server → Alle Clients: Spielt einen Granaten-Sound (pinPull, pinRattle, handleRelease, throw/toss) ab.
        /// SoF2-Referenz: Grenade-Sequenz: pinPull → pinRattle → handleRelease → throw/toss.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void GrenadeSoundClientRpc(string soundPath)
        {
            if (string.IsNullOrEmpty(soundPath))
            {
                return;
            }

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                return;
            }

            AudioClip clip = soundManager.GetClip(soundPath);
            if (clip == null)
            {
                return;
            }

            Vector3 soundPos = transform.position;
            GameObject soundObj = new("GrenadeSoundFX");
            soundObj.transform.position = soundPos;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Destroy(soundObj, clip.length + 0.1f);
        }

        /// <summary>
        /// Server → Owner-Client: Wendet SoF2 kickAngles als View-Punch an.
        /// KickAngles-Format: [minPitch, maxPitch, minYaw, maxYaw].
        /// Pitch-Kick bewegt die Kamera nach oben (Rueckstoss), Yaw-Kick seitlich.
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void ApplyKickAnglesClientRpc(float pitchKick, float yawKick)
        {
            ClientPlayerCharacter client = GetComponent<ClientPlayerCharacter>();
            client?.ApplyKickAngles(pitchKick, yawKick);
        }

        /// <summary>
        /// Server: Verarbeitet einen AltAttack aus dem PlayerCommand.
        /// Wie SoF2 AltFire — konsumiert Alt-Ammo und startet AltAttack-Cooldown.
        /// Fuer Projektil-Waffen (F1 Grenade altAttack): Delegiert an ProcessProjectileAttack.
        /// </summary>
        private void ProcessAltAttack(PlayerCommand cmd)
        {
            // Alt-Munition verbrauchen (Server-autoritativ)
            if (!m_CharacterState.TryConsumeAltAmmo())
            {
                // Empty-Sound abspielen (Dry-Fire Click — SoF2: leeres Magazin beim AltFire)
                if (Time.time >= m_ServerNextEmptySoundTime)
                {
                    WeaponDataLoader emptyLoader = ServiceLocator.Get<WeaponDataLoader>();
                    WeaponDefinition emptyWeapon = emptyLoader?.GetById(m_CharacterState.CurrentWeaponName);
                    string emptySoundPath = ResolveWeaponSoundPath(emptyWeapon, "empty");
                    if (!string.IsNullOrEmpty(emptySoundPath))
                    {
                        WeaponEmptySoundClientRpc(emptySoundPath);
                        m_ServerNextEmptySoundTime = Time.time + 0.4f;
                    }
                }

                return;
            }

            // AltAttack-Parameter von aktueller Waffe laden
            UpdateServerAltAttackParameters();

            // Pruefen ob AltAttack ein Projektil hat (Knife-Throw, M4 M203)
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
            WeaponAttackDefinition altAttackDef = weapon?.AltAttack;

            if (altAttackDef?.Projectile != null)
            {
                // Fallback-Cooldown: Wenn kein mp_altAttack, mp_attack als Cooldown nutzen
                if (m_ServerAltAttackFrames <= 0 && weapon?.Animations != null
                    && weapon.Animations.TryGetValue("mp_attack", out WeaponAnimationEntry fallbackAnim))
                {
                    m_ServerAltAttackFrames = fallbackAnim.Duration;
                    m_ServerAltAttackFps = fallbackAnim.Fps;
                }

                ProcessProjectileAttack(cmd, altAttackDef, weapon, true);

                // Alt-Ammo auto-reload nach Projektil-Spawn (z.B. M4 M203)
                if (m_CharacterState.CanAltReload())
                {
                    m_CharacterState.CompleteAltReload();
                    Debug.Log($"[NetworkedPlayerCharacter] Server: Alt-ammo auto-reload after projectile for client {OwnerClientId}");
                }

                return;
            }

            if (m_ServerAltAttackFrames <= 0)
            {
                return;
            }

            // Melee/Hitscan-AltAttack: Cooldown starten + Raycast + Damage + KickAngles
            m_ServerAltAttackFramesRemaining = m_ServerAltAttackFrames;
            m_ServerAltAttackFrameAccumulator = 0f;

            // Muzzle-Effekte fuer Hitscan-AltAttack (falls definiert)
            if (altAttackDef != null)
            {
                string altShellsoundPath = "";
                string altAmmoTypeForShell = weapon?.AltAttack?.Ammo?.Type ?? weapon?.Ammo?.Type ?? "";
                if (!string.IsNullOrEmpty(altAmmoTypeForShell))
                {
                    string shooterSurface = DetectSurfaceAtPosition(transform.position);
                    SurfaceImpactDataLoader shellLoader = ServiceLocator.Get<SurfaceImpactDataLoader>();
                    if (shellLoader != null)
                    {
                        altShellsoundPath = shellLoader.GetShellsoundPath(shooterSurface, altAmmoTypeForShell);
                    }
                }

                string altFireSoundPath = ResolveWeaponSoundPath(weapon, "altFire");

                MuzzleEffectsClientRpc(
                    altAttackDef.MuzzleFlash ?? "",
                    altAttackDef.MuzzleSmoke ?? "",
                    altAttackDef.ShellCasingEject ?? "",
                    altAttackDef.EjectBone ?? "",
                    altShellsoundPath,
                    altFireSoundPath,
                    altAttackDef.Volume > 0f ? altAttackDef.Volume : 1f
                );
            }

            // Melee/Hitscan Raycast (einzelner Schuss, keine Pellets, keine Inaccuracy)
            if (altAttackDef != null && altAttackDef.Damage > 0 && altAttackDef.Range > 0)
            {
                Vector3 eyePos = m_ServerPlayerCharacter.GetEyePosition();
                Vector3 aimDirection = Quaternion.Euler(cmd.PitchAngle, cmd.YawAngle, 0f) * Vector3.forward;
                float rangeMeters = altAttackDef.Range * SOF2_UNIT_SCALE;

                m_ServerPlayerCharacter.SetPhysicsColliderEnabled(false);

                int hitboxLayerMask = LayerMask.GetMask(HITBOX_LAYER_NAME);
                bool didHit = Physics.Raycast(eyePos, aimDirection, out RaycastHit hit, rangeMeters, hitboxLayerMask);

                // Welt-Geometrie-Raycast fuer Impact-Effekte
                int worldLayerMask = ~hitboxLayerMask;
                bool didHitWorld = Physics.Raycast(eyePos, aimDirection, out RaycastHit worldHit, rangeMeters, worldLayerMask);

                float hitboxDist = didHit ? hit.distance : float.MaxValue;
                float worldDist = didHitWorld ? worldHit.distance : float.MaxValue;

                Vector3 hitPoint;
                Vector3 impactNormal = Vector3.zero;
                string impactEffectId = "";
                string debrisEffectId = "";
                string impactSoundPath = "";

                if (hitboxDist <= worldDist && didHit)
                {
                    hitPoint = hit.point;
                }
                else if (didHitWorld)
                {
                    hitPoint = worldHit.point;
                    impactNormal = worldHit.normal;

                    SurfaceTypeMarker surfaceMarker = worldHit.collider.GetComponentInParent<SurfaceTypeMarker>();
                    string surfaceType = surfaceMarker != null ? surfaceMarker.SurfaceType : "default";

                    // AltAttack Munitionstyp (z.B. Knife)
                    string altAmmoType = weapon?.AltAttack?.Ammo?.Type ?? weapon?.Ammo?.Type ?? "";
                    SurfaceImpactDataLoader surfaceLoader = ServiceLocator.Get<SurfaceImpactDataLoader>();
                    if (surfaceLoader != null)
                    {
                        impactEffectId = surfaceLoader.GetImpactEffectId(surfaceType, altAmmoType);
                        debrisEffectId = surfaceLoader.GetDebrisEffectId(surfaceType, altAmmoType);
                        impactSoundPath = surfaceLoader.GetImpactSoundPath(surfaceType, altAmmoType);
                    }
                }
                else
                {
                    hitPoint = eyePos + aimDirection * rangeMeters;
                }

                if (didHit && hitboxDist <= worldDist)
                {
                    HitboxCollider hitbox = hit.collider.GetComponent<HitboxCollider>();
                    if (hitbox != null)
                    {
                        int finalDamage = Mathf.RoundToInt(altAttackDef.Damage * hitbox.DamageMultiplier);

                        NetworkedCharacterState targetState = hit.collider.GetComponentInParent<NetworkedCharacterState>();
                        if (targetState != null && targetState != m_CharacterState)
                        {
                            int newHealth = Mathf.Max(0, targetState.Health - finalDamage);
                            targetState.SetHealth(newHealth);

                            Debug.Log($"[NetworkedPlayerCharacter] Server: AltAttack HIT! Client {OwnerClientId} → {targetState.CharacterName} | Region={hitbox.HitRegion} | Damage={finalDamage} (Base={altAttackDef.Damage} × {hitbox.DamageMultiplier:F2}) | Health={newHealth}");
                        }
                    }
                }

                m_ServerPlayerCharacter.SetPhysicsColliderEnabled(true);

                string altTracerEffectId = altAttackDef.TracerEffect ?? "";
                TracerClientRpc(eyePos, hitPoint, impactNormal, altTracerEffectId, impactEffectId, debrisEffectId, impactSoundPath);
            }

            // KickAngles: Rueckstoss an Owner-Client senden (falls definiert)
            // SoF2 Skalierung: ×0.5 (kickPitch += value*500, extract /1000)
            if (altAttackDef?.KickAngles != null && altAttackDef.KickAngles.Count >= 4)
            {
                float pitchKick = UnityEngine.Random.Range(altAttackDef.KickAngles[0], altAttackDef.KickAngles[1]) * 0.5f;
                float yawKick = UnityEngine.Random.Range(altAttackDef.KickAngles[2], altAttackDef.KickAngles[3]) * 0.5f;
                ApplyKickAnglesClientRpc(pitchKick, yawKick);
            }

            Debug.Log($"[NetworkedPlayerCharacter] Server: AltAttack aus Command #{cmd.SequenceNumber} für Client {OwnerClientId} — {m_ServerAltAttackFrames} Frames @ {m_ServerAltAttackFps}fps Cooldown");
        }

        /// <summary>
        /// Aktualisiert AltAttack-Frames und FPS basierend auf der aktuellen Waffe des Characters.
        /// Liest mp_altAttack aus dem WeaponDataLoader.
        /// </summary>
        private void UpdateServerAltAttackParameters()
        {
            if (m_CharacterState == null)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(m_CharacterState.CurrentWeaponName);
            if (weapon?.Animations == null)
            {
                return;
            }

            if (weapon.Animations.TryGetValue("mp_altAttack", out WeaponAnimationEntry altAttackAnim))
            {
                m_ServerAltAttackFrames = altAttackAnim.Duration;
                m_ServerAltAttackFps = altAttackAnim.Fps;
            }
            else
            {
                m_ServerAltAttackFrames = 0;
                m_ServerAltAttackFps = 20;
            }

            // SoF2 fireDelay fuer AltAttack (Millisekunden → Frames)
            WeaponAttackDefinition altAttackDef = weapon.AltAttack;
            if (altAttackDef != null && altAttackDef.FireDelay > 0 && m_ServerAltAttackFps > 0)
            {
                int altFireDelayFrames = Mathf.CeilToInt((altAttackDef.FireDelay / 1000f) * m_ServerAltAttackFps);
                m_ServerAltAttackFrames = Mathf.Max(m_ServerAltAttackFrames, altFireDelayFrames);
            }
        }

        /// <summary>
        /// Server: Verarbeitet einen Reload-Request aus dem PlayerCommand.
        /// Prueft via CharacterState ob Reload moeglich ist und startet den Cooldown.
        /// </summary>
        private void ProcessReload()
        {
            if (!m_CharacterState.CanReload())
            {
                return;
            }

            UpdateServerReloadParameters();

            if (m_ServerReloadFrames <= 0)
            {
                // Keine Reload-Animation definiert — sofort reloaden
                m_CharacterState.CompleteReload();
                return;
            }

            if (m_ServerIsShellReload)
            {
                WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
                WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
                if (weapon?.Ammo != null)
                {
                    int shellsNeeded = weapon.Ammo.MaxClip - m_CharacterState.CurrentClipAmmo;
                    m_ServerShellsRemaining = Mathf.Min(shellsNeeded, m_CharacterState.ReserveAmmo);
                    m_ServerShellReloadPhase = ShellReloadPhase.Start;
                    m_ServerShellPhaseFramesRemaining = m_ServerReloadStartFrames;
                    m_ServerReloadFramesRemaining = 1;
                }
                else
                {
                    m_ServerReloadFramesRemaining = m_ServerReloadFrames;
                    m_ServerShellReloadPhase = ShellReloadPhase.None;
                }
            }
            else
            {
                m_ServerReloadFramesRemaining = m_ServerReloadFrames;
                m_ServerShellReloadPhase = ShellReloadPhase.None;
            }

            m_ServerReloadFrameAccumulator = 0f;

            Debug.Log($"[NetworkedPlayerCharacter] Server: Reload started for client {OwnerClientId} — Phase={m_ServerShellReloadPhase}, ShellsRemaining={m_ServerShellsRemaining}");
        }

        /// <summary>
        /// Aktualisiert Reload-Frames und FPS basierend auf der aktuellen Waffe des Characters.
        /// Liest mp_reload aus dem WeaponDataLoader.
        /// </summary>
        private void UpdateServerReloadParameters()
        {
            if (m_CharacterState == null)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(m_CharacterState.CurrentWeaponName);
            if (weapon?.Animations == null)
            {
                return;
            }

            if (weapon.Animations.TryGetValue("mp_reload", out WeaponAnimationEntry reloadAnim))
            {
                m_ServerReloadFrames = reloadAnim.Duration;
                m_ServerReloadFps = reloadAnim.Fps;
                m_ServerIsShellReload = false;
            }
            else if (weapon.Animations.TryGetValue("mp_reloadStart", out WeaponAnimationEntry startAnim) &&
                     weapon.Animations.TryGetValue("mp_reloadShell", out WeaponAnimationEntry shellAnim) &&
                     weapon.Animations.TryGetValue("mp_reloadEnd", out WeaponAnimationEntry endAnim))
            {
                m_ServerReloadStartFrames = startAnim.Duration;
                m_ServerReloadShellFrames = shellAnim.Duration;
                m_ServerReloadEndFrames = endAnim.Duration;
                m_ServerReloadFps = startAnim.Fps;
                m_ServerIsShellReload = true;
                m_ServerReloadFrames = 1;
            }
            else
            {
                m_ServerReloadFrames = 0;
                m_ServerReloadFps = 20;
                m_ServerIsShellReload = false;
            }
        }

        /// <summary>
        /// Server: Tickt den Shell-by-Shell Reload phasenweise.
        /// Start → Shell (wiederholt, je 1 Shell transferieren) → End → fertig.
        /// </summary>
        private void TickServerShellReload(float deltaTime)
        {
            m_ServerReloadFrameAccumulator += deltaTime;
            float frameInterval = 1f / m_ServerReloadFps;

            while (m_ServerReloadFrameAccumulator >= frameInterval && m_ServerShellPhaseFramesRemaining > 0)
            {
                m_ServerReloadFrameAccumulator -= frameInterval;
                m_ServerShellPhaseFramesRemaining--;
            }

            if (m_ServerShellPhaseFramesRemaining > 0)
            {
                return;
            }

            switch (m_ServerShellReloadPhase)
            {
                case ShellReloadPhase.Start:
                    m_ServerShellReloadPhase = ShellReloadPhase.Shell;
                    m_ServerShellPhaseFramesRemaining = m_ServerReloadShellFrames;
                    break;

                case ShellReloadPhase.Shell:
                    m_CharacterState.TransferOneShell();
                    m_ServerShellsRemaining--;

                    if (m_ServerShellsRemaining > 0)
                    {
                        m_ServerShellPhaseFramesRemaining = m_ServerReloadShellFrames;
                    }
                    else
                    {
                        m_ServerShellReloadPhase = ShellReloadPhase.End;
                        m_ServerShellPhaseFramesRemaining = m_ServerReloadEndFrames;
                    }
                    break;

                case ShellReloadPhase.End:
                    m_ServerShellReloadPhase = ShellReloadPhase.None;
                    m_ServerReloadFramesRemaining = 0;
                    m_ServerReloadFrameAccumulator = 0f;
                    break;
            }
        }

        // ===== Weapon Swap (Drop/Raise) =====

        /// <summary>
        /// Server: Startet den Waffenwechsel-Prozess (Drop alte Waffe, dann Raise neue Waffe).
        /// Wird von NetworkedCharacterState via OnWeaponSwapRequested aufgerufen.
        /// Wie SoF2: Re-Switch waehrend laufendem Swap ist erlaubt (unterbricht und startet neuen Drop).
        /// </summary>
        private void OnServerWeaponSwapRequested(string targetWeapon)
        {
            if (string.IsNullOrEmpty(targetWeapon))
            {
                return;
            }

            if (targetWeapon == m_CharacterState.CurrentWeaponName && !m_ServerIsSwapping)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            // Raise-Daten der Zielwaffe lesen (immer aktualisieren bei neuem Target)
            int raiseFrames = 6;
            int raiseFps = 10;
            WeaponDefinition targetWeaponDef = loader.GetById(targetWeapon);
            if (targetWeaponDef?.Animations != null &&
                targetWeaponDef.Animations.TryGetValue("mp_raise", out WeaponAnimationEntry raiseAnim))
            {
                raiseFrames = raiseAnim.Duration;
                raiseFps = raiseAnim.Fps;
            }

            // Wenn bereits im Drop: nur Target + Raise-Daten aktualisieren, Drop-Timer laeuft weiter
            if (m_ServerIsSwapping && m_ServerSwapPhase == WeaponSwapPhase.Drop)
            {
                m_ServerSwapTargetWeapon = targetWeapon;
                m_ServerSwapRaiseFrames = raiseFrames;
                m_ServerSwapRaiseFps = raiseFps;
                Debug.Log($"[NetworkedPlayerCharacter] Server: Swap target updated during drop for client {OwnerClientId}: → {targetWeapon} (Raise {raiseFrames}f@{raiseFps}fps)");
                return;
            }

            // Drop-Daten der aktuellen Waffe (oder sichtbaren Waffe bei Re-Switch waehrend Raise)
            int dropFrames = 6;
            int dropFps = 10;
            string dropSourceWeapon = m_ServerIsSwapping && m_ServerSwapPhase == WeaponSwapPhase.Raise
                ? m_ServerSwapTargetWeapon
                : m_CharacterState.CurrentWeaponName;

            WeaponDefinition currentWeapon = loader.GetById(dropSourceWeapon);
            if (currentWeapon?.Animations != null &&
                currentWeapon.Animations.TryGetValue("mp_drop", out WeaponAnimationEntry dropAnim))
            {
                dropFrames = dropAnim.Duration;
                dropFps = dropAnim.Fps;
            }

            m_ServerIsSwapping = true;
            m_ServerSwapPhase = WeaponSwapPhase.Drop;
            m_ServerSwapFramesRemaining = dropFrames;
            m_ServerSwapFrameAccumulator = 0f;
            m_ServerSwapFps = dropFps;
            m_ServerSwapRaiseFrames = raiseFrames;
            m_ServerSwapRaiseFps = raiseFps;
            m_ServerSwapTargetWeapon = targetWeapon;

            Debug.Log($"[NetworkedPlayerCharacter] Server: Weapon swap started for client {OwnerClientId}: {dropSourceWeapon} → {targetWeapon} (Drop {dropFrames}f@{dropFps}fps, Raise {raiseFrames}f@{raiseFps}fps)");
        }

        /// <summary>
        /// Server: Zaehlt Swap-Frames herunter und wechselt die Phase (Drop → Raise → Done).
        /// Bei Drop-Ende wird die Waffe autoritativ gewechselt (setzt NetworkVariable).
        /// </summary>
        private void TickServerWeaponSwap(float deltaTime)
        {
            m_ServerSwapFrameAccumulator += deltaTime;
            float frameInterval = 1f / m_ServerSwapFps;
            while (m_ServerSwapFrameAccumulator >= frameInterval && m_ServerSwapFramesRemaining > 0)
            {
                m_ServerSwapFrameAccumulator -= frameInterval;
                m_ServerSwapFramesRemaining--;
            }

            if (m_ServerSwapFramesRemaining <= 0)
            {
                if (m_ServerSwapPhase == WeaponSwapPhase.Drop)
                {
                    // Drop fertig: Waffe autoritativ wechseln (setzt NetworkVariable → OnWeaponChanged)
                    m_CharacterState.SetCurrentWeaponName(m_ServerSwapTargetWeapon);
                    UpdateServerAttackParameters();
                    UpdateServerReloadParameters();

                    // Raise-Phase starten
                    m_ServerSwapPhase = WeaponSwapPhase.Raise;
                    m_ServerSwapFramesRemaining = m_ServerSwapRaiseFrames;
                    m_ServerSwapFrameAccumulator = 0f;
                    m_ServerSwapFps = m_ServerSwapRaiseFps;
                }
                else if (m_ServerSwapPhase == WeaponSwapPhase.Raise)
                {
                    // Raise fertig: Swap abgeschlossen → Ready-Sound abspielen
                    m_ServerIsSwapping = false;
                    m_ServerSwapPhase = WeaponSwapPhase.None;

                    // Ready-Sound aus Waffen-Definition (SoF2: weapon ready click/rack)
                    WeaponDataLoader readyLoader = ServiceLocator.Get<WeaponDataLoader>();
                    WeaponDefinition readyWeapon = readyLoader?.GetById(m_ServerSwapTargetWeapon ?? m_CharacterState.CurrentWeaponName);
                    string readySoundPath = ResolveWeaponSoundPath(readyWeapon, "ready");
                    if (!string.IsNullOrEmpty(readySoundPath))
                    {
                        WeaponReadySoundClientRpc(readySoundPath);
                    }

                    m_ServerSwapTargetWeapon = null;

                    Debug.Log($"[NetworkedPlayerCharacter] Server: Weapon swap completed for client {OwnerClientId}");
                }
            }
        }

        // ===== Animation Sync =====

        /// <summary>
        /// Abonniert das OnVisualInstantiated-Event der SkinHandler-Komponente.
        /// Wird für Owner und Remote Clients aufgerufen, damit alle den Animator finden.
        /// </summary>
        private void SubscribeToVisualInstantiated()
        {
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated += OnVisualInstantiated;
            }
        }

        /// <summary>
        /// Deregistriert das OnVisualInstantiated-Event.
        /// </summary>
        private void UnsubscribeFromVisualInstantiated()
        {
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }
        }

        /// <summary>
        /// Callback wenn das Visual-Prefab instanziiert wurde.
        /// Sucht den Animator auf dem instanziierten Visual.
        /// </summary>
        private void OnVisualInstantiated(GameObject visualInstance)
        {
            m_Animator = visualInstance.GetComponentInChildren<Animator>();

            if (m_Animator == null)
            {
                Debug.LogWarning($"[NetworkedPlayerCharacter] Animator nicht auf Visual gefunden! Character {CharacterId}");
            }
            else
            {
                // Root Motion deaktivieren: Vertikale Positionierung kommt
                // ausschliesslich aus der SoF2-Physik-Simulation (PlayerPhysicsSimulation).
                // Ohne dies wuerde die Jump-Animation die Visual-Position ueber die
                // Physik-Capsule hinaus nach oben verschieben.
                m_Animator.applyRootMotion = false;
                Debug.Log($"[NetworkedPlayerCharacter] Animator gefunden auf Visual für Character {CharacterId} (Root Motion deaktiviert)");
            }
        }

        /// <summary>
        /// Schreibt den Animation-State in die NetworkVariable (nur Owner).
        /// Wird von <see cref="ClientPlayerCharacter"/> aufgerufen.
        /// Wendet den State sofort lokal auf den Animator an (zero-latency für Owner).
        /// </summary>
        public void WriteAnimationState(NetworkAnimationState state)
        {
            m_AnimationState.Value = state;
            ApplyAnimationToAnimator(state);
        }

        /// <summary>
        /// Wendet die Animation-Parameter auf den lokalen Animator an.
        /// Wird für Owner sofort nach Schreiben aufgerufen,
        /// für Remotes in Update() aus der NetworkVariable gelesen.
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

            // Attack-Animation Re-Trigger: wenn IsAttacking true und AttackSequence sich
            // geaendert hat, Attack-State auf dem Torso-Layer von Frame 0 neu starten.
            // Noetig weil die Attack-Animationen loop=false haben und der Animator sie
            // bei unveraendertem Bool nicht erneut abspielt.
            if (state.IsAttacking && state.AttackSequence != m_LastAttackSequence)
            {
                AnimatorStateInfo torsoState = m_Animator.GetCurrentAnimatorStateInfo(TORSO_LAYER_INDEX);
                m_Animator.Play(torsoState.fullPathHash, TORSO_LAYER_INDEX, 0f);
            }

            m_LastAttackSequence = state.AttackSequence;
        }

        /// <summary>
        /// Feuert den Jump-Trigger auf dem Animator (Owner lokal + RPC an Remotes).
        /// Wird von <see cref="ClientPlayerCharacter"/> aufgerufen wenn der Spieler springt.
        /// </summary>
        public void RequestJumpTrigger()
        {
            // Owner: sofort lokal auslösen
            if (m_Animator != null)
            {
                m_Animator.SetTrigger(s_JumpHash);
            }

            // An Server senden → Server broadcastet an Remotes
            SendJumpTriggerServerRpc();
        }

        /// <summary>
        /// Server empfängt Jump-Trigger vom Owner und broadcastet an alle anderen Clients.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SendJumpTriggerServerRpc()
        {
            BroadcastJumpTriggerClientRpc();
        }

        /// <summary>
        /// Alle Clients (außer Server): Jump-Trigger auf dem Animator setzen.
        /// Owner ignoriert (hat bereits lokal getriggert).
        /// </summary>
        [Rpc(SendTo.NotServer)]
        private void BroadcastJumpTriggerClientRpc()
        {
            // Owner hat bereits lokal getriggert
            if (IsOwner)
            {
                return;
            }

            if (m_Animator != null)
            {
                m_Animator.SetTrigger(s_JumpHash);
            }
        }

        /// <summary>
        /// Erzwingt den Animator-State fuer eine Swap-Animation (Drop oder Raise)
        /// auf dem Torso-Layer ab Frame 0.
        /// Berechnet die Animator-Speed aus JSON-Daten (duration/fps) relativ zur
        /// Clip-Dauer (SWAP_CLIP_DURATION), damit die Animation exakt so lange
        /// laeuft wie in SoF2 vorgesehen.
        /// </summary>
        public void ForcePlaySwapState(int stateHash, int duration, int fps)
        {
            if (m_Animator != null)
            {
                float desiredDuration = (float)duration / fps;
                float speed = SWAP_CLIP_DURATION / desiredDuration;
                m_Animator.SetFloat(s_SwapSpeedHash, speed);
                m_Animator.Play(stateHash, TORSO_LAYER_INDEX, 0f);
            }
        }

        /// <summary>
        /// Liefert den Animator-State-Hash fuer die Drop-Animation basierend auf
        /// dem mp_drop.name Wert aus der Waffen-JSON (z.B. "TORSO_DROP_KNIFE").
        /// </summary>
        public static int GetDropStateHash(string dropAnimName)
        {
            return dropAnimName switch
            {
                "TORSO_DROP_KNIFE" => s_KnifeDropHash,
                "TORSO_DROP_ONEHANDED" => s_DropOneHandedHash,
                _ => s_DropTwoHandedHash,
            };
        }

        /// <summary>
        /// Liefert den Animator-State-Hash fuer die Raise-Animation basierend auf
        /// dem mp_raise.name Wert aus der Waffen-JSON (z.B. "TORSO_RAISE_KNIFE").
        /// </summary>
        public static int GetRaiseStateHash(string raiseAnimName)
        {
            return raiseAnimName switch
            {
                "TORSO_RAISE_KNIFE" => s_KnifeReadyHash,
                "TORSO_RAISE_ONEHANDED" => s_ReadyOneHandedHash,
                _ => s_ReadyTwoHandedHash,
            };
        }
        // ===== Utility =====

        /// <summary>
        /// Rekursive Tiefensuche nach einem Child-Transform mit gegebenem Namen.
        /// </summary>
        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent.name == childName)
            {
                return parent;
            }

            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform result = FindDeepChild(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
