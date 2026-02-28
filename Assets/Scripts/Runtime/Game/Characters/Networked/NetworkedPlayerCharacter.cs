using System;
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
    /// </summary>
    public class NetworkedPlayerCharacter : NetworkedCharacter, ICharacter
    {
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

            Debug.Log("[NetworkedPlayerCharacter] Owner: Client-Side Prediction aktiv");
        }

        /// <summary>
        /// Remote-Client: Nur Interpolation, kein Input.
        /// </summary>
        protected override void OnRemoteSpawn()
        {
            base.OnRemoteSpawn();
            Debug.Log($"[NetworkedPlayerCharacter] Remote: Client {OwnerClientId} - Interpolation aktiv");
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && !IsServer)
            {
                m_ServerPosition.OnValueChanged -= OnServerPositionCorrected;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            // Remote-Clients: Interpolation zur Server-Position
            if (!IsOwner && !IsServer)
            {
                InterpolateRemotePosition();
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
    }
}
