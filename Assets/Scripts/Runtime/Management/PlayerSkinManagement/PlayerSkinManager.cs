using System;
using System.Collections.Generic;
using System.IO;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Manages player skin selection and application across game and metagame scenes.
    /// Uses state machine pattern to handle different skin loading states.
    /// Zustandsverwaltung (runtime state)
    /// Welcher Skin ist aktuell aktiv
    /// State Machine (Idle/Loading/Applied/Error)
    /// Event-Broadcasting bei Änderungen
    /// Zustandsbehaftet – tracked was gerade läuft
    /// </summary>
    public class PlayerSkinManager : StateMachine<PlayerSkinState, PlayerSkinManager>
    {
        internal readonly PlayerSkinIdleState m_Idle = new();
        internal readonly PlayerSkinLoadingState m_Loading = new();
        internal readonly PlayerSkinAppliedState m_Applied = new();
        internal readonly PlayerSkinErrorState m_Error = new();

        // Current skin data
        private string m_CurrentSkinName;
        public string CurrentSkinName => m_CurrentSkinName;
        private GameObject m_CurrentPlayerPrefab;
        public GameObject CurrentPlayerPrefab => m_CurrentPlayerPrefab;

        private PlayerSkinDataRegistry m_PlayerSkinDataRegistry;
        public PlayerSkinDataRegistry PlayerSkinDataRegistry => m_PlayerSkinDataRegistry;
        private PrefabManager PrefabManager => ApplicationEntryPoint.Singleton.PrefabManager;
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<PlayerSkinState> states = new() { m_Idle, m_Loading, m_Applied, m_Error };
            InitializeStates(states, m_Idle);

            // Initialize skin data registry (loads skins and surface definitions)
            m_PlayerSkinDataRegistry = new PlayerSkinDataRegistry();
            Debug.Log("[PlayerSkinManager] Initialized");
        }

        void OnDestroy()
        {
            Debug.Log("[PlayerSkinManager] Destroyed");
        }

        /// <summary>
        /// Load and set the player prefab for the given model name
        /// from Addressables via PrefabManager
        /// </summary>
        public GameObject LoadPrefabForModel(string modelName)
        {
            string prefabPath = $"characters/{modelName}";
            //TODO - beim laden oder warten loader anzeigen
            return PrefabManager.LoadPrefab(prefabPath);
        }

        /// <summary>
        /// handles skin load failures from the states
        /// </summary>
        public void OnSkinLoadFailure(string message, PlayerSkinStatus status)
        {
            Debug.LogError($"[PlayerSkinManager] {status}: {message}");
            EventManager.Broadcast(new PlayerSkinErrorEvent { error = message, status = status });
            ChangeState(m_Error);
        }

        /// <summary>
        /// Set the current player prefab
        /// </summary>
        internal void SetCurrentPlayerPrefab(GameObject prefab)
        {
            m_CurrentPlayerPrefab = prefab;
            Debug.Log($"[PlayerSkinManager] prefab assigned: {prefab.name}");
        }

        /// <summary>
        /// Reset all Renderers under the current prefab to active
        /// </summary>
        public void ResetAllRenderersInCurrentPlayerPrefabToActive()
        {
            if (m_CurrentPlayerPrefab == null)
            {
                Debug.LogError("[PlayerSkinManager] Cannot reset Renderers - no prefab loaded");
                return;
            }

            var allRenderers = m_CurrentPlayerPrefab.GetComponentsInChildren<Renderer>(true);

            int activatedCount = 0;
            foreach (var renderer in allRenderers)
            {
                if (!renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(true);
                    activatedCount++;
                }
            }

            Debug.Log($"[PlayerSkinManager] Reset {activatedCount} GameObjects to active");
        }

        /// <summary>
        /// Disable and enable surfaces based on the selected skin data
        /// </summary>
        public void DisableAndEnableSurfacesForCurrentPlayerPrefab(SkinDefinition selectedSkinData)
        {
            var allRenderers = m_CurrentPlayerPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in allRenderers)
            {
                if (renderer != null && renderer.gameObject != null)
                {
                    if (renderer.gameObject.name != null && renderer.gameObject.name.ToLower().Contains("_off")
                    && !renderer.gameObject.name.ToLower().Contains("stupidtriangle"))
                    {
                        renderer.gameObject.SetActive(false);
                    }
                }
                if (selectedSkinData != null && selectedSkinData.prefs != null)
                {
                    string rendererName = renderer.gameObject.name;
                    string cleanRendererName = System.Text.RegularExpressions.Regex.Replace(rendererName, @"_\d+$", "");
                    if (selectedSkinData.prefs.surfaces_on != null)
                    {
                        if (selectedSkinData.prefs.surfaces_on.ContainsValue(cleanRendererName))
                        {
                            renderer.gameObject.SetActive(true);
                        }
                    }
                    if (selectedSkinData.prefs.surfaces_off != null)
                    {
                        if (selectedSkinData.prefs.surfaces_off.ContainsValue(cleanRendererName))
                        {
                            renderer.gameObject.SetActive(false);
                        }
                    }
                }
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
        /// Apply the current skin to a specific character GameObject
        /// </summary>
        public void ApplySkinToCharacter(GameObject character)
        {
            if (m_CurrentState is IPlayerSkinApplicationHandler handler)
            {
                handler.OnApplySkinToCharacter(character, m_CurrentSkinName);
            }
        }

        /// <summary>
        /// Internal: Signal successful skin load (called by states)
        /// </summary>
        internal void OnSkinLoadSuccess(string skinName)
        {
            m_CurrentSkinName = skinName;
            // Broadcast skin changed event
            EventManager.Broadcast(new PlayerSkinChangedEvent { skinName = skinName });
            m_CurrentState.OnSkinLoadSuccess();
        }
    }
}
