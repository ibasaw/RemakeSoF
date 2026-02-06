using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Orchestriert das Laden und Anwenden von Spieler-Skins via State Machine (Idle/Loading/Applied/Error),
    /// verwaltet aktuellen Skin/Prefab, delegiert an PlayerSkinLoader/PlayerSkinApplier 
    /// und broadcastet Status/Fehler.
    /// </summary>
    public class PlayerSkinManager : StateMachine<PlayerSkinState, PlayerSkinManager>
    {
        internal readonly PlayerSkinIdleState m_Idle = new();
        internal readonly PlayerSkinLoadingState m_Loading = new();
        internal readonly PlayerSkinAppliedState m_Applied = new();
        internal readonly PlayerSkinErrorState m_Error = new();

        // Internal service - pure asset application
        private readonly PlayerSkinApplier m_Applier = new();

        // Current skin data
        private string m_CurrentSkinName;
        private GameObject m_CurrentPlayerPrefab; // Runtime instance of prefab

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<PlayerSkinState> states = new() { m_Idle, m_Loading, m_Applied, m_Error };
            InitializeStates(states, m_Idle);

            Debug.Log("[PlayerSkinManager] Initialized");
        }

        void OnDestroy()
        {
            CleanupCurrentInstance();
            Debug.Log("[PlayerSkinManager] Destroyed");
        }

        internal bool TryLoadAndApplySkin(string skinName, out GameObject prefab)
        {
            prefab = null;
            CleanupCurrentInstance();

            if (string.IsNullOrEmpty(skinName))
            {
                OnSkinLoadFailure("Skin name is empty", PlayerSkinStatus.InvalidSkinName);
                return false;
            }

            SkinDefinitionLoader skinLoader = ServiceLocator.Get<SkinDefinitionLoader>();
            SkinDefinition skinDefinition = skinLoader.GetByName(skinName);
            if (skinDefinition == null)
            {
                OnSkinLoadFailure($"Skin definition file not found: {skinName}", PlayerSkinStatus.SkinNotFound);
                return false;
            }
            if (skinDefinition?.materials == null || skinDefinition.materials.Count == 0)
            {
                Debug.LogError($"[PlayerSkinApplier] Keine 'materials' in skinDefinition JSON vorhanden: {skinName}");
                //OnSkinLoadFailure($"Keine 'materials' in skinDefinition JSON vorhanden: {skinName}", PlayerSkinStatus.SkinDataParseError);
                //TODO: anatoli - state machine auf failure setzen wenn nix vorhanden?
                return false;
            }
            CharacterTemplateLoader templateLoader = ServiceLocator.Get<CharacterTemplateLoader>();
            List<CharacterTemplate> characterTemplates = templateLoader.GetBySkinName(skinName);

            string modelName = skinDefinition.GetModelName();
            if (string.IsNullOrEmpty(modelName))
            {
                OnSkinLoadFailure($"No valid model name found in skin definition file: {skinName}", PlayerSkinStatus.ModelNameParseError);
                return false;
            }
            string animationSetName = skinLoader.GetAnimationSetNameForModelName(modelName);

            string prefabPath = $"characters/models/{modelName}";
            GameObject prefabAsset = ServiceLocator.Get<PrefabManager>().LoadPrefab<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                OnSkinLoadFailure($"Failed to load prefab for model: {modelName}", PlayerSkinStatus.PrefabNotFound);
                return false;
            }

            // Clone Asset für Modifikationen (nicht das Original bearbeiten)
            prefab = CreatePrefabInstance(prefabAsset);
            SetCurrentPlayerPrefab(prefab);
            m_Applier.ApplyAnimatorController(prefab, $"models/animator/loadout_{animationSetName}");
            m_Applier.ResetAllRenderersToActive(prefab);

            LegacyShaderLoader shaderLoader = ServiceLocator.Get<LegacyShaderLoader>();
            Dictionary<string, Dictionary<string, ShaderEntry>> allShaderDefinitions = shaderLoader.GetAll();
            ServiceLocator.Get<TextureManager>().CreateMaterialsFromSkinDefinition(allShaderDefinitions, skinDefinition);
            m_Applier.DisableAndEnableSurfaces(prefab, skinDefinition);
            m_Applier.DisableAndEnableSurfaces(prefab, characterTemplates, skinName);
            
            // Manager holt konkrete Daten und übergibt sie
            SurfaceDefinitionLoader surfaceLoader = ServiceLocator.Get<SurfaceDefinitionLoader>();
            SkinSurfaceDefinition defaultSurface = surfaceLoader.GetByModelName("default");
            SkinSurfaceDefinition modelSurface = surfaceLoader.GetByModelName(modelName);
            if (defaultSurface != null)
            {
                m_Applier.ApplySurfaceDefinitions(prefab, skinName, "default", skinDefinition, defaultSurface);
            }
            if (modelSurface != null)
            {
                m_Applier.ApplySurfaceDefinitions(prefab, skinName, modelName, skinDefinition, modelSurface);
            }
            return true;
        }

        /// <summary>
        /// Load the next available skin in the registry 
        /// </summary>
        public void LoadNextSkin()
        {
            SkinDefinitionLoader skinLoader = ServiceLocator.Get<SkinDefinitionLoader>();
            string nextName = skinLoader.GetNextSkinName(m_CurrentSkinName);
            if (!string.IsNullOrEmpty(nextName))
            {
                ChangeSkin(nextName);
            }
        }

        /// <summary>
        /// Load the previous available skin in the registry
        /// </summary>
        public void LoadPreviousSkin()
        {
            SkinDefinitionLoader skinLoader = ServiceLocator.Get<SkinDefinitionLoader>();
            string prevName = skinLoader.GetPreviousSkinName(m_CurrentSkinName);
            if (!string.IsNullOrEmpty(prevName))
            {
                ChangeSkin(prevName);
            }
        }

        /// <summary>
        /// Request to change the player's skin
        /// </summary>
        public void ChangeSkin(string skinName)
        {
            if (m_CurrentState is IPlayerSkinChangeHandler handler)
            {
                handler.OnSkinChangeRequested(skinName);
            }
        }

        /// <summary>
        /// Internal: Signal failed skin load (called by states)
        /// </summary>
        internal void OnSkinLoadFailure(string message, PlayerSkinStatus status)
        {
            Debug.LogError($"[PlayerSkinManager] {status}: {message}");
            EventManager.Broadcast(new PlayerSkinErrorEvent { error = message, status = status });
            ChangeState(m_Error);
        }

        /// <summary>
        /// Internal: Signal successful skin load (called by states)
        /// And update current client skin data
        /// </summary>
        internal void OnSkinLoadSuccess(string skinName, GameObject prefab)
        {
            m_CurrentSkinName = skinName;
            m_CurrentPlayerPrefab = prefab;
            m_CurrentState.OnSkinLoadSuccess();
        }

        /// <summary>
        /// Signal successful skin application and broadcast event to listeners
        /// Called when skin is fully applied and ready
        /// </summary>
        internal void OnSkinApplied()
        {
            Debug.Log($"[PlayerSkinManager] Applied skin: {m_CurrentSkinName}");
            EventManager.Broadcast(new PlayerSkinChangedEvent
            {
                skinName = m_CurrentSkinName
            });
        }

        internal void SetCurrentPlayerPrefab(GameObject prefab)
        {
            m_CurrentPlayerPrefab = prefab;
        }

        internal GameObject GetCurrentPlayerPrefab()
        {
            return m_CurrentPlayerPrefab;
        }

        /// <summary>
        /// Erstellt eine neue Instanz des Prefab-Assets und räumt die alte Instanz auf
        /// </summary>
        private GameObject CreatePrefabInstance(GameObject prefabAsset)
        {
            CleanupCurrentInstance();
            GameObject instance = Instantiate(prefabAsset);
            //instance.SetActive(false); // Nicht sofort sichtbar TODO: anatoli - evtl. inaktiv lassen bis alles angewendet ist?
            Debug.Log($"[PlayerSkinManager] Created new prefab instance: {instance.name}");
            return instance;
        }

        /// <summary>
        /// Räumt die aktuelle Prefab-Instanz auf
        /// </summary>
        private void CleanupCurrentInstance()
        {
            if (m_CurrentPlayerPrefab != null)
            {
                Debug.Log($"[PlayerSkinManager] Cleaning up old prefab instance: {m_CurrentPlayerPrefab.name}");
                Destroy(m_CurrentPlayerPrefab);
                m_CurrentPlayerPrefab = null;
            }
        }
    }
}
