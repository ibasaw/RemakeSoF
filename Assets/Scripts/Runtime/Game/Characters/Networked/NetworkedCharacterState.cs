using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Netzwerk-synchronisierter State für Character.
    /// Enthält alle Werte, die zwischen Server und Clients synchronisiert werden,
    /// inklusive Skin-Daten pro Character.
    /// </summary>
    public class NetworkedCharacterState : NetworkBehaviour
    {
        // ===== Identity =====

        /// <summary>
        /// Character-Name.
        /// </summary>
        [SerializeField]
        private NetworkVariable<FixedString64Bytes> m_CharacterName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ===== Combat State =====

        /// <summary>
        /// Ist der Character noch am Leben?
        /// </summary>
        [SerializeField]
        private NetworkVariable<bool> m_IsAlive = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Aktuelle Health.
        /// </summary>
        [SerializeField]
        private NetworkVariable<int> m_Health = new(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ===== Team & Score =====

        /// <summary>
        /// Team-ID (0 = Team A, 1 = Team B, etc.).
        /// </summary>
        [SerializeField]
        private NetworkVariable<uint> m_TeamId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Aktuelle Score/Kills.
        /// </summary>
        [SerializeField]
        private NetworkVariable<int> m_Kills = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Aktuelle Deaths.
        /// </summary>
        [SerializeField]
        private NetworkVariable<int> m_Deaths = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ===== Skin / Visual =====

        /// <summary>
        /// Der aktuelle Skin-Name des Characters.
        /// Owner darf seinen eigenen Skin ändern (z.B. im Metagame).
        /// </summary>
        private NetworkVariable<FixedString128Bytes> m_CurrentSkinName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        // ===== Public Properties =====

        public string CharacterName => m_CharacterName.Value.ToString();
        public bool IsAlive => m_IsAlive.Value;
        public int Health => m_Health.Value;
        public uint TeamId => m_TeamId.Value;
        public int Kills => m_Kills.Value;
        public int Deaths => m_Deaths.Value;
        public string CurrentSkinName => m_CurrentSkinName.Value.ToString();

        // ===== Events =====

        /// <summary>
        /// Event: Character Health hat sich geändert.
        /// </summary>
        public event System.Action<int> OnHealthChanged;

        /// <summary>
        /// Event: Character ist gestorben.
        /// </summary>
        public event System.Action OnCharacterDied;

        /// <summary>
        /// Event: Character ist respawned.
        /// </summary>
        public event System.Action OnCharacterRespawned;

        /// <summary>
        /// Event: Skin hat sich geändert (skinName).
        /// </summary>
        public event System.Action<string> OnSkinChanged;

        // ===== Lifecycle =====

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_Health.OnValueChanged += OnHealthValueChanged;
            m_IsAlive.OnValueChanged += OnIsAliveValueChanged;
            m_CurrentSkinName.OnValueChanged += OnSkinNameValueChanged;

            Debug.Log($"[NetworkedCharacterState] OnNetworkSpawn | Name={CharacterName} | Health={Health} | Skin={CurrentSkinName}");

            // Initiales Skin-Event feuern, falls bereits ein Skin-Name gesetzt ist
            // (z.B. bei Late-Join, wenn Owner den Skin vor unserem Spawn gesetzt hat)
            string initialSkin = CurrentSkinName;
            if (!string.IsNullOrEmpty(initialSkin))
            {
                OnSkinChanged?.Invoke(initialSkin);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            m_Health.OnValueChanged -= OnHealthValueChanged;
            m_IsAlive.OnValueChanged -= OnIsAliveValueChanged;
            m_CurrentSkinName.OnValueChanged -= OnSkinNameValueChanged;
        }

        // ===== Server Setters =====

        /// <summary>
        /// Server: Setzt den Character-Namen.
        /// </summary>
        public void SetCharacterName(string name)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedCharacterState] SetCharacterName can only be called on the server.");
                return;
            }

            m_CharacterName.Value = new FixedString64Bytes(name);
        }

        /// <summary>
        /// Server: Setzt das Team.
        /// </summary>
        public void SetTeam(uint teamId)
        {
            if (!IsServer)
            {
                return;
            }

            m_TeamId.Value = teamId;
        }

        /// <summary>
        /// Server: Setzt Health.
        /// </summary>
        public void SetHealth(int health)
        {
            if (!IsServer)
            {
                return;
            }

            m_Health.Value = Mathf.Max(0, health);
        }

        /// <summary>
        /// Server: Setzt den Alive-Status.
        /// </summary>
        public void SetIsAlive(bool isAlive)
        {
            if (!IsServer)
            {
                return;
            }

            m_IsAlive.Value = isAlive;
        }

        /// <summary>
        /// Server: Addiert einen Kill.
        /// </summary>
        public void AddKill()
        {
            if (!IsServer)
            {
                return;
            }

            m_Kills.Value++;
        }

        /// <summary>
        /// Server: Addiert einen Death.
        /// </summary>
        public void AddDeath()
        {
            if (!IsServer)
            {
                return;
            }

            m_Deaths.Value++;
        }

        // ===== Owner Setters (Skin) =====

        /// <summary>
        /// Owner: Setzt den aktuellen Skin-Namen.
        /// </summary>
        public void SetCurrentSkinName(string skinName)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("[NetworkedCharacterState] SetCurrentSkinName can only be called by the owner.");
                return;
            }

            m_CurrentSkinName.Value = new FixedString128Bytes(skinName);
            Debug.Log($"[NetworkedCharacterState] Owner {OwnerClientId} set skin to: {skinName}");
        }

        /// <summary>
        /// Registriert einen Callback für Skin-Änderungen (für Legacy-Kompatibilität).
        /// </summary>
        public void RegisterOnSkinChanged(NetworkVariable<FixedString128Bytes>.OnValueChangedDelegate callback)
        {
            m_CurrentSkinName.OnValueChanged += callback;
        }

        /// <summary>
        /// Deregistriert einen Callback für Skin-Änderungen.
        /// </summary>
        public void UnregisterOnSkinChanged(NetworkVariable<FixedString128Bytes>.OnValueChangedDelegate callback)
        {
            m_CurrentSkinName.OnValueChanged -= callback;
        }

        // ===== Value Changed Handlers =====

        private void OnHealthValueChanged(int oldValue, int newValue)
        {
            Debug.Log($"[NetworkedCharacterState] Health changed: {oldValue} → {newValue}");
            OnHealthChanged?.Invoke(newValue);
        }

        private void OnIsAliveValueChanged(bool oldValue, bool newValue)
        {
            if (!newValue)
            {
                Debug.Log("[NetworkedCharacterState] Character died");
                OnCharacterDied?.Invoke();
            }
            else
            {
                Debug.Log("[NetworkedCharacterState] Character respawned");
                OnCharacterRespawned?.Invoke();
            }
        }

        private void OnSkinNameValueChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
        {
            Debug.Log($"[NetworkedCharacterState] Skin changed: {oldValue} → {newValue}");
            OnSkinChanged?.Invoke(newValue.ToString());
        }
    }
}
