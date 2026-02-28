using System;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Networked script to handle AI character logic that needs to be networked.
    /// Provides hooks for spawn and despawn events via Action delegates.
    /// </summary>
    public class NetworkedAICharacter : NetworkBehaviour, ICharacter
    {
        /// <summary>
        /// Invoked after <see cref="OnNetworkSpawn"/> to notify non-NetworkBehaviour components.
        /// </summary>
        public event Action OnNetworkSpawnHook;

        /// <summary>
        /// Invoked after <see cref="OnNetworkDespawn"/> to notify non-NetworkBehaviour components.
        /// </summary>
        public event Action OnNetworkDespawnHook;

        /// <summary>
        /// Synchronized AI movement speed for animation.
        /// </summary>
        NetworkVariable<float> m_Speed = new();

        /// <summary>
        /// Gets or sets the AI movement speed (server-authoritative).
        /// </summary>
        public float Speed
        {
            get => m_Speed.Value;
            set => m_Speed.Value = value;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnNetworkSpawnHook?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            OnNetworkDespawnHook?.Invoke();
        }
    }
}
