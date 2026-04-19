using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
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
        /// Zugriff auf das aktuell instanziierte Visual-GameObject.
        /// Wird benoetigt damit spaete Subscriber pruefen koennen ob das Visual
        /// bereits vor ihrer Subscription instanziiert wurde (Race-Condition-Guard).
        /// </summary>
        public GameObject CurrentVisualInstance => m_CurrentVisualInstance;

        /// <summary>
        /// Zuletzt geladener Skin-Name (idempotent-Guard).
        /// </summary>
        private string m_LoadedSkinName;

        /// <summary>
        /// True wenn auf dem Dedicated Server (kein Host). Skeleton wird ohne Renderer geladen.
        /// </summary>
        private bool m_IsServerMode;

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
                // Dedicated Server: Skeleton ohne Rendering laden fuer Hitbox-Collider
                m_IsServerMode = true;
                m_CharacterState.OnSkinChanged += OnSkinChanged;

                string skinName = m_CharacterState.CurrentSkinName;
                if (!string.IsNullOrEmpty(skinName))
                {
                    LoadServerSkeleton(skinName);
                }
                return;
            }

            // Auf Skin-Änderungen reagieren
            m_CharacterState.OnSkinChanged += OnSkinChanged;

            // Initialen Skin laden, falls bereits ein Name vorhanden ist
            string skinName2 = m_CharacterState.CurrentSkinName;
            if (!string.IsNullOrEmpty(skinName2))
            {
                LoadAndApplySkin(skinName2);
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

            if (m_IsServerMode)
            {
                LoadServerSkeleton(skinName);
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
            Debug.Log($"[ClientCharacterSkinHandler] Networked Character State: Name {m_CharacterState.CharacterName}, Skin {m_CharacterState.CurrentSkinName}, Health {m_CharacterState.Health}");

            // Listener benachrichtigen (z.B. ClientPlayerCharacter für Kamera-Targets)
            OnVisualInstantiated?.Invoke(m_CurrentVisualInstance);
        }

        /// <summary>
        /// Laedt auf dem Dedicated Server nur das Skeleton (ohne Renderer) fuer Hitbox-Erstellung.
        /// Nutzt SkinDefinitionLoader → modelName → PrefabManager direkt, ohne PlayerSkinManager.
        /// </summary>
        private void LoadServerSkeleton(string skinName)
        {
            if (skinName == m_LoadedSkinName)
            {
                return;
            }

            SkinDefinitionLoader skinLoader = ServiceLocator.Get<SkinDefinitionLoader>();
            if (skinLoader == null)
            {
                Debug.LogError("[ClientCharacterSkinHandler] SkinDefinitionLoader nicht auf Server registriert — keine Hitboxen moeglich.");
                return;
            }

            SkinDefinition skinDef = skinLoader.GetByName(skinName);
            if (skinDef == null)
            {
                Debug.LogError($"[ClientCharacterSkinHandler] Skin-Definition nicht gefunden: {skinName}");
                return;
            }

            string modelName = skinDef.GetModelName();
            if (string.IsNullOrEmpty(modelName))
            {
                Debug.LogError($"[ClientCharacterSkinHandler] Kein Model-Name in Skin-Definition: {skinName}");
                return;
            }

            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogError("[ClientCharacterSkinHandler] PrefabManager nicht verfuegbar auf Server.");
                return;
            }

            string prefabPath = $"characters/models/{modelName}";
            GameObject prefabAsset = prefabManager.LoadPrefab<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[ClientCharacterSkinHandler] Server-Skeleton-Prefab nicht gefunden: {prefabPath}");
                return;
            }

            ClearCurrentVisual();

            m_CurrentVisualInstance = UnityEngine.Object.Instantiate(prefabAsset, m_VisualRoot);
            m_CurrentVisualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Alle visuellen Komponenten entfernen — Server braucht nur Bone-Hierarchie fuer Hitboxen
            foreach (Renderer renderer in m_CurrentVisualInstance.GetComponentsInChildren<Renderer>(true))
            {
                UnityEngine.Object.Destroy(renderer);
            }
            foreach (ParticleSystem particleSystem in m_CurrentVisualInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                UnityEngine.Object.Destroy(particleSystem);
            }

            // AnimatorController laden damit Server Bones korrekt animiert (Hitbox-Tracking)
            string animationSetName = skinLoader.GetAnimationSetNameForModelName(modelName);
            if (!string.IsNullOrEmpty(animationSetName))
            {
                string animatorPath = $"models/animator/game_{animationSetName}";
                RuntimeAnimatorController baseController = prefabManager.LoadPrefab<RuntimeAnimatorController>(animatorPath);
                if (baseController != null)
                {
                    Animator animator = m_CurrentVisualInstance.GetComponentInChildren<Animator>();
                    if (animator == null)
                    {
                        animator = m_CurrentVisualInstance.AddComponent<Animator>();
                    }
                    AnimatorOverrideController overrideController = new();
                    overrideController.runtimeAnimatorController = baseController;
                    animator.runtimeAnimatorController = overrideController;
                    Debug.Log($"[ClientCharacterSkinHandler] Server-AnimatorController geladen: {animatorPath}");
                }
                else
                {
                    Debug.LogWarning($"[ClientCharacterSkinHandler] AnimatorController nicht gefunden: {animatorPath}");
                }
            }

            m_LoadedSkinName = skinName;

            Debug.Log($"[ClientCharacterSkinHandler] Server-Skeleton geladen fuer Hitboxen: {skinName} (Model={modelName})");

            OnVisualInstantiated?.Invoke(m_CurrentVisualInstance);
        }

        /// <summary>
        /// Setzt das Visual zurueck und laedt den Skin neu.
        /// Wird bei Respawn aufgerufen um alle Gore-Surfaces wiederherzustellen.
        /// </summary>
        public void ForceReloadSkin()
        {
            string skinName = m_CharacterState != null ? m_CharacterState.CurrentSkinName : null;
            if (string.IsNullOrEmpty(skinName))
            {
                return;
            }

            // Altes Visual zerstoeren (inklusive aller Gore-Chunks/BoltOns)
            ClearCurrentVisual();

            if (m_IsServerMode)
            {
                LoadServerSkeleton(skinName);
            }
            else
            {
                LoadAndApplySkin(skinName);
            }
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
