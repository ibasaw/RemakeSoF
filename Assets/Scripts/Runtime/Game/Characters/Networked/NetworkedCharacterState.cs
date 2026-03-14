using System;
using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
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

        // ===== Ammo =====

        /// <summary>
        /// Aktuelle Munition im Magazin der aktuellen Waffe.
        /// Server-autoritativ, Clients lesen fuer Animator und HUD.
        /// </summary>
        private readonly NetworkVariable<int> m_CurrentClipAmmo = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Reserve-Munition der aktuellen Waffe (nicht im Magazin).
        /// Server-autoritativ, Clients lesen fuer HUD.
        /// </summary>
        private readonly NetworkVariable<int> m_ReserveAmmo = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Aktuelle Alt-Attack Munition im Magazin (z.B. M203 Granate am M4).
        /// Server-autoritativ, Clients lesen fuer HUD.
        /// </summary>
        private readonly NetworkVariable<int> m_AltClipAmmo = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Reserve-Munition fuer Alt-Attack (z.B. zusaetzliche M203 Granaten).
        /// Server-autoritativ, Clients lesen fuer HUD.
        /// </summary>
        private readonly NetworkVariable<int> m_AltReserveAmmo = new(
            0,
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
        /// Server-seitiger Ammo-Cache: speichert Clip/Reserve + AltClip/AltReserve pro Waffe beim Waffenwechsel.
        /// Wird nicht synchronisiert — Server ist Single Source of Truth fuer Ammo.
        /// </summary>
        private readonly Dictionary<string, (int clip, int reserve, int altClip, int altReserve)> m_AmmoCache = new();

        /// <summary>
        /// Server: Zielwaffe des laufenden Swaps. Wird von CycleWeapon genutzt um bei
        /// erneutem Cycling waehrend eines Swaps von der Zielwaffe weiterzuzaehlen.
        /// Wird beim Waffenwechsel-Commit (OnWeaponNameValueChanged) zurueckgesetzt.
        /// </summary>
        private string m_PendingSwapTarget;



        // ===== Public Properties =====

        public string CharacterName => m_CharacterName.Value.ToString();
        public bool IsAlive => m_IsAlive.Value;
        public int Health => m_Health.Value;
        public uint TeamId => m_TeamId.Value;
        public int Kills => m_Kills.Value;
        public int Deaths => m_Deaths.Value;
        public string CurrentSkinName => m_CurrentSkinName.Value.ToString();
        public string CurrentWeaponName => m_CurrentWeaponName.Value.ToString();
        public int CurrentClipAmmo => m_CurrentClipAmmo.Value;
        public int ReserveAmmo => m_ReserveAmmo.Value;
        public int AltClipAmmo => m_AltClipAmmo.Value;
        public int AltReserveAmmo => m_AltReserveAmmo.Value;
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

        /// <summary>
        /// Server-Event: Wird gefeuert wenn ein Waffenwechsel angefordert wird (statt direktem Setzen).
        /// NetworkedPlayerCharacter abonniert dieses Event um den Swap-Prozess (Drop/Raise) zu steuern.
        /// </summary>
        public event System.Action<string> OnWeaponSwapRequested;

        /// <summary>
        /// Event: Munition hat sich geaendert (clipAmmo, reserveAmmo).
        /// </summary>
        public event System.Action<int, int> OnAmmoChanged;

        /// <summary>
        /// Event: Alt-Attack Munition hat sich geaendert (altClipAmmo, altReserveAmmo).
        /// </summary>
        public event System.Action<int, int> OnAltAmmoChanged;

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
            m_CurrentClipAmmo.OnValueChanged += OnClipAmmoValueChanged;
            m_ReserveAmmo.OnValueChanged += OnReserveAmmoValueChanged;
            m_AltClipAmmo.OnValueChanged += OnAltClipAmmoValueChanged;
            m_AltReserveAmmo.OnValueChanged += OnAltReserveAmmoValueChanged;
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

        /// <summary>
        /// Feuert alle aktuellen Werte als Events, damit spaet subscribende Listener
        /// (z.B. MatchController) den aktuellen State erhalten.
        /// NGO OnValueChanged feuert nicht fuer initiale Werte.
        /// </summary>
        public void NotifyCurrentState()
        {
            string weaponName = CurrentWeaponName;
            if (!string.IsNullOrEmpty(weaponName))
            {
                OnWeaponChanged?.Invoke(weaponName);
                OnAmmoChanged?.Invoke(CurrentClipAmmo, ReserveAmmo);
                OnAltAmmoChanged?.Invoke(AltClipAmmo, AltReserveAmmo);
            }

            OnHealthChanged?.Invoke(Health);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            m_CharacterName.OnValueChanged -= OnCharacterNameValueChanged;
            m_Health.OnValueChanged -= OnHealthValueChanged;
            m_IsAlive.OnValueChanged -= OnIsAliveValueChanged;
            m_CurrentSkinName.OnValueChanged -= OnSkinNameValueChanged;
            m_CurrentWeaponName.OnValueChanged -= OnWeaponNameValueChanged;
            m_CurrentClipAmmo.OnValueChanged -= OnClipAmmoValueChanged;
            m_ReserveAmmo.OnValueChanged -= OnReserveAmmoValueChanged;
            m_AltClipAmmo.OnValueChanged -= OnAltClipAmmoValueChanged;
            m_AltReserveAmmo.OnValueChanged -= OnAltReserveAmmoValueChanged;
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

            // Server: Aktuelle Ammo der alten Waffe cachen, neue Waffe laden/initialisieren
            if (IsServer)
            {
                string oldWeapon = oldValue.ToString();
                if (!string.IsNullOrEmpty(oldWeapon))
                {
                    m_AmmoCache[oldWeapon] = (m_CurrentClipAmmo.Value, m_ReserveAmmo.Value, m_AltClipAmmo.Value, m_AltReserveAmmo.Value);
                }

                // Pending-Target zuruecksetzen wenn committed Waffe == Ziel
                if (!string.IsNullOrEmpty(m_PendingSwapTarget) &&
                    string.Equals(m_PendingSwapTarget, newValue.ToString(), StringComparison.Ordinal))
                {
                    m_PendingSwapTarget = null;
                }

                if (newValue.Length > 0)
                {
                    InitializeAmmoForCurrentWeapon();
                }
            }

            OnWeaponChanged?.Invoke(newValue.ToString());
        }

        /// <summary>
        /// Callback wenn sich die Clip-Munition aendert.
        /// </summary>
        private void OnClipAmmoValueChanged(int oldValue, int newValue)
        {
            OnAmmoChanged?.Invoke(newValue, m_ReserveAmmo.Value);
        }

        /// <summary>
        /// Callback wenn sich die Reserve-Munition aendert.
        /// </summary>
        private void OnReserveAmmoValueChanged(int oldValue, int newValue)
        {
            OnAmmoChanged?.Invoke(m_CurrentClipAmmo.Value, newValue);
        }

        /// <summary>
        /// Callback wenn sich die Alt-Clip-Munition aendert.
        /// </summary>
        private void OnAltClipAmmoValueChanged(int oldValue, int newValue)
        {
            OnAltAmmoChanged?.Invoke(newValue, m_AltReserveAmmo.Value);
        }

        /// <summary>
        /// Callback wenn sich die Alt-Reserve-Munition aendert.
        /// </summary>
        private void OnAltReserveAmmoValueChanged(int oldValue, int newValue)
        {
            OnAltAmmoChanged?.Invoke(m_AltClipAmmo.Value, newValue);
        }

        /// <summary>
        /// Callback wenn sich die Waffen-Inventar-Liste aendert.
        /// </summary>
        private void OnWeaponInventoryListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            OnWeaponInventoryChanged?.Invoke();
        }

        // ===== Ammo Methods =====

        /// <summary>
        /// Server: Initialisiert Clip- und Reserve-Munition fuer die aktuelle Waffe.
        /// Laedt aus dem AmmoCache wenn vorhanden (Per-Weapon Persistenz),
        /// sonst aus dem WeaponDataLoader (StartClip / StartReserve).
        /// </summary>
        private void InitializeAmmoForCurrentWeapon()
        {
            string weaponName = CurrentWeaponName;

            // Cache-Hit: Gespeicherte Ammo-Werte wiederherstellen
            if (m_AmmoCache.TryGetValue(weaponName, out (int clip, int reserve, int altClip, int altReserve) cached))
            {
                m_CurrentClipAmmo.Value = cached.clip;
                m_ReserveAmmo.Value = cached.reserve;
                m_AltClipAmmo.Value = cached.altClip;
                m_AltReserveAmmo.Value = cached.altReserve;
                Debug.Log($"[NetworkedCharacterState] Ammo restored from cache for '{weaponName}': Clip={cached.clip}, Reserve={cached.reserve}, AltClip={cached.altClip}, AltReserve={cached.altReserve}");
                return;
            }

            // Cache-Miss: Aus WeaponDataLoader initialisieren (erster Equip)
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(weaponName);
            if (weapon?.Ammo == null)
            {
                m_CurrentClipAmmo.Value = 0;
                m_ReserveAmmo.Value = 0;
                m_AltClipAmmo.Value = 0;
                m_AltReserveAmmo.Value = 0;
                return;
            }

            m_CurrentClipAmmo.Value = weapon.Ammo.StartClip;
            m_ReserveAmmo.Value = weapon.Ammo.StartReserve;

            // Alt-Attack Munition initialisieren (z.B. M4 M203)
            if (weapon.AltAttack?.Ammo != null)
            {
                m_AltClipAmmo.Value = weapon.AltAttack.Ammo.StartClip;
                m_AltReserveAmmo.Value = weapon.AltAttack.Ammo.StartReserve;
            }
            else
            {
                m_AltClipAmmo.Value = 0;
                m_AltReserveAmmo.Value = 0;
            }

            Debug.Log($"[NetworkedCharacterState] Ammo initialized for '{weaponName}': Clip={weapon.Ammo.StartClip}, Reserve={weapon.Ammo.StartReserve}, Infinite={weapon.Ammo.Infinite}, AltClip={m_AltClipAmmo.Value}, AltReserve={m_AltReserveAmmo.Value}");
        }

        /// <summary>
        /// Server: Versucht einen Schuss Munition zu verbrauchen.
        /// Gibt true zurueck wenn der Angriff erlaubt ist (Infinite oder Clip > 0).
        /// Bei nicht-infinite Waffen wird ClipAmmo um 1 reduziert.
        /// </summary>
        public bool TryConsumeAmmo()
        {
            if (!IsServer)
            {
                return false;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.Ammo != null && weapon.Ammo.Infinite)
            {
                return true;
            }

            if (m_CurrentClipAmmo.Value <= 0)
            {
                return false;
            }

            m_CurrentClipAmmo.Value--;
            return true;
        }

        /// <summary>
        /// Server: Versucht Alt-Attack Munition zu verbrauchen.
        /// Unterstuetzte Patterns:
        /// - Melee-AltAttack (z.B. AK74 Bayonet): immer erlaubt, kein Verbrauch.
        /// - Separate Alt-Ammo (z.B. M4 M203): verbraucht aus AltClipAmmo.
        /// - Projektil ohne eigene Ammo (z.B. Knife-Throw): verbraucht aus ReserveAmmo.
        /// </summary>
        public bool TryConsumeAltAmmo()
        {
            if (!IsServer)
            {
                return false;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.AltAttack == null)
            {
                return false;
            }

            // Melee-AltAttack (Bayonet): immer erlaubt
            if (!string.IsNullOrEmpty(weapon.AltAttack.Melee))
            {
                return true;
            }

            // Separate Alt-Ammo (M4 M203): aus eigenem Magazin verbrauchen
            if (weapon.AltAttack.Ammo != null)
            {
                if (m_AltClipAmmo.Value <= 0)
                {
                    return false;
                }

                m_AltClipAmmo.Value--;
                return true;
            }

            // Projektil ohne eigene Ammo (Knife-Throw): aus Weapon-Reserve verbrauchen
            if (weapon.AltAttack.Projectile != null)
            {
                if (m_ReserveAmmo.Value <= 0)
                {
                    return false;
                }

                m_ReserveAmmo.Value--;
                return true;
            }

            // Fallback: erlaubt (z.B. AltAttack ohne Ammo/Projectile/Melee)
            return true;
        }

        /// <summary>
        /// Server: Prueft ob ein Alt-Reload gestartet werden kann.
        /// Nur relevant fuer Waffen mit separater Alt-Ammo (z.B. M4 M203).
        /// </summary>
        public bool CanAltReload()
        {
            if (!IsServer)
            {
                return false;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.AltAttack?.Ammo == null)
            {
                return false;
            }

            if (m_AltClipAmmo.Value >= weapon.AltAttack.Ammo.MaxClip)
            {
                return false;
            }

            return m_AltReserveAmmo.Value > 0;
        }

        /// <summary>
        /// Server: Fuehrt den Alt-Reload durch — transferiert Munition von Alt-Reserve in Alt-Clip.
        /// </summary>
        public void CompleteAltReload()
        {
            if (!IsServer)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.AltAttack?.Ammo == null)
            {
                return;
            }

            int needed = weapon.AltAttack.Ammo.MaxClip - m_AltClipAmmo.Value;
            int transfer = Mathf.Min(needed, m_AltReserveAmmo.Value);

            m_AltClipAmmo.Value += transfer;
            m_AltReserveAmmo.Value -= transfer;

            Debug.Log($"[NetworkedCharacterState] Alt-Reload complete for '{CurrentWeaponName}': AltClip={m_AltClipAmmo.Value}, AltReserve={m_AltReserveAmmo.Value}");
        }

        /// <summary>
        /// Server: Prueft ob ein Reload gestartet werden kann.
        /// Gibt true zurueck wenn: Waffe nicht infinite, Clip nicht voll, Reserve > 0.
        /// </summary>
        public bool CanReload()
        {
            if (!IsServer)
            {
                return false;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.Ammo == null || weapon.Ammo.Infinite)
            {
                return false;
            }

            if (m_CurrentClipAmmo.Value >= weapon.Ammo.MaxClip)
            {
                return false;
            }

            return m_ReserveAmmo.Value > 0;
        }

        /// <summary>
        /// Server: Fuehrt den Reload durch — transferiert Munition von Reserve in Clip.
        /// Berechnet benoetigte Munition (MaxClip - aktuelles Clip), begrenzt auf verfuegbare Reserve.
        /// </summary>
        public void CompleteReload()
        {
            if (!IsServer)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.Ammo == null || weapon.Ammo.Infinite)
            {
                return;
            }

            int needed = weapon.Ammo.MaxClip - m_CurrentClipAmmo.Value;
            int transfer = Mathf.Min(needed, m_ReserveAmmo.Value);

            m_CurrentClipAmmo.Value += transfer;
            m_ReserveAmmo.Value -= transfer;

            Debug.Log($"[NetworkedCharacterState] Reload complete for '{CurrentWeaponName}': Clip={m_CurrentClipAmmo.Value}, Reserve={m_ReserveAmmo.Value}");
        }

        /// <summary>
        /// Server: Transferiert genau eine Shell von Reserve in Clip (Shell-by-Shell Reload).
        /// </summary>
        public void TransferOneShell()
        {
            if (!IsServer)
            {
                return;
            }

            if (m_ReserveAmmo.Value <= 0)
            {
                return;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(CurrentWeaponName);

            if (weapon?.Ammo == null || m_CurrentClipAmmo.Value >= weapon.Ammo.MaxClip)
            {
                return;
            }

            m_CurrentClipAmmo.Value += 1;
            m_ReserveAmmo.Value -= 1;
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
            string target = weaponName.ToString();
            OnWeaponSwapRequested?.Invoke(target);
            Debug.Log($"[NetworkedCharacterState] Server weapon swap requested for client {OwnerClientId}: {target}");
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
        /// Liefert den Animator-Index fuer eine Waffe aus den geladenen Waffendaten.
        /// Gibt 0 zurueck wenn die Waffe unbekannt ist.
        /// </summary>
        public static int GetWeaponAnimatorIndex(string weaponName)
        {
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader != null)
            {
                return loader.GetAnimatorIndex(weaponName);
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

            // Bei laufendem Swap: von der Zielwaffe weiter cyclen, nicht von der committed Waffe
            FixedString64Bytes baseWeapon = !string.IsNullOrEmpty(m_PendingSwapTarget)
                ? new FixedString64Bytes(m_PendingSwapTarget)
                : m_CurrentWeaponName.Value;

            int currentIndex = -1;
            for (int i = 0; i < m_WeaponInventory.Count; i++)
            {
                if (m_WeaponInventory[i] == baseWeapon)
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

            m_PendingSwapTarget = nextWeapon;
            OnWeaponSwapRequested?.Invoke(nextWeapon);
            Debug.Log($"[NetworkedCharacterState] Server weapon swap requested for client {OwnerClientId}: {baseWeapon} → {nextWeapon}");
        }
    }
}
