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
        private GameObject m_CurrentPlayerPrefabAsset; // Asset reference, not instance

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<PlayerSkinState> states = new() { m_Idle, m_Loading, m_Applied, m_Error };
            InitializeStates(states, m_Idle);

            Debug.Log("[PlayerSkinManager] Initialized");
        }

        void OnDestroy()
        {
            Debug.Log("[PlayerSkinManager] Destroyed");
        }

        public bool TryApplyAnimationSet(GameObject prefab, string animatorControllerPath)
        {
            SkinDefinitionLoader skinLoader = ServiceLocator.Get<SkinDefinitionLoader>();
            SkinDefinition skinDefinition = skinLoader.GetByName(m_CurrentSkinName);
            if (skinDefinition == null)
            {
                return false;
            }
            string modelName = skinDefinition.GetModelName();
            if (string.IsNullOrEmpty(modelName))
            {
                return false;
            }
            string animationSetName = skinLoader.GetAnimationSetNameForModelName(modelName);
            m_Applier.ApplyAnimatorController(prefab, $"models/animator/{animatorControllerPath}_{animationSetName}");
            return true;
        }

        internal bool TryLoadAndApplySkin(string skinName, out GameObject prefab)
        {
            prefab = null;

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

            // Wende Materialien direkt auf das Asset-Prefab an (nicht instanziieren)
            prefab = prefabAsset;
            m_Applier.ApplyAnimatorController(prefab, $"models/animator/loadout_{animationSetName}");
            m_Applier.ResetAllRenderersToActive(prefab);

            ServiceLocator.Get<TextureManager>().CreateMaterialsFromSkinDefinition(skinDefinition);
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
        internal void OnSkinLoadSuccess(string skinName, GameObject prefabAsset)
        {
            m_CurrentSkinName = skinName;
            m_CurrentPlayerPrefabAsset = prefabAsset; // Store asset reference
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

        internal GameObject GetCurrentPlayerPrefab()
        {
            return m_CurrentPlayerPrefabAsset;
        }
    }
}
