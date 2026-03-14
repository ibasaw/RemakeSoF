using System.Collections.Generic;
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
        /// Nur der Server schreibt diesen Wert (wird initial aus dem ConnectionPayload gesetzt).
        /// Der Owner kann eine Änderung via <see cref="RequestSkinChangeServerRpc"/> anfragen.
        /// </summary>
        private readonly NetworkVariable<FixedString128Bytes> m_CurrentSkinName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ===== Weapon =====

        /// <summary>
        /// Der aktuelle Waffen-Name des Characters (Addressable-Key, z.B. "knife").
        /// Server-autoritativ: Owner fragt Wechsel an, Server validiert und setzt.
        /// Alle Clients reagieren auf Aenderung und laden/attachen die Waffe lokal.
        /// </summary>
        private readonly NetworkVariable<FixedString64Bytes> m_CurrentWeaponName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ===== Weapon Inventory =====

        /// <summary>
        /// Synchronisierte Liste der verfuegbaren Waffen des Characters.
        /// Server-autoritativ: Server fuegt Waffen hinzu/entfernt sie.
        /// Clients lesen die Liste fuer UI und Waffenwechsel-Logik.
        /// </summary>
        private NetworkList<FixedString64Bytes> m_WeaponInventory;

        /// <summary>
        /// Statisches Mapping von Waffen-Name auf Animator-Index.
        /// CurrentWeapon (Int) im Animator: 0 = knife, 1 = rpg7.
        /// </summary>
        private static readonly Dictionary<string, int> s_WeaponAnimatorIndices = new()
        {
            { "knife", 0 },
            { "rpg7", 1 },
        };

        // ===== Public Properties =====

        public string CharacterName => m_CharacterName.Value.ToString();
        public bool IsAlive => m_IsAlive.Value;
        public int Health => m_Health.Value;
        public uint TeamId => m_TeamId.Value;
        public int Kills => m_Kills.Value;
        public int Deaths => m_Deaths.Value;
        public string CurrentSkinName => m_CurrentSkinName.Value.ToString();
        public string CurrentWeaponName => m_CurrentWeaponName.Value.ToString();
        public int WeaponCount => m_WeaponInventory?.Count ?? 0;

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
        /// Event: Character-Name hat sich geändert.
        /// </summary>
        public event System.Action<string> OnCharacterNameChanged;

        /// <summary>
        /// Event: Skin hat sich geändert (skinName).
        /// </summary>
        public event System.Action<string> OnSkinChanged;

        /// <summary>
        /// Event: Waffe hat sich geändert (weaponName).
        /// </summary>
        public event System.Action<string> OnWeaponChanged;

        /// <summary>
        /// Event: Waffen-Inventar hat sich geaendert.
        /// </summary>
        public event System.Action OnWeaponInventoryChanged;

        // ===== Lifecycle =====

        private void Awake()
        {
            m_WeaponInventory = new NetworkList<FixedString64Bytes>(
                null,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_CharacterName.OnValueChanged += OnCharacterNameValueChanged;
            m_Health.OnValueChanged += OnHealthValueChanged;
            m_IsAlive.OnValueChanged += OnIsAliveValueChanged;
            m_CurrentSkinName.OnValueChanged += OnSkinNameValueChanged;
            m_CurrentWeaponName.OnValueChanged += OnWeaponNameValueChanged;
            m_WeaponInventory.OnListChanged += OnWeaponInventoryListChanged;

            Debug.Log($"[NetworkedCharacterState] OnNetworkSpawn | Name={CharacterName} | Health={Health} | Skin={CurrentSkinName} | Weapon={CurrentWeaponName}");

            // Initiale Events feuern, falls bereits Werte gesetzt sind
            // (z.B. bei Late-Join, wenn Server die Werte vor unserem Spawn gesetzt hat)
            string initialName = CharacterName;
            if (!string.IsNullOrEmpty(initialName))
            {
                OnCharacterNameChanged?.Invoke(initialName);
            }

            string initialSkin = CurrentSkinName;
            if (!string.IsNullOrEmpty(initialSkin))
            {
                OnSkinChanged?.Invoke(initialSkin);
            }

            string initialWeapon = CurrentWeaponName;
            if (!string.IsNullOrEmpty(initialWeapon))
            {
                OnWeaponChanged?.Invoke(initialWeapon);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            m_CharacterName.OnValueChanged -= OnCharacterNameValueChanged;
            m_Health.OnValueChanged -= OnHealthValueChanged;
            m_IsAlive.OnValueChanged -= OnIsAliveValueChanged;
            m_CurrentSkinName.OnValueChanged -= OnSkinNameValueChanged;
            m_CurrentWeaponName.OnValueChanged -= OnWeaponNameValueChanged;
            m_WeaponInventory.OnListChanged -= OnWeaponInventoryListChanged;
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
        /// Server: Setzt den aktuellen Skin-Namen (initial aus ConnectionPayload oder Skin-Wechsel).
        /// </summary>
        public void SetCurrentSkinName(string skinName)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedCharacterState] SetCurrentSkinName can only be called on the server.");
                return;
            }

            m_CurrentSkinName.Value = new FixedString128Bytes(skinName);
            Debug.Log($"[NetworkedCharacterState] Server set skin for client {OwnerClientId} to: {skinName}");
        }

        /// <summary>
        /// Owner: Fordert den Server auf, den Skin zu wechseln (z.B. In-Game Skin-Wechsel).
        /// </summary>
        [ServerRpc]
        public void RequestSkinChangeServerRpc(FixedString128Bytes skinName)
        {
            m_CurrentSkinName.Value = skinName;
            Debug.Log($"[NetworkedCharacterState] Server applied skin change for client {OwnerClientId}: {skinName}");
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

        private void OnCharacterNameValueChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
        {
            Debug.Log($"[NetworkedCharacterState] Name changed: {oldValue} → {newValue}");
            OnCharacterNameChanged?.Invoke(newValue.ToString());
        }

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

        private void OnWeaponNameValueChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
        {
            Debug.Log($"[NetworkedCharacterState] Weapon changed: {oldValue} → {newValue}");
            OnWeaponChanged?.Invoke(newValue.ToString());
        }

        /// <summary>
        /// Callback wenn sich die Waffen-Inventar-Liste aendert.
        /// </summary>
        private void OnWeaponInventoryListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            OnWeaponInventoryChanged?.Invoke();
        }

        // ===== Weapon Setters =====

        /// <summary>
        /// Server: Setzt die aktuelle Waffe (z.B. beim Spawn oder Waffen-Pickup).
        /// </summary>
        public void SetCurrentWeaponName(string weaponName)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedCharacterState] SetCurrentWeaponName can only be called on the server.");
                return;
            }

            m_CurrentWeaponName.Value = new FixedString64Bytes(weaponName);
            Debug.Log($"[NetworkedCharacterState] Server set weapon for client {OwnerClientId} to: {weaponName}");
        }

        /// <summary>
        /// Owner: Fordert den Server auf, die Waffe zu wechseln.
        /// Server kann hier spaeter Validierung einbauen (hat der Spieler die Waffe?).
        /// </summary>
        [ServerRpc]
        public void RequestWeaponChangeServerRpc(FixedString64Bytes weaponName)
        {
            m_CurrentWeaponName.Value = weaponName;
            Debug.Log($"[NetworkedCharacterState] Server applied weapon change for client {OwnerClientId}: {weaponName}");
        }

        // ===== Weapon Inventory =====

        /// <summary>
        /// Server: Fuegt eine Waffe zum Inventar hinzu (Duplikate werden ignoriert).
        /// </summary>
        public void AddWeapon(string weaponName)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedCharacterState] AddWeapon can only be called on the server.");
                return;
            }

            FixedString64Bytes fixedName = new(weaponName);

            for (int i = 0; i < m_WeaponInventory.Count; i++)
            {
                if (m_WeaponInventory[i] == fixedName)
                {
                    return;
                }
            }

            m_WeaponInventory.Add(fixedName);
            Debug.Log($"[NetworkedCharacterState] Server added weapon '{weaponName}' for client {OwnerClientId}. Inventory count: {m_WeaponInventory.Count}");
        }

        /// <summary>
        /// Liefert den Waffen-Namen am angegebenen Inventar-Index.
        /// </summary>
        public string GetWeaponAt(int index)
        {
            if (index < 0 || index >= m_WeaponInventory.Count)
            {
                return string.Empty;
            }

            return m_WeaponInventory[index].ToString();
        }

        /// <summary>
        /// Liefert den Animator-Index fuer eine Waffe (0 = knife, 1 = rpg7).
        /// Gibt 0 zurueck wenn die Waffe unbekannt ist.
        /// </summary>
        public static int GetWeaponAnimatorIndex(string weaponName)
        {
            if (s_WeaponAnimatorIndices.TryGetValue(weaponName, out int index))
            {
                return index;
            }

            return 0;
        }

        /// <summary>
        /// Owner: Fordert den Server auf, zur naechsten Waffe im Inventar zu wechseln.
        /// </summary>
        [ServerRpc]
        public void RequestNextWeaponServerRpc()
        {
            CycleWeapon(1);
        }

        /// <summary>
        /// Owner: Fordert den Server auf, zur vorherigen Waffe im Inventar zu wechseln.
        /// </summary>
        [ServerRpc]
        public void RequestPreviousWeaponServerRpc()
        {
            CycleWeapon(-1);
        }

        /// <summary>
        /// Server: Cycled die aktuelle Waffe um den angegebenen Offset (1 = naechste, -1 = vorherige).
        /// Wrap-around am Inventar-Ende/Anfang (wie SoF2 weapon cycling).
        /// </summary>
        private void CycleWeapon(int direction)
        {
            if (m_WeaponInventory.Count <= 1)
            {
                return;
            }

            FixedString64Bytes current = m_CurrentWeaponName.Value;
            int currentIndex = -1;
            for (int i = 0; i < m_WeaponInventory.Count; i++)
            {
                if (m_WeaponInventory[i] == current)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = (currentIndex + direction + m_WeaponInventory.Count) % m_WeaponInventory.Count;
            string nextWeapon = m_WeaponInventory[nextIndex].ToString();

            m_CurrentWeaponName.Value = new FixedString64Bytes(nextWeapon);
            Debug.Log($"[NetworkedCharacterState] Server cycled weapon for client {OwnerClientId}: {current} → {nextWeapon}");
        }
    }
}
