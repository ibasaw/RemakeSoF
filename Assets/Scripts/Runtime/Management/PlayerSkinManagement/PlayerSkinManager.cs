using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.Core;
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
        
        private PlayerSkinDataRegistry m_PlayerSkinDataRegistry;
        private PlayerSkinLoader m_Loader;
        private PlayerSkinApplier m_Applier;

        // Current skin data
        private string m_CurrentSkinName;
        private GameObject m_CurrentPlayerPrefab;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<PlayerSkinState> states = new() { m_Idle, m_Loading, m_Applied, m_Error };
            InitializeStates(states, m_Idle);

            m_PlayerSkinDataRegistry = new PlayerSkinDataRegistry();
            m_Loader = new PlayerSkinLoader(m_PlayerSkinDataRegistry);
            m_Applier = new PlayerSkinApplier(m_PlayerSkinDataRegistry);
            
            Debug.Log("[PlayerSkinManager] Initialized");
        }

        void OnDestroy()
        {
            Debug.Log("[PlayerSkinManager] Destroyed");
            m_PlayerSkinDataRegistry.ClearAllCaches();
        }

        internal bool TryLoadAndApplySkin(string skinName, string animatorName, out GameObject prefab)
        {
            prefab = null;

            if (string.IsNullOrEmpty(skinName))
            {
                OnSkinLoadFailure("Skin name is empty", PlayerSkinStatus.InvalidSkinName);
                return false;
            }

            SkinDefinition skinDefinition = m_Loader.GetSkinByName(skinName);
            if (skinDefinition == null)
            {
                OnSkinLoadFailure($"Skin definition file not found: {skinName}", PlayerSkinStatus.SkinNotFound);
                return false;
            }

            string modelName = skinDefinition.GetModelName();
            if (string.IsNullOrEmpty(modelName))
            {
                OnSkinLoadFailure($"No valid model name found in skin definition file: {skinName}", PlayerSkinStatus.ModelNameParseError);
                return false;
            }

            string prefabPath = $"characters/models/{modelName}";
            prefab = m_Loader.LoadPrefabForModel(prefabPath);
            if (prefab == null)
            {
                OnSkinLoadFailure($"Failed to load prefab for model: {modelName}", PlayerSkinStatus.PrefabNotFound);
                return false;
            }

            RuntimeAnimatorController controller = m_Loader.CreateAnimatorController($"models/animator/{animatorName}");
            if (!prefab.TryGetComponent<Animator>(out var animator))
            {
                animator = prefab.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;

            SetCurrentPlayerPrefab(prefab);
            m_Applier.ResetAllRenderersToActive(prefab);
            m_Applier.CreateMaterials(skinName, skinDefinition);
            m_Applier.DisableAndEnableSurfaces(prefab, skinDefinition);
            m_Applier.ApplyMaterialsToPrefab(prefab, skinName, skinDefinition);

            return true;
        }

        /// <summary>
        /// Load the next available skin in the registry 
        /// </summary>
        public void LoadNextSkin()
        {
            string nextName = m_PlayerSkinDataRegistry.GetNextSkinName(m_CurrentSkinName);
            if (!string.IsNullOrEmpty(nextName))
            {
                ChangeSkin(nextName, "loadout_preview");
            }
        }

        /// <summary>
        /// Load the previous available skin in the registry
        /// </summary>
        public void LoadPreviousSkin()
        {
            string prevName = m_PlayerSkinDataRegistry.GetPreviousSkinName(m_CurrentSkinName);
            if (!string.IsNullOrEmpty(prevName))
            {
                ChangeSkin(prevName, "loadout_preview");
            }
        }

        /// <summary>
        /// Request to change the player's skin
        /// </summary>
        public void ChangeSkin(string skinName, string animatorName)
        {
            if (m_CurrentState is IPlayerSkinChangeHandler handler)
            {
                handler.OnSkinChangeRequested(skinName, animatorName);
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
    }
}
