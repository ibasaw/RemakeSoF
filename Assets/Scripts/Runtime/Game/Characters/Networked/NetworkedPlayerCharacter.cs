using System;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Server Authority + Client-Side Prediction nach SoF2-Vorbild.
    /// Owner: Input lokal anwenden (Prediction) + RPC an Server senden.
    /// Server: Validiert Bewegung, setzt autoritative Position.
    /// Remote: Interpoliert von Server-Position.
    /// Trebt Animator-Parameter auf allen Clients via NetworkVariable.
    /// </summary>
    public class NetworkedPlayerCharacter : NetworkedCharacter, ICharacter
    {
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

        // ===== Movement Sync =====

        /// <summary>
        /// Letzte Server-Position für Reconciliation.
        /// </summary>
        private Vector3 m_LastServerPosition;

        /// <summary>
        /// Zeitpunkt des letzten Server-Updates (Server-seitig).
        /// </summary>
        private float m_LastServerUpdateTime;

        /// <summary>
        /// Schwellwert für Position-Korrektur (wenn Prediction zu weit ab ist).
        /// </summary>
        private const float k_ReconciliationThreshold = 0.5f;

        /// <summary>
        /// Wird auf dem Server aufgerufen: Spawn-Point zuweisen und Server-Position setzen.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            // Spawn-Point vom Server zuweisen
            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.LogWarning("[NetworkedPlayerCharacter] ServerPlayerSpawnPoints nicht verfügbar! Spawne bei Origin.");
                m_ServerPosition.Value = Vector3.zero;
                m_ServerRotation.Value = Quaternion.identity;
                m_LastServerPosition = Vector3.zero;
                m_LastServerUpdateTime = Time.time;
                return;
            }

            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            transform.position = position;
            transform.rotation = rotation;

            // Server-Position als Source of Truth setzen
            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;
            m_LastServerPosition = position;
            m_LastServerUpdateTime = Time.time;

            Debug.Log($"[NetworkedPlayerCharacter] Server: Spieler gespawnt bei {position}");
        }

        /// <summary>
        /// Owner-Client: Client-Side Prediction starten.
        /// CharacterController wird durch ClientPlayerCharacter aktiviert.
        /// </summary>
        protected override void OnOwnerSpawn()
        {
            base.OnOwnerSpawn();

            // Registriere auf Server-Position-Änderungen für Reconciliation
            m_ServerPosition.OnValueChanged += OnServerPositionCorrected;

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
            if (IsOwner && !IsServer)
            {
                m_ServerPosition.OnValueChanged -= OnServerPositionCorrected;
            }

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
        /// Owner-Client: Sendet die lokal vorhergesagte Position an den Server zur Validierung.
        /// Wird von ClientPlayerCharacter aufgerufen nachdem Input lokal angewendet wurde.
        /// </summary>
        /// <param name="predictedPosition">Die lokal vorhergesagte Position.</param>
        /// <param name="predictedRotation">Die lokal vorhergesagte Rotation.</param>
        /// <param name="clientDeltaTime">Die Zeit, die der Client für diese Bewegung brauchte (Time.deltaTime).</param>
        public void SendPredictedMovement(Vector3 predictedPosition, Quaternion predictedRotation, float clientDeltaTime)
        {
            if (!IsOwner || IsServer)
            {
                return;
            }

            SubmitMovementServerRpc(predictedPosition, predictedRotation, clientDeltaTime);
        }

        /// <summary>
        /// Owner-Client: Sendet einen Attack-Request an den Server.
        /// </summary>
        public void RequestAttack()
        {
            if (!IsOwner || IsServer)
            {
                return;
            }

            PerformAttackServerRpc();
        }

        /// <summary>
        /// Server: Empfängt die vorhergesagte Position vom Client und validiert.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SubmitMovementServerRpc(Vector3 predictedPosition, Quaternion predictedRotation, float clientDeltaTime)
        {
            // Server validiert die Bewegung basierend auf der Client-seitigen deltaTime
            // (nicht auf Netzwerk-Latenz, die ist in clientDeltaTime nicht enthalten)
            if (ValidateMovement(m_ServerPosition.Value, predictedPosition, clientDeltaTime))
            {
                // Akzeptiert: Setze als autoritative Position (Source of Truth)
                m_ServerPosition.Value = predictedPosition;
                m_ServerRotation.Value = predictedRotation;
                m_LastServerPosition = predictedPosition;
                m_LastServerUpdateTime = Time.time;

                // Server-Transform aktualisieren (für Kollisionserkennung etc.)
                transform.position = predictedPosition;
                transform.rotation = predictedRotation;
            }
            else
            {
                // Abgelehnt: Korrigiere den Client
                Debug.LogWarning($"[NetworkedPlayerCharacter] Server: Ungültige Bewegung von Client {OwnerClientId}. Korrigiere.");
                CorrectionClientRpc(m_ServerPosition.Value, m_ServerRotation.Value);
            }
        }

        /// <summary>
        /// Server → Owner-Client: Position-Korrektur bei Cheat-Verdacht oder ungültiger Bewegung.
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void CorrectionClientRpc(Vector3 correctPosition, Quaternion correctRotation)
        {
            // Owner-Client: Server hat die Position korrigiert → Prediction überschreiben
            transform.position = correctPosition;
            transform.rotation = correctRotation;

            Debug.LogWarning($"[NetworkedPlayerCharacter] Owner: Position vom Server korrigiert auf {correctPosition}");
        }

        /// <summary>
        /// Owner-Client: Reconciliation wenn Server-Position sich zu stark von Prediction unterscheidet.
        /// </summary>
        private void OnServerPositionCorrected(Vector3 oldPos, Vector3 newPos)
        {
            if (!IsOwner || IsServer)
            {
                return;
            }

            // Prüfe ob die Prediction zu weit von der Server-Position entfernt ist
            float discrepancy = Vector3.Distance(transform.position, newPos);
            if (discrepancy > k_ReconciliationThreshold)
            {
                // Snap zur Server-Position (Reconciliation)
                transform.position = newPos;
                Debug.Log($"[NetworkedPlayerCharacter] Owner: Reconciliation - Snap um {discrepancy:F2}m");
            }
        }

        /// <summary>
        /// Remote-Client: Smooth Interpolation zur Server-Position.
        /// </summary>
        private void InterpolateRemotePosition()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                m_ServerPosition.Value,
                Time.deltaTime * k_RemoteInterpolationSpeed
            );

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                m_ServerRotation.Value,
                Time.deltaTime * k_RemoteInterpolationSpeed
            );
        }

        /// <summary>
        /// Server: Validiert und verarbeitet einen Angriff.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void PerformAttackServerRpc()
        {
            Debug.Log($"[NetworkedPlayerCharacter] Server: Attack validiert für Client {OwnerClientId}");

            // TODO: Server-seitige Hit-Detection hier
            BroadcastAttackClientRpc();
        }

        /// <summary>
        /// Alle Clients: Attack-Animation abspielen.
        /// </summary>
        [Rpc(SendTo.NotServer)]
        private void BroadcastAttackClientRpc()
        {
            Debug.Log($"[NetworkedPlayerCharacter] Client: Attack-Animation für Character {CharacterId}");
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
