using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.GoreManagement;
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
        // ===== Cached Shaders =====
        private static Shader s_CachedSpritesShader;

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

        /// <summary>
        /// Event: Hit-Confirmation vom Server empfangen.
        /// Wird nur auf dem Owner-Client gefeuert.
        /// Parameter: HitRegion, Damage, IsKill.
        /// </summary>
        public event Action<HitRegion, int, bool> OnHitConfirmed;

        // ===== Animation Sync =====

        /// <summary>
        /// SkinHandler-Referenz für OnVisualInstantiated-Event.
        /// Wird benötigt um nach Visual-Instanziierung den Animator zu finden.
        /// </summary>
        [SerializeField]
        private ClientCharacterSkinHandler m_SkinHandler;

        /// <summary>
        /// Eigene Hitbox-Referenz fuer Self-Hit-Vermeidung.
        /// Server deaktiviert eigene Hitboxen vor Raycasts und aktiviert sie danach wieder.
        /// </summary>
        private ClientHitboxSystem m_OwnHitboxSystem;

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
        /// <summary>Layer-Name fuer Broad-Phase Raycasts: nutzt den Movement-BoxCollider auf Player-Layer.</summary>
        private const string PLAYER_LAYER_NAME = "Player";

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

        // ===== Server-Side Reload Sound Tracking =====

        /// <summary>Reload-Sound-Events fuer die aktuelle Waffe (aus WeaponDefinition.ReloadSounds).</summary>
        private ReloadSoundDefinition m_ServerReloadSounds;

        /// <summary>Bitmask: welche Standard-Reload-Sound-Events bereits gefeuert wurden (max 32).</summary>
        private int m_ServerReloadSoundsFired;

        /// <summary>Bitmask: welche Phase-Sound-Events bereits gefeuert wurden (max 32, pro Phase reset).</summary>
        private int m_ServerShellPhaseSoundsFired;

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

        // ===== SoF2 pm_debounce (Server-Authoritative Button Debounce) =====
        // Direkt aus bg_public.h: PMD_ATTACK, PMD_ALTATTACK.
        // PM_GetAttackButtons() in bg_pmove.c setzt das Flag beim Feuern.
        // Solange das Flag gesetzt ist UND der Button gehalten wird UND der FireMode != auto,
        // wird der Button serverseitig maskiert (kein erneuter Schuss).
        // Cleared sobald der Button losgelassen wird.

        /// <summary>SoF2 PMD_ATTACK: Gesetzt wenn Primary Attack gefeuert wurde. Cleared bei Button-Release.</summary>
        private const int PMD_ATTACK = 0x0002;

        /// <summary>SoF2 PMD_FIREMODE: Gesetzt wenn FireMode-Button gedrueckt. Cleared bei Release.</summary>
        private const int PMD_FIREMODE = 0x0004;

        /// <summary>SoF2 PMD_ALTATTACK: Gesetzt wenn AltAttack gefeuert wurde. Cleared bei Button-Release.</summary>
        private const int PMD_ALTATTACK = 0x0010;

        /// <summary>
        /// SoF2 pm_debounce Bitfield (server-autoritativ).
        /// Verhindert Auto-Re-Fire bei gehaltener Taste fuer Single/Burst/Grenade-Waffen.
        /// Exakt wie bg_pmove.c PM_GetAttackButtons() — Flag wird beim Feuern gesetzt,
        /// bei Button-Release cleared, bei gehaltener Taste + non-auto Modus wird Button maskiert.
        /// </summary>
        private int m_ServerDebounce;

        /// <summary>
        /// Server-seitig gecachter Feuermodus der aktuellen Waffe ("auto", "single", "burst").
        /// Wird bei Waffenwechsel aus WeaponDataLoader aktualisiert.
        /// SoF2: ps->firemode[ps->weapon] — Server kennt den Modus autoritativ.
        /// </summary>
        private string m_ServerFireMode = "auto";

        /// <summary>
        /// Server-seitig gecachter Alt-Feuermodus der aktuellen Waffe.
        /// SoF2: altAttack kann eigenen fireMode haben.
        /// </summary>
        private string m_ServerAltFireMode = "auto";

        /// <summary>
        /// Verbleibende Burst-Schuesse auf dem Server (nur im "burst"-Modus).
        /// SoF2: weaponFireBurstCount in bg_pmove.c.
        /// </summary>
        private int m_ServerBurstShotsRemaining;

        // ===== Movement Sync =====



        /// <summary>
        /// Wird auf dem Server aufgerufen: Spawn-Point zuweisen und Server-Position setzen.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            // Server-seitige Physik + BoxCollider initialisieren
            m_ServerPlayerCharacter.InitializeServer();

            // Hitbox-System cachen fuer Self-Hit-Vermeidung bei Raycasts
            m_OwnHitboxSystem = GetComponent<ClientHitboxSystem>();

            // Animator-Referenz nach Visual-Instanziierung setzen (Server braucht Animator fuer Hitbox-Bone-Tracking)
            SubscribeToVisualInstantiated();

            // Weapon-Swap-Event abonnieren (Server verarbeitet Swap-Timing)
            m_CharacterState.OnWeaponSwapRequested += OnServerWeaponSwapRequested;

            // Spawn-Point vom Server zuweisen — ggf. warten bis Map geladen ist
            if (ServerPlayerSpawnPoints.Instance == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[NetworkedPlayerCharacter] ServerPlayerSpawnPoints noch nicht verfügbar — warte auf Map-Laden.");
#endif
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
            TeamId spawnTeam = GetSpawnTeamId();
            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint(spawnTeam);
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            m_ServerPlayerCharacter.ResetForRespawn();
            m_ServerPlayerCharacter.SetReady();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: Spieler gespawnt bei {position} (Team={spawnTeam})");
#endif
        }

        /// <summary>
        /// Server-seitiger Respawn auf den naechsten Spawn-Point.
        /// Wird z. B. nach einem Map-Wechsel genutzt, um Spieler sicher auf die neue Map zu setzen.
        /// </summary>
        public void RespawnAtNextSpawnPoint()
        {
            if (!IsServer)
            {
                return;
            }

            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.LogWarning("[NetworkedPlayerCharacter] RespawnAtNextSpawnPoint: Keine ServerPlayerSpawnPoints vorhanden.");
                return;
            }

            // Spieler wiederbeleben falls tot (z.B. nach HideAndSeek-Runde ohne Respawn)
            if (m_CharacterState != null && !m_CharacterState.IsAlive)
            {
                m_CharacterState.SetHealth(100);
                m_CharacterState.SetIsAlive(true);
            }

            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint(GetSpawnTeamId());
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            // Owner sofort hart korrigieren, damit keine alte Prediction-Position sichtbar bleibt.
            CorrectionClientRpc(position, rotation);

            m_ServerPlayerCharacter.ResetForRespawn();
            m_ServerPlayerCharacter.SetReady();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: Spieler respawned bei {position}");
#endif
        }

        /// <summary>
        /// Ermittelt die TeamId fuer Spawn-Point-Auswahl basierend auf dem GametypeTeam des Spielers.
        /// GametypeTeam.Red (Hider) → TeamId.TeamOne, GametypeTeam.Blue (Seeker) → TeamId.TeamTwo.
        /// Fallback: TeamId.TeamOne.
        /// </summary>
        private TeamId GetSpawnTeamId()
        {
            if (m_CharacterState == null)
            {
                return TeamId.TeamOne;
            }

            uint teamId = m_CharacterState.TeamId;
            // GametypeTeam.Red=1 → TeamOne (Hider), GametypeTeam.Blue=2 → TeamTwo (Seeker)
            return teamId == 2 ? TeamId.TeamTwo : TeamId.TeamOne;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[NetworkedPlayerCharacter] Owner: Client-Side Prediction aktiv");
#endif
        }

        /// <summary>
        /// Remote-Client: Nur Interpolation, kein Input.
        /// </summary>
        protected override void OnRemoteSpawn()
        {
            base.OnRemoteSpawn();

            // Animator-Referenz nach Visual-Instanziierung setzen
            SubscribeToVisualInstantiated();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Remote: Client {OwnerClientId} - Interpolation aktiv");
#endif
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

            // Remote-Clients + Dedicated Server: Interpolation zur Server-Position + Animator treiben
            // Server treibt Animator fuer akkurate Bone-Positionen (Hitbox-Tracking wie SoF2 GHOUL2)
            if (!IsOwner)
            {
                if (!IsServer)
                {
                    InterpolateRemotePosition();
                }
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
                // Host-Mode: Physik läuft schon lokal, Server-Position direkt aktualisieren.
                // Button-Inputs (Attack, Reload, Swap, Grenade) muessen trotzdem serverseitig verarbeitet werden.
                m_ServerPosition.Value = transform.position;
                m_ServerRotation.Value = transform.rotation;

                ProcessServerCommandLogic(cmd);
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
            m_ServerRotation.Value = Quaternion.Euler(0f, cmd.MoveYawAngle, 0f);

            // Button-Inputs, Frame-Counting, Attack/Reload/Swap verarbeiten
            ProcessServerCommandLogic(cmd);

            // Acknowledgement an Owner-Client senden (für Reconciliation)
            MovementAckClientRpc(ack);
        }

        /// <summary>
        /// Gemeinsame Server-Logik fuer Button-Inputs, Frame-Counting, Attack/Reload/Swap.
        /// Wird sowohl von SubmitCommandServerRpc (Remote-Clients) als auch von
        /// SendPlayerCommand (Host-Mode) aufgerufen.
        /// </summary>
        private void ProcessServerCommandLogic(PlayerCommand cmd)
        {
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

                    // Reload-Sound-Events pruefen (Standard-Reload)
                    TickServerReloadSounds();

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

            // ===== SoF2 PM_GetAttackButtons (bg_pmove.c:2482-2580) =====
            // Server-autoritative Button-Debounce: maskiert Buttons bei gehaltener Taste
            // fuer Single/Burst/Grenade-Waffen. Exakt wie SoF2 pm_debounce.
            int attackButtons = cmd.Buttons;

            // SoF2 BUTTON_FIREMODE Debounce (bg_pmove.c:2489-2500): Feuermodus-Wechsel
            if (cmd.HasButton(CommandButtons.FireMode))
            {
                if ((m_ServerDebounce & PMD_FIREMODE) == 0)
                {
                    m_ServerDebounce |= PMD_FIREMODE;
                    CycleServerFireMode();
                }
            }
            else
            {
                m_ServerDebounce &= ~PMD_FIREMODE;
            }

            // PMD_ATTACK Debounce (SoF2 bg_pmove.c:2504-2512)
            if ((m_ServerDebounce & PMD_ATTACK) != 0)
            {
                if (!cmd.HasButton(CommandButtons.Attack))
                {
                    // Button losgelassen → Debounce aufheben
                    m_ServerDebounce &= ~PMD_ATTACK;
                }
                else if (m_ServerFireMode != "auto")
                {
                    // Button gehalten + nicht Auto → Attack maskieren (kein Re-Fire)
                    attackButtons &= ~CommandButtons.Attack;
                }
            }

            // SoF2 Burst-Fire (bg_pmove.c:2530-2545): Attack gedrueckt + kein laufender Burst → Burst starten
            if (m_ServerFireMode == "burst" && (attackButtons & CommandButtons.Attack) != 0
                && m_ServerBurstShotsRemaining <= 0)
            {
                m_ServerBurstShotsRemaining = 3;
            }

            // PMD_ALTATTACK Debounce (SoF2 bg_pmove.c:2558-2569)
            if ((m_ServerDebounce & PMD_ALTATTACK) != 0)
            {
                if (!cmd.HasButton(CommandButtons.AltAttack))
                {
                    m_ServerDebounce &= ~PMD_ALTATTACK;
                }
                else if (m_ServerAltFireMode != "auto")
                {
                    attackButtons &= ~CommandButtons.AltAttack;
                }
            }

            // Burst-Fire: Verbleibende Burst-Schuesse automatisch abfeuern (SoF2 bg_pmove.c:2539-2545)
            if (m_ServerBurstShotsRemaining > 0)
            {
                attackButtons |= CommandButtons.Attack;
                attackButtons &= ~CommandButtons.AltAttack;
                attackButtons &= ~CommandButtons.Reload;
                attackButtons &= ~CommandButtons.Zoom;
                attackButtons &= ~CommandButtons.FireMode;
            }

            // Button-Inputs verarbeiten (SoF2: FireWeapon aus usercmd_t.buttons)
            // Server gated: Attack nur starten wenn keine Attacke, kein Reload, kein AltAttack, kein Swap und keine Granate laeuft (Anti-Cheat)
            bool noActionRunning = m_ServerAttackFramesRemaining <= 0
                && m_ServerReloadFramesRemaining <= 0
                && m_ServerAltAttackFramesRemaining <= 0
                && !m_ServerIsSwapping
                && !m_ServerIsGrenadeCooking
                && m_ServerGrenadeThrowFramesRemaining <= 0;

            // SoF2 bg_pmove.c:2930-2932: Burst-Counter dekrementieren wenn Waffe bereit
            // (vor dem Fire-Code, nicht pro Schuss — damit Burst bei leerem Magazin auslaueft)
            if (noActionRunning && m_ServerBurstShotsRemaining > 0)
            {
                m_ServerBurstShotsRemaining--;
            }

            if ((attackButtons & CommandButtons.Attack) != 0 && noActionRunning)
            {
                ProcessAttack(cmd);
            }

            // Server gated: AltAttack nur starten wenn keine Action laeuft
            if ((attackButtons & CommandButtons.AltAttack) != 0 && noActionRunning)
            {
                ProcessAltAttack(cmd);
            }

            // Server gated: Reload nur starten wenn keine Action laeuft und Reload moeglich
            if (cmd.HasButton(CommandButtons.Reload) && noActionRunning)
            {
                ProcessReload();
            }
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
        /// Server → Owner-Client: Hit-Confirmation mit getroffener Region, Damage und Kill-Flag.
        /// Wird nur an den Schuetzen gesendet, damit dieser Hitmarker/HUD-Feedback anzeigen kann.
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void HitConfirmRpc(int hitRegionValue, int damage, bool isKill)
        {
            OnHitConfirmed?.Invoke((HitRegion)hitRegionValue, damage, isKill);
        }

        /// <summary>
        /// Server → Owner-Client: Hard-Correction (Respawn, Teleport, Anti-Cheat).
        /// Überschreibt Client-Position ohne Reconciliation.
        /// Resettet auch die Client-Simulation (Velocity, Grounded-State), damit keine
        /// alte Fall-Velocity den Spieler nach dem Teleport wegschleudert.
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void CorrectionClientRpc(Vector3 correctPosition, Quaternion correctRotation)
        {
            // Owner-Client: Server hat die Position korrigiert → Prediction überschreiben
            transform.SetPositionAndRotation(correctPosition, correctRotation);

            // Client-Simulation resetten: alte Velocity/State verwerfen
            ClientPlayerCharacter client = GetComponent<ClientPlayerCharacter>();
            client?.ResetSimulationForRespawn();

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

            // SoF2 bg_pmove.c:3255 — pm_debounce setzen bei erfolgreichem Schuss
            m_ServerDebounce |= PMD_ATTACK;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Hitscan] eyePos={eyePos}, playerPos={transform.position}, eyeHeight={eyePos.y - transform.position.y:F3}, pitch={cmd.PitchAngle:F2}, yaw={cmd.YawAngle:F2}");
#endif

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

            // Eigenen Collider + Hitboxen deaktivieren fuer Self-Hit-Vermeidung
            m_ServerPlayerCharacter.SetPhysicsColliderEnabled(false);
            m_OwnHitboxSystem?.SetHitboxesEnabled(false);

            // Hitbox-Collider folgen animierten Bones per Transform-Parenting.
            // Physics.autoSyncTransforms ist in Unity 2021+ standardmaessig false,
            // d.h. Collider-Positionen werden nur in FixedUpdate synchronisiert.
            // Da dieser Code im Update-Frame (via RPC/Command) laeuft, muessen
            // wir die Physik-Transforms manuell synchronisieren, damit Raycasts
            // die aktuellen Bone-Positionen treffen.
            Physics.SyncTransforms();

            int hitboxLayerMask = LayerMask.GetMask("Hitbox");
            int playerLayerMask = LayerMask.GetMask(PLAYER_LAYER_NAME);

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
                attackDef.MuzzleFlashInworld ?? "",
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

                // Zwei separate Raycasts: Hitbox-Trigger und Welt-Geometrie.
                // Ein kombinierter Raycast wuerde bei naeherem Welt-Collider die Bone-Trigger verschlucken.
                bool didHitBone = Physics.Raycast(eyePos, aimDirection, out RaycastHit boneHit, rangeMeters, hitboxLayerMask, QueryTriggerInteraction.Collide);
                int worldLayerMask = ~(hitboxLayerMask | playerLayerMask | LayerMask.GetMask("BrushCollision"));
                bool didHitWorld = Physics.Raycast(eyePos, aimDirection, out RaycastHit worldHit, rangeMeters, worldLayerMask);

                // Naeherer Treffer gewinnt
                float boneDist = didHitBone ? boneHit.distance : float.MaxValue;
                float worldDist = didHitWorld ? worldHit.distance : float.MaxValue;

                Vector3 hitPoint;
                Vector3 impactNormal = Vector3.zero;
                string impactEffectId = "";
                string debrisEffectId = "";
                string impactSoundPath = "";

                bool boneResolved = false;
                HitRegion resolvedRegion = default;
                float resolvedMultiplier = 1.0f;

                if (didHitBone && boneDist <= worldDist)
                {
                    HitboxCollider hitboxCollider = boneHit.collider.GetComponent<HitboxCollider>();
                    if (hitboxCollider != null)
                    {
                        boneResolved = true;
                        resolvedRegion = hitboxCollider.HitRegion;
                        resolvedMultiplier = hitboxCollider.DamageMultiplier;
                        hitPoint = boneHit.point;
                        impactNormal = -aimDirection.normalized;

                        SurfaceImpactDataLoader surfaceLoader = ServiceLocator.Get<SurfaceImpactDataLoader>();
                        if (surfaceLoader != null)
                        {
                            impactEffectId = surfaceLoader.GetImpactEffectId("flesh", ammoType);
                            debrisEffectId = surfaceLoader.GetDebrisEffectId("flesh", ammoType);
                            impactSoundPath = surfaceLoader.GetImpactSoundPath("flesh", ammoType);
                        }
                    }
                    else
                    {
                        hitPoint = boneHit.point;
                    }
                }
                else if (didHitWorld)
                {
                    hitPoint = worldHit.point;
                    impactNormal = worldHit.normal;

                    SurfaceTypeMarker surfaceMarker = worldHit.collider.GetComponentInParent<SurfaceTypeMarker>();
                    string surfaceType = surfaceMarker != null ? surfaceMarker.SurfaceType : "default";

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

                if (boneResolved)
                {
                    int finalDamage = Mathf.RoundToInt(attackDef.Damage * resolvedMultiplier);

                    NetworkedCharacterState targetState = boneHit.collider.GetComponentInParent<NetworkedCharacterState>();
                    if (targetState != null && targetState != m_CharacterState)
                    {
                        int previousHealth = targetState.Health;
                        int newHealth = Mathf.Max(0, previousHealth - finalDamage);
                        targetState.SetHealth(newHealth);

                        bool isKill = newHealth <= 0 && previousHealth > 0;
                        HitConfirmRpc((int)resolvedRegion, finalDamage, isKill);

                        if (attackDef.Gore)
                        {
                            TryProcessGoreHit(targetState.gameObject, resolvedRegion, aimDirection, boneHit.point, finalDamage, previousHealth, newHealth, m_CharacterState.CurrentWeaponName, false);
                        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[NetworkedPlayerCharacter] Server: HIT! Client {OwnerClientId} → {targetState.CharacterName} | Pellet={i + 1}/{pelletCount} | Region={resolvedRegion} | Damage={finalDamage} (Base={attackDef.Damage} × {resolvedMultiplier:F2}) | Health={newHealth}");
#endif
                    }
                }

                // Tracer + Impact pro Pellet an alle Clients senden
                string tracerEffectId = attackDef.TracerEffect ?? "";
                TracerClientRpc(eyePos, hitPoint, impactNormal, tracerEffectId, impactEffectId, debrisEffectId, impactSoundPath);
            }

            // Eigenen Collider + Hitboxen wieder aktivieren
            m_ServerPlayerCharacter.SetPhysicsColliderEnabled(true);
            m_OwnHitboxSystem?.SetHitboxesEnabled(true);

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
        /// Server: Wechselt den Primary FireMode zyklisch durch die verfuegbaren Modi.
        /// SoF2 BG_FindFireMode (bg_weapons.c): Zykliert durch ps->firemode[weapon] Liste.
        /// Wird bei PMD_FIREMODE Debounce-Trigger aufgerufen.
        /// </summary>
        private void CycleServerFireMode()
        {
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(m_CharacterState?.CurrentWeaponName ?? "");
            System.Collections.Generic.List<string> modes = weapon?.Attack?.FireModes;
            if (modes == null || modes.Count <= 1)
            {
                return;
            }

            int currentIndex = modes.IndexOf(m_ServerFireMode);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            m_ServerFireMode = modes[(currentIndex + 1) % modes.Count];
            m_ServerBurstShotsRemaining = 0;
        }

        /// <summary>
        /// Server: Aktualisiert FireMode, AltFireMode und setzt Debounce/Burst zurueck.
        /// Wird nur bei Waffenwechsel aufgerufen (nicht bei jedem Schuss).
        /// SoF2: ps->firemode[ps->weapon] wird bei Waffenwechsel initialisiert.
        /// </summary>
        private void UpdateServerFireModeParameters()
        {
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(m_CharacterState?.CurrentWeaponName ?? "");
            if (weapon == null)
            {
                m_ServerFireMode = "auto";
                m_ServerAltFireMode = "auto";
                m_ServerDebounce = 0;
                m_ServerBurstShotsRemaining = 0;
                return;
            }

            // SoF2 ps->firemode[ps->weapon]: Server-seitig den FireMode cachen
            // Projektilwaffen (Granaten, RPG) sind immer "single" wenn kein FireMode definiert
            string defaultFireMode = weapon.Attack?.FireMode ?? "auto";
            if (weapon.Attack?.FireMode == null && weapon.Attack?.Projectile != null)
            {
                defaultFireMode = "single";
            }
            m_ServerFireMode = defaultFireMode;

            // AltAttack FireMode (SoF2: altAttack kann eigenen fireMode haben)
            m_ServerAltFireMode = weapon.AltAttack?.FireMode ?? "auto";
            if (weapon.AltAttack?.FireMode == null && weapon.AltAttack?.Projectile != null)
            {
                m_ServerAltFireMode = "single";
            }

            // Debounce + Burst bei Waffenwechsel zuruecksetzen
            m_ServerDebounce = 0;
            m_ServerBurstShotsRemaining = 0;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[NetworkedPlayerCharacter] Server: Grenade cook started for client {OwnerClientId} — Timer={projDef.Timer:F2}s (isAlt={isAlt})");
#endif

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

            // SoF2 MISSILE_PRESTEP: Projektil wird 32 QU (0.8128m) vor dem Muzzle gespawnt,
            // damit es nicht den eigenen Spieler-Collider trifft und sofort detoniert.
            // Wall-Check: Falls eine Wand naeher als der Offset ist, dort spawnen.
            const float MISSILE_PRESTEP = 32f * 0.0254f;
            Vector3 spawnPos = eyePos;
            int wallMask = LayerMask.GetMask("Default") | LayerMask.GetMask("BrushCollision");
            if (Physics.Raycast(eyePos, aimDirection, out RaycastHit wallCheck, MISSILE_PRESTEP, wallMask))
            {
                spawnPos = wallCheck.point - aimDirection * 0.02f;
            }
            else
            {
                spawnPos = eyePos + aimDirection * MISSILE_PRESTEP;
            }

            // Fire-Sound aus Waffen-Definition (SoF2: flashSound)
            string projFireSoundPath = ResolveWeaponSoundPath(weapon, isAlt ? "altFire" : "fire");

            // Muzzle-Effekte fuer Sofort-Projektile (RPG, MM1) — nicht fuer gekochte Granaten
            MuzzleEffectsClientRpc(
                attackDef.MuzzleFlash ?? "",
                attackDef.MuzzleFlashInworld ?? "",
                attackDef.MuzzleSmoke ?? "",
                attackDef.ShellCasingEject ?? "",
                attackDef.EjectBone ?? "",
                "",
                projFireSoundPath,
                attackDef.Volume > 0f ? attackDef.Volume : 1f
            );

            SpawnProjectile(spawnPos, aimDirection, attackDef, projDef);

            // KickAngles: Rueckstoss an Owner-Client senden
            // SoF2 Skalierung: ×0.5 (kickPitch += value*500, extract /1000)
            if (attackDef.KickAngles != null && attackDef.KickAngles.Count >= 4)
            {
                float pitchKick = UnityEngine.Random.Range(attackDef.KickAngles[0], attackDef.KickAngles[1]) * 0.5f;
                float yawKick = UnityEngine.Random.Range(attackDef.KickAngles[2], attackDef.KickAngles[3]) * 0.5f;
                ApplyKickAnglesClientRpc(pitchKick, yawKick);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: Projectile ({projDef.Detonation}) spawned for client {OwnerClientId} — Speed={projDef.Speed} Gravity={projDef.Gravity}");
#endif
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
                projectileId,
                projDef.ExplosionEffect ?? ""
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
                projectileId,
                projDef.ExplosionEffect ?? ""
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: Grenade {(timerExpired ? "EXPLODED IN HAND" : "thrown")} for client {OwnerClientId} — RemainingTimer={remainingTimer:F2}s, TimerExpired={timerExpired}, ThrowFrames={m_ServerGrenadeThrowFramesRemaining}");
#endif
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
            // SoF2: Kein extra PMD nach Throw — der Release der den Throw triggert hat PMD_ATTACK
            // bereits cleared. noActionRunning (ThrowFrames > 0) verhindert Re-Fire waehrend Throw.
            if (m_ServerGrenadeThrowFramesRemaining <= 0)
            {
                if (m_CharacterState.CanReload())
                {
                    m_CharacterState.CompleteReload();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[NetworkedPlayerCharacter] Server: Grenade auto-reload after throw for client {OwnerClientId}");
#endif
                }
            }
        }

        /// <summary>
        /// Server → Alle Clients: Projektil-Spawn fuer Client-seitige Visualisierung.
        /// Erstellt ein ClientProjectileVisual mit datengetriebenem Trail-Effekt auf allen Clients.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void ProjectileSpawnClientRpc(Vector3 spawnPosition, Vector3 direction, float speed, float gravity, float bounce, string detonation, float timer, uint projectileId, string effectId, string explosionEffectId, string modelKey, string loopSoundPath)
        {
            // Dedicated Server: keine visuellen Effekte — Shader sind gestripped.
            if (IsServer && !IsHost)
            {
                return;
            }

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
            // Dedicated Server: keine visuellen Effekte (Tracer, Impact, Sound) — Shader sind gestripped.
            if (IsServer && !IsHost)
            {
                return;
            }

            // Tracer-Startpunkt: Owner sieht Tracer ab Eye-Position (= Crosshair-Linie),
            // andere Clients sehen Tracer ab Waffen-Muendung (EjectBone) fuer visuellen Realismus.
            // Ohne diese Trennung entsteht Parallaxe: Crosshair zeigt auf Trefferpunkt (Eye-Ray),
            // aber Tracer startet tiefer (Barrel) → sieht verschoben aus fuer den Schuetzen.
            Vector3 tracerStart = serverStart;
            if (!IsOwner && m_Animator != null)
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
            if (s_CachedSpritesShader == null)
            {
                s_CachedSpritesShader = Shader.Find("Sprites/Default");
            }
            Material tracerMat = new(s_CachedSpritesShader);
            lr.material = tracerMat;
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.useWorldSpace = true;

            // Material muss separat zerstoert werden, da Destroy(GameObject) es nicht freigibt.
            Destroy(tracerMat, TRACER_DURATION);
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
        private void MuzzleEffectsClientRpc(string muzzleFlashId, string muzzleFlashInworldId, string muzzleSmokeId,
            string shellCasingId, string ejectBoneName, string shellsoundPath, string fireSoundPath, float soundVolume)
        {
            // Dedicated Server: keine visuellen/Audio-Effekte — Shader und Audio sind gestripped.
            if (IsServer && !IsHost)
            {
                return;
            }

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
                    // SoF2: Owner sieht 1P-Flash (depthHack), andere Spieler sehen _inworld-Variante
                    string resolvedFlashId = muzzleFlashId;
                    if (!IsOwner && !string.IsNullOrEmpty(muzzleFlashInworldId))
                    {
                        resolvedFlashId = muzzleFlashInworldId;
                    }

                    effectFactory.SpawnMuzzleEffect(flashPos, flashRot, resolvedFlashId);
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

            // SoF2 bg_pmove.c:3255 — pm_debounce setzen bei erfolgreichem AltAttack
            m_ServerDebounce |= PMD_ALTATTACK;

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[NetworkedPlayerCharacter] Server: Alt-ammo auto-reload after projectile for client {OwnerClientId}");
#endif
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
                    altAttackDef.MuzzleFlashInworld ?? "",
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
                m_OwnHitboxSystem?.SetHitboxesEnabled(false);

                int hitboxLayerMask = LayerMask.GetMask("Hitbox");
                int playerLayerMask = LayerMask.GetMask(PLAYER_LAYER_NAME);

                // Zwei separate Raycasts: Hitbox-Trigger und Welt-Geometrie.
                bool didHitBone = Physics.Raycast(eyePos, aimDirection, out RaycastHit boneHit, rangeMeters, hitboxLayerMask, QueryTriggerInteraction.Collide);
                int worldLayerMask = ~(hitboxLayerMask | playerLayerMask | LayerMask.GetMask("BrushCollision"));
                bool didHitWorld = Physics.Raycast(eyePos, aimDirection, out RaycastHit worldHit, rangeMeters, worldLayerMask);

                float boneDist = didHitBone ? boneHit.distance : float.MaxValue;
                float worldDist = didHitWorld ? worldHit.distance : float.MaxValue;

                Vector3 hitPoint;
                Vector3 impactNormal = Vector3.zero;
                string impactEffectId = "";
                string debrisEffectId = "";
                string impactSoundPath = "";

                bool boneResolved = false;
                HitRegion resolvedRegion = default;
                float resolvedMultiplier = 1.0f;

                string altAmmoType = weapon?.AltAttack?.Ammo?.Type ?? weapon?.Ammo?.Type ?? "";
                if (didHitBone && boneDist <= worldDist)
                {
                    HitboxCollider hitboxCollider = boneHit.collider.GetComponent<HitboxCollider>();
                    if (hitboxCollider != null)
                    {
                        boneResolved = true;
                        resolvedRegion = hitboxCollider.HitRegion;
                        resolvedMultiplier = hitboxCollider.DamageMultiplier;
                        hitPoint = boneHit.point;
                        impactNormal = -aimDirection.normalized;

                        SurfaceImpactDataLoader surfaceLoader = ServiceLocator.Get<SurfaceImpactDataLoader>();
                        if (surfaceLoader != null)
                        {
                            impactEffectId = surfaceLoader.GetImpactEffectId("flesh", altAmmoType);
                            debrisEffectId = surfaceLoader.GetDebrisEffectId("flesh", altAmmoType);
                            impactSoundPath = surfaceLoader.GetImpactSoundPath("flesh", altAmmoType);
                        }
                    }
                    else
                    {
                        hitPoint = boneHit.point;
                    }
                }
                else if (didHitWorld)
                {
                    hitPoint = worldHit.point;
                    impactNormal = worldHit.normal;

                    SurfaceTypeMarker surfaceMarker = worldHit.collider.GetComponentInParent<SurfaceTypeMarker>();
                    string surfaceType = surfaceMarker != null ? surfaceMarker.SurfaceType : "default";

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

                if (boneResolved)
                {
                    int finalDamage = Mathf.RoundToInt(altAttackDef.Damage * resolvedMultiplier);

                    NetworkedCharacterState targetState = boneHit.collider.GetComponentInParent<NetworkedCharacterState>();
                    if (targetState != null && targetState != m_CharacterState)
                    {
                        int previousHealth = targetState.Health;
                        int newHealth = Mathf.Max(0, previousHealth - finalDamage);
                        targetState.SetHealth(newHealth);

                        bool isKill = newHealth <= 0 && previousHealth > 0;
                        HitConfirmRpc((int)resolvedRegion, finalDamage, isKill);

                        if (altAttackDef.Gore)
                        {
                            TryProcessGoreHit(targetState.gameObject, resolvedRegion, aimDirection, boneHit.point, finalDamage, previousHealth, newHealth, m_CharacterState.CurrentWeaponName, true);
                        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[NetworkedPlayerCharacter] Server: AltAttack HIT! Client {OwnerClientId} → {targetState.CharacterName} | Region={resolvedRegion} | Damage={finalDamage} (Base={altAttackDef.Damage} × {resolvedMultiplier:F2}) | Health={newHealth}");
#endif
                    }
                }

                m_ServerPlayerCharacter.SetPhysicsColliderEnabled(true);
                m_OwnHitboxSystem?.SetHitboxesEnabled(true);

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: AltAttack aus Command #{cmd.SequenceNumber} für Client {OwnerClientId} — {m_ServerAltAttackFrames} Frames @ {m_ServerAltAttackFps}fps Cooldown");
#endif
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
        /// Leitet einen server-seitigen Treffer in das Gore-System weiter.
        /// Nutzt eine SoF2-nahe DamageLevel-Einordnung basierend auf verbleibender Health
        /// und der Schwere des finalen Treffers.
        /// Bei Dismemberment (DamageLevel >= 4) werden Hitboxen SOFORT serverseitig deaktiviert,
        /// damit nachfolgende Raycasts (gleicher Frame / naechster Frame) nicht mehr treffen.
        /// Die ClientRpc-basierte Deaktivierung ist asynchron und hat 1+ Frame Verzoegerung.
        /// </summary>
        private void TryProcessGoreHit(
            GameObject targetCharacterRoot,
            HitRegion hitRegion,
            Vector3 shotDirection,
            Vector3 hitPoint,
            int finalDamage,
            int previousHealth,
            int newHealth,
            string weaponId,
            bool isAltAttack)
        {
            if (targetCharacterRoot == null || !IsServer)
            {
                return;
            }

            int damageLevel = ComputeSoF2DamageLevel(finalDamage, previousHealth, newHealth);

            // Server-seitige sofortige Hitbox-Deaktivierung bei Dismemberment.
            // Ohne dies kann der Server dismemberte Hitboxen fuer 1+ Frames weiter treffen,
            // weil die ClientRpc-basierte Deaktivierung erst im naechsten Network-Tick greift.
            if (damageLevel >= 4)
            {
                ClientHitboxSystem targetHitboxSystem = targetCharacterRoot.GetComponentInChildren<ClientHitboxSystem>();
                if (targetHitboxSystem != null)
                {
                    targetHitboxSystem.DisableHitboxForRegionAndChildren(hitRegion);
                }
            }

            NetworkObject targetNetworkObject = targetCharacterRoot.GetComponent<NetworkObject>();
            if (targetNetworkObject == null)
            {
                return;
            }

            ApplyGoreHitClientRpc(
                targetNetworkObject,
                (int)hitRegion,
                damageLevel,
                shotDirection,
                hitPoint,
                weaponId ?? "",
                isAltAttack);
        }

        [ClientRpc]
        private void ApplyGoreHitClientRpc(
            NetworkObjectReference targetCharacterRef,
            int hitRegionValue,
            int damageLevel,
            Vector3 hitDirection,
            Vector3 hitPoint,
            string weaponId,
            bool isAltAttack)
        {
            if (!targetCharacterRef.TryGet(out NetworkObject targetCharacterNetworkObject) || targetCharacterNetworkObject == null)
            {
                return;
            }

            GoreManager goreManager = ServiceLocator.Get<GoreManager>();
            if (goreManager == null)
            {
                return;
            }

            GoreHitData hitData = new()
            {
                CharacterRoot = targetCharacterNetworkObject.gameObject,
                HitRegion = (HitRegion)hitRegionValue,
                DamageLevel = damageLevel,
                HitDirection = hitDirection,
                HitPoint = hitPoint,
                WeaponId = weaponId,
                IsAltAttack = isAltAttack,
            };

            goreManager.ProcessGoreHit(hitData);
        }

        /// <summary>
        /// Approximiert die SoF2 DamageLevel-Skala (0..5) fuer das Gore-System.
        /// 0-3: nicht-toedlich, 4-5: toedlich mit Dismemberment.
        /// </summary>
        private int ComputeSoF2DamageLevel(int finalDamage, int previousHealth, int newHealth)
        {
            if (newHealth <= 0)
            {
                // Hoher Overkill wird als High Death (5) behandelt.
                if (finalDamage >= 75 || previousHealth <= 35)
                {
                    return 5;
                }

                return 4;
            }

            if (finalDamage >= 60)
            {
                return 3;
            }

            if (finalDamage >= 35)
            {
                return 2;
            }

            if (finalDamage >= 15)
            {
                return 1;
            }

            return 0;
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

            // Reload-Sound-Events laden und Tracking zuruecksetzen
            WeaponDataLoader soundLoader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition soundWeapon = soundLoader?.GetById(m_CharacterState.CurrentWeaponName);
            m_ServerReloadSounds = soundWeapon?.ReloadSounds;
            m_ServerReloadSoundsFired = 0;
            m_ServerShellPhaseSoundsFired = 0;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: Reload started for client {OwnerClientId} — Phase={m_ServerShellReloadPhase}, ShellsRemaining={m_ServerShellsRemaining}");
#endif
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

            // Shell-Phase-Sound-Events pruefen
            TickServerShellReloadSounds();

            if (m_ServerShellPhaseFramesRemaining > 0)
            {
                return;
            }

            switch (m_ServerShellReloadPhase)
            {
                case ShellReloadPhase.Start:
                    m_ServerShellReloadPhase = ShellReloadPhase.Shell;
                    m_ServerShellPhaseFramesRemaining = m_ServerReloadShellFrames;
                    m_ServerShellPhaseSoundsFired = 0;
                    break;

                case ShellReloadPhase.Shell:
                    m_CharacterState.TransferOneShell();
                    m_ServerShellsRemaining--;

                    if (m_ServerShellsRemaining > 0)
                    {
                        m_ServerShellPhaseFramesRemaining = m_ServerReloadShellFrames;
                        m_ServerShellPhaseSoundsFired = 0;
                    }
                    else
                    {
                        m_ServerShellReloadPhase = ShellReloadPhase.End;
                        m_ServerShellPhaseFramesRemaining = m_ServerReloadEndFrames;
                        m_ServerShellPhaseSoundsFired = 0;
                    }
                    break;

                case ShellReloadPhase.End:
                    m_ServerShellReloadPhase = ShellReloadPhase.None;
                    m_ServerReloadFramesRemaining = 0;
                    m_ServerReloadFrameAccumulator = 0f;
                    break;
            }
        }

        /// <summary>
        /// Server: Prueft und feuert Reload-Sound-Events fuer Standard-Reloads (einzelne Animation).
        /// Berechnet den aktuellen Fortschritt (0-1) und feuert alle faelligen Events per RPC.
        /// </summary>
        private void TickServerReloadSounds()
        {
            ReloadSoundEvent[] events = m_ServerReloadSounds?.Events;
            if (events == null || events.Length == 0 || m_ServerReloadFrames <= 0)
            {
                return;
            }

            float progress = 1f - (float)m_ServerReloadFramesRemaining / m_ServerReloadFrames;

            for (int i = 0; i < events.Length && i < 32; i++)
            {
                if ((m_ServerReloadSoundsFired & (1 << i)) != 0)
                {
                    continue;
                }

                if (progress >= events[i].Time)
                {
                    m_ServerReloadSoundsFired |= (1 << i);
                    FireReloadSoundRpc(events[i].Sound);
                }
            }
        }

        /// <summary>
        /// Server: Prueft und feuert Reload-Sound-Events fuer Shell-Reloads (phasenbasiert).
        /// Waehlt die Events der aktuellen Phase und berechnet den Phase-Fortschritt (0-1).
        /// </summary>
        private void TickServerShellReloadSounds()
        {
            if (m_ServerReloadSounds == null)
            {
                return;
            }

            ReloadSoundEvent[] events;
            int totalPhaseFrames;

            switch (m_ServerShellReloadPhase)
            {
                case ShellReloadPhase.Start:
                    events = m_ServerReloadSounds.StartEvents;
                    totalPhaseFrames = m_ServerReloadStartFrames;
                    break;
                case ShellReloadPhase.Shell:
                    events = m_ServerReloadSounds.ShellEvents;
                    totalPhaseFrames = m_ServerReloadShellFrames;
                    break;
                case ShellReloadPhase.End:
                    events = m_ServerReloadSounds.EndEvents;
                    totalPhaseFrames = m_ServerReloadEndFrames;
                    break;
                default:
                    return;
            }

            if (events == null || events.Length == 0 || totalPhaseFrames <= 0)
            {
                return;
            }

            float progress = 1f - (float)m_ServerShellPhaseFramesRemaining / totalPhaseFrames;

            for (int i = 0; i < events.Length && i < 32; i++)
            {
                if ((m_ServerShellPhaseSoundsFired & (1 << i)) != 0)
                {
                    continue;
                }

                if (progress >= events[i].Time)
                {
                    m_ServerShellPhaseSoundsFired |= (1 << i);
                    FireReloadSoundRpc(events[i].Sound);
                }
            }
        }

        /// <summary>
        /// Loest den Sound-Key auf und sendet den Reload-Sound per RPC an alle Clients.
        /// </summary>
        private void FireReloadSoundRpc(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey))
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
            string soundPath = ResolveWeaponSoundPath(weapon, soundKey);

            if (!string.IsNullOrEmpty(soundPath))
            {
                ReloadSoundClientRpc(soundPath);
            }
        }

        /// <summary>
        /// Server → Alle Clients: Spielt einen Reload-Sound (clipOut, clipIn, boltRelease etc.).
        /// SoF2-Referenz: Animation-Events triggerten Waffensounds bei bestimmten Reload-Frames.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        private void ReloadSoundClientRpc(string soundPath)
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
            GameObject soundObj = new("ReloadSoundFX");
            soundObj.transform.position = soundPos;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 20f;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            UnityEngine.Object.Destroy(soundObj, clip.length + 0.1f);
        }

        // ===== Weapon Swap (Drop/Raise) =====

        /// <summary>
        /// Server: Startet den Waffenwechsel-Prozess (Drop alte Waffe, dann Raise neue Waffe).
        /// Wird von NetworkedCharacterState via OnWeaponSwapRequested aufgerufen.
        /// SoF2 pm_shared.c: weaponTime blockiert ALLE Aktionen waehrend Drop+Raise.
        /// Kein Re-Switch waehrend laufendem Swap (weder Drop noch Raise).
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

            // SoF2: Kein neuer Waffenwechsel waehrend laufendem Swap (Drop oder Raise).
            // pm_shared.c: weaponTime > 0 blockiert PM_BeginWeaponChange komplett.
            if (m_ServerIsSwapping)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            // Raise-Daten der Zielwaffe lesen
            int raiseFrames = 6;
            int raiseFps = 10;
            WeaponDefinition targetWeaponDef = loader.GetById(targetWeapon);
            if (targetWeaponDef?.Animations != null &&
                targetWeaponDef.Animations.TryGetValue("mp_raise", out WeaponAnimationEntry raiseAnim))
            {
                raiseFrames = raiseAnim.Duration;
                raiseFps = raiseAnim.Fps;
            }

            // Drop-Daten der aktuellen Waffe
            int dropFrames = 6;
            int dropFps = 10;
            string dropSourceWeapon = m_CharacterState.CurrentWeaponName;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedPlayerCharacter] Server: Weapon swap started for client {OwnerClientId}: {dropSourceWeapon} → {targetWeapon} (Drop {dropFrames}f@{dropFps}fps, Raise {raiseFrames}f@{raiseFps}fps)");
#endif
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
                    UpdateServerFireModeParameters();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[NetworkedPlayerCharacter] Server: Weapon swap completed for client {OwnerClientId}");
#endif
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

                // Server: Kein Kamera-Culling — Animator muss ohne sichtbare Kamera evaluieren
                if (IsServer)
                {
                    m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[NetworkedPlayerCharacter] Animator gefunden auf Visual für Character {CharacterId} (Root Motion deaktiviert, Server={IsServer})");
#endif
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
