using System;
using System.Collections;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
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

        // ===== Server-Side Attack Gating (SoF2: weaponTime in playerState_t) =====

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

            // Button-Inputs verarbeiten (SoF2: FireWeapon aus usercmd_t.buttons)
            // Server gated: Attack nur starten wenn keine Attacke, kein Reload, kein AltAttack und kein Swap laeuft (Anti-Cheat)
            bool noActionRunning = m_ServerAttackFramesRemaining <= 0 && m_ServerReloadFramesRemaining <= 0 && m_ServerAltAttackFramesRemaining <= 0 && !m_ServerIsSwapping;

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
        /// </summary>
        private void ProcessAttack(PlayerCommand cmd)
        {
            // Munition verbrauchen (Server-autoritativ)
            if (!m_CharacterState.TryConsumeAmmo())
            {
                Debug.Log($"[NetworkedPlayerCharacter] Server: Attack blocked — no ammo for client {OwnerClientId}");
                return;
            }

            // Attack-Parameter von aktueller Waffe laden
            UpdateServerAttackParameters();

            // Server startet Attack-Cooldown (frame-basiert, wie SoF2 weaponTime)
            m_ServerAttackFramesRemaining = m_ServerAttackFrames;
            m_ServerAttackFrameAccumulator = 0f;

            Debug.Log($"[NetworkedPlayerCharacter] Server: Attack aus Command #{cmd.SequenceNumber} für Client {OwnerClientId} bei Yaw {cmd.YawAngle:F1} — {m_ServerAttackFrames} Frames @ {m_ServerAttackFps}fps Cooldown");

            // TODO: Server-seitige Hit-Detection (Raycast/SphereCast von Server-Position in Blickrichtung)
            // TODO: Damage an getroffene Spieler via HitboxCollider.HitRegion + DamageMultiplier
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
        }

        /// <summary>
        /// Server: Verarbeitet einen AltAttack aus dem PlayerCommand.
        /// Wie SoF2 AltFire — konsumiert Alt-Ammo und startet AltAttack-Cooldown.
        /// </summary>
        private void ProcessAltAttack(PlayerCommand cmd)
        {
            // Alt-Munition verbrauchen (Server-autoritativ)
            if (!m_CharacterState.TryConsumeAltAmmo())
            {
                Debug.Log($"[NetworkedPlayerCharacter] Server: AltAttack blocked — no alt ammo for client {OwnerClientId}");
                return;
            }

            // AltAttack-Parameter von aktueller Waffe laden
            UpdateServerAltAttackParameters();

            if (m_ServerAltAttackFrames <= 0)
            {
                return;
            }

            // Server startet AltAttack-Cooldown (frame-basiert)
            m_ServerAltAttackFramesRemaining = m_ServerAltAttackFrames;
            m_ServerAltAttackFrameAccumulator = 0f;

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
                    // Raise fertig: Swap abgeschlossen
                    m_ServerIsSwapping = false;
                    m_ServerSwapPhase = WeaponSwapPhase.None;
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
    }
}
