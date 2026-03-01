using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Client-seitige Komponente auf dem Player-Prefab.
    /// Beobachtet <see cref="NetworkedCharacterState.OnSkinChanged"/> und lädt/instanziiert
    /// das Skin-Visual lokal als Child unter <see cref="m_VisualRoot"/>.
    /// Funktioniert für Owner UND Remote-Clients (jeder sieht jeden Skin).
    /// Server ignoriert diese Komponente.
    /// </summary>
    [RequireComponent(typeof(NetworkedCharacterState))]
    public class ClientCharacterSkinHandler : MonoBehaviour
    {
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        [SerializeField]
        private NetworkedCharacter m_NetworkedCharacter;

        /// <summary>
        /// Transform unter dem das Skin-Visual instanziiert wird.
        /// Muss als Child auf dem Player-Prefab existieren.
        /// </summary>
        [SerializeField]
        private Transform m_VisualRoot;

        /// <summary>
        /// Event das nach erfolgreicher Visual-Instanziierung gefeuert wird.
        /// Parameter: das instanziierte Visual-GameObject.
        /// </summary>
        public event Action<GameObject> OnVisualInstantiated;

        /// <summary>
        /// Aktuell instanziiertes Visual-GameObject.
        /// </summary>
        private GameObject m_CurrentVisualInstance;

        /// <summary>
        /// Zuletzt geladener Skin-Name (idempotent-Guard).
        /// </summary>
        private string m_LoadedSkinName;

        private void Awake()
        {
            m_NetworkedCharacter.OnNetworkSpawnHook += OnNetworkSpawn;
            m_NetworkedCharacter.OnNetworkDespawnHook += OnNetworkDespawn;
        }

        private void OnDestroy()
        {
            m_NetworkedCharacter.OnNetworkSpawnHook -= OnNetworkSpawn;
            m_NetworkedCharacter.OnNetworkDespawnHook -= OnNetworkDespawn;
        }

        /// <summary>
        /// Bei Spawn: initialen Skin laden (falls bereits gesetzt) und auf Änderungen lauschen.
        /// Server braucht keine Visuals.
        /// </summary>
        private void OnNetworkSpawn()
        {
            if (m_NetworkedCharacter.IsServer && !m_NetworkedCharacter.IsHost)
            {
                // Dedicated Server: keine Visuals nötig
                enabled = false;
                return;
            }

            // Auf Skin-Änderungen reagieren
            m_CharacterState.OnSkinChanged += OnSkinChanged;

            // Initialen Skin laden, falls bereits ein Name vorhanden ist
            string skinName = m_CharacterState.CurrentSkinName;
            if (!string.IsNullOrEmpty(skinName))
            {
                LoadAndApplySkin(skinName);
            }
        }

        /// <summary>
        /// Bei Despawn: Listener entfernen und Visual aufräumen.
        /// </summary>
        private void OnNetworkDespawn()
        {
            m_CharacterState.OnSkinChanged -= OnSkinChanged;
            ClearCurrentVisual();
        }

        /// <summary>
        /// Callback bei Skin-Änderung via <see cref="NetworkedCharacterState.OnSkinChanged"/>.
        /// </summary>
        private void OnSkinChanged(string skinName)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                return;
            }

            LoadAndApplySkin(skinName);
        }

        /// <summary>
        /// Lädt den Skin und instanziiert das Visual als Child unter <see cref="m_VisualRoot"/>.
        /// Owner-Optimierung: nutzt das bereits im <see cref="PlayerSkinManager"/> gecachte Asset,
        /// falls der angeforderte Skin dem aktuell geladenen entspricht.
        /// Remote-Clients: laden den Skin immer via <see cref="PlayerSkinManager.TryLoadAndApplySkin"/>.
        /// Idempotent: lädt nicht erneut, wenn der selbe Skin bereits aktiv ist.
        /// </summary>
        private void LoadAndApplySkin(string skinName)
        {
            // Idempotent-Guard: nicht doppelt laden
            if (skinName == m_LoadedSkinName)
            {
                return;
            }

            PlayerSkinManager skinManager = ApplicationEntryPoint.Singleton.PlayerSkinManager;
            if (skinManager == null)
            {
                Debug.LogWarning("[ClientCharacterSkinHandler] PlayerSkinManager not available.");
                return;
            }

            GameObject prefabAsset = null;

            // Remote: Skin frisch laden
            if (prefabAsset == null)
            {
                bool success = skinManager.TryLoadAndApplySkin(skinName, out prefabAsset);
                if (!success || prefabAsset == null)
                {
                    Debug.LogWarning($"[ClientCharacterSkinHandler] Failed to load skin: {skinName}");
                    return;
                }
            }
            skinManager.TryApplyAnimationSet(prefabAsset, "game");

            // Altes Visual entfernen
            ClearCurrentVisual();

            // Neues Visual unter VisualRoot instanziieren
            m_CurrentVisualInstance = Instantiate(prefabAsset, m_VisualRoot);
            m_CurrentVisualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            // m_CurrentVisualInstance.transform.localScale = Vector3.one;

            m_LoadedSkinName = skinName;

            Debug.Log($"[ClientCharacterSkinHandler] Skin applied: {skinName} (Owner={m_NetworkedCharacter.IsOwner}) on Character {m_NetworkedCharacter.CharacterId}");

            // Listener benachrichtigen (z.B. ClientPlayerCharacter für Kamera-Targets)
            OnVisualInstantiated?.Invoke(m_CurrentVisualInstance);
        }

        /// <summary>
        /// Entfernt das aktuell instanziierte Visual.
        /// </summary>
        private void ClearCurrentVisual()
        {
            if (m_CurrentVisualInstance != null)
            {
                Destroy(m_CurrentVisualInstance);
                m_CurrentVisualInstance = null;
                m_LoadedSkinName = null;
            }
        }
    }
}
