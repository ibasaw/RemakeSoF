using System;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Basis-NetworkBehaviour für alle Character (Player + NPCs).
    /// Server Authority: Server ist Source of Truth für Position und State.
    /// Clients senden Input per RPC, Server validiert und broadcastet.
    /// </summary>
    public abstract class NetworkedCharacter : NetworkBehaviour
    {
        /// <summary>
        /// Eindeutige Character-ID (wird vom Server gesetzt).
        /// </summary>
        [SerializeField]
        protected NetworkVariable<ulong> m_CharacterId = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Autoritative Position vom Server (Source of Truth).
        /// </summary>
        protected NetworkVariable<Vector3> m_ServerPosition = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Autoritative Rotation vom Server (Source of Truth).
        /// </summary>
        protected NetworkVariable<Quaternion> m_ServerRotation = new(
            Quaternion.identity,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Interpolationsgeschwindigkeit für Remote-Clients.
        /// </summary>
        protected const float k_RemoteInterpolationSpeed = 15f;



        /// <summary>
        /// Invoked after OnNetworkSpawn to notify non-NetworkBehaviour components.
        /// </summary>
        public event Action OnNetworkSpawnHook;

        /// <summary>
        /// Invoked after OnNetworkDespawn to notify non-NetworkBehaviour components.
        /// </summary>
        public event Action OnNetworkDespawnHook;

        public ulong CharacterId => m_CharacterId.Value;
        public Vector3 ServerPosition => m_ServerPosition.Value;
        public Quaternion ServerRotation => m_ServerRotation.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Debug.Log($"[NetworkedCharacter] OnNetworkSpawn | IsServer={IsServer} | IsOwner={IsOwner} | ClientId={OwnerClientId}");

            if (IsServer)
            {
                m_CharacterId.Value = NetworkObjectId;
                OnServerSpawn();
            }

            if (IsOwner && !IsServer)
            {
                OnOwnerSpawn();
            }
            else if (!IsOwner && !IsServer)
            {
                OnRemoteSpawn();
            }

            OnNetworkSpawnHook?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Debug.Log($"[NetworkedCharacter] OnNetworkDespawn | CharacterId={CharacterId}");
            OnNetworkDespawnHook?.Invoke();
        }

        /// <summary>
        /// Server: Character-Initialisierung.
        /// </summary>
        protected virtual void OnServerSpawn()
        {
            Debug.Log($"[NetworkedCharacter] Server: Character {NetworkObjectId} gespawnt");
        }

        /// <summary>
        /// Owner-Client: Input-Verarbeitung aktivieren, Client-Side Prediction starten.
        /// </summary>
        protected virtual void OnOwnerSpawn()
        {
            Debug.Log("[NetworkedCharacter] Owner-Client: Prediction aktiviert");
        }

        /// <summary>
        /// Remote-Client: Interpolation starten.
        /// </summary>
        protected virtual void OnRemoteSpawn()
        {
            Debug.Log("[NetworkedCharacter] Remote-Client: Interpolation aktiviert");
        }

    }
}
