using System;
using System.Collections;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
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
        private static readonly int s_JumpHash = Animator.StringToHash("Jump");

        /// <summary>
        /// Aktueller synchronisierter Animation-State (für Remote-Bone-Rotation).
        /// </summary>
        public NetworkAnimationState CurrentAnimationState => m_AnimationState.Value;

        // ===== Server-Side Attack Gating (SoF2: weaponTime in playerState_t) =====

        /// <summary>Verbleibende Attack-Frames auf dem Server (autoritativ, nicht manipulierbar).</summary>
        private int m_ServerAttackFramesRemaining;

        /// <summary>Frame-Akkumulator auf dem Server fuer frame-diskretes Timing.</summary>
        private float m_ServerAttackFrameAccumulator;

        /// <summary>Anzahl Animation-Frames fuer einen Knife-Slash (SoF2: knifeslash01_mp duration=6).</summary>
        private const int k_ServerAttackFrames = 6;

        /// <summary>FPS der Attack-Animation (SoF2: knifeslash01_mp fps=20).</summary>
        private const int k_ServerAttackFps = 20;

        // ===== Movement Sync =====



        /// <summary>
        /// Wird auf dem Server aufgerufen: Spawn-Point zuweisen und Server-Position setzen.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            // Server-seitige Physik + CapsuleCollider initialisieren
            m_ServerPlayerCharacter.InitializeServer();

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
        public void SendCapsuleDimensions(float height, float radius, Vector3 center, float groundCheckDist)
        {
            if (!IsOwner)
            {
                return;
            }

            if (IsServer)
            {
                // Host-Mode: ServerPlayerCharacter direkt setzen
                m_ServerPlayerCharacter.SetCapsuleDimensions(height, radius, center, groundCheckDist);
                return;
            }

            SubmitCapsuleDimensionsServerRpc(height, radius, center, groundCheckDist);
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
                float frameInterval = 1f / k_ServerAttackFps;
                while (m_ServerAttackFrameAccumulator >= frameInterval && m_ServerAttackFramesRemaining > 0)
                {
                    m_ServerAttackFrameAccumulator -= frameInterval;
                    m_ServerAttackFramesRemaining--;
                }
            }

            // Button-Inputs verarbeiten (SoF2: FireWeapon aus usercmd_t.buttons)
            // Server gated: Attack nur starten wenn keine Attacke laeuft (Anti-Cheat)
            if (cmd.HasButton(CommandButtons.Attack) && m_ServerAttackFramesRemaining <= 0)
            {
                ProcessAttack(cmd);
            }

            // Acknowledgement an Owner-Client senden (für Reconciliation)
            MovementAckClientRpc(ack);
        }

        /// <summary>
        /// Server: Empfängt Capsule-Dimensionen vom Client und setzt sie auf der Server-Simulation.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SubmitCapsuleDimensionsServerRpc(float height, float radius, Vector3 center, float groundCheckDist)
        {
            m_ServerPlayerCharacter.SetCapsuleDimensions(height, radius, center, groundCheckDist);
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
            // Server startet Attack-Cooldown (frame-basiert, wie SoF2 weaponTime)
            m_ServerAttackFramesRemaining = k_ServerAttackFrames;
            m_ServerAttackFrameAccumulator = 0f;

            Debug.Log($"[NetworkedPlayerCharacter] Server: Attack aus Command #{cmd.SequenceNumber} für Client {OwnerClientId} bei Yaw {cmd.YawAngle:F1} — {k_ServerAttackFrames} Frames @ {k_ServerAttackFps}fps Cooldown");

            // TODO: Server-seitige Hit-Detection (Raycast/SphereCast von Server-Position in Blickrichtung)
            // TODO: Damage an getroffene Spieler via HitboxCollider.HitRegion + DamageMultiplier
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
                Debug.Log($"[NetworkedPlayerCharacter] Animator gefunden auf Visual für Character {CharacterId}");
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
    }
}
