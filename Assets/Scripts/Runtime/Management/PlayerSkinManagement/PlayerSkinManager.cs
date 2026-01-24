using System;
using System.Collections.Generic;
using System.Linq;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Orchestriert das Laden und Anwenden von Spieler-Skins via State Machine (Idle/Loading/Applied/Error),
    /// verwaltet aktuellen Skin/Prefab, ruft PrefabManager + PlayerSkinDataRegistry auf und broadcastet Status/Fehler.
    /// </summary>
    public class PlayerSkinManager : StateMachine<PlayerSkinState, PlayerSkinManager>
    {
        internal readonly PlayerSkinIdleState m_Idle = new();
        internal readonly PlayerSkinLoadingState m_Loading = new();
        internal readonly PlayerSkinAppliedState m_Applied = new();
        internal readonly PlayerSkinErrorState m_Error = new();
        private PlayerSkinDataRegistry m_PlayerSkinDataRegistry;

        // Current skin data
        private string m_CurrentSkinName;
        private GameObject m_CurrentPlayerPrefab;

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
            m_PlayerSkinDataRegistry.ClearAllCaches();
        }

        public SkinDefinition GetSkinByName(string skinName)
        {
            return m_PlayerSkinDataRegistry.GetSkinByName(skinName);
        }

        public Dictionary<string, ShaderEntry> GetLegacyShaderDefinitionForModel(string modelName)
        {
            return m_PlayerSkinDataRegistry.GetLegacyShaderDefinitionForModel(modelName);
        }

        /// <summary>
        /// Erstellt Materialien basierend auf der SkinDefinition (JSON)
        /// </summary>
        public void CreateMaterialsFromSkinDefinition(string selectedSkinName,
            Dictionary<string, ShaderEntry> shaderDefinition, SkinDefinition skinDefinition)
        {
            if (skinDefinition?.materials == null || skinDefinition.materials.Count == 0)
            {
                Debug.LogError($"[PlayerSkinManager] Keine 'materials' in skinDefinition JSON vorhanden: {selectedSkinName}");
                return;
            }
            ServiceLocator.Get<TextureManager>().CreateMaterialsFromSkinDefinition(shaderDefinition, skinDefinition);
        }

        public void ApplyMaterialsToCurrentPlayerPrefab(string skinDefinitionName, SkinDefinition skinDefinition)
        {
            ApplySurfaceDefinitions(skinDefinitionName, "default", skinDefinition);
            string modelName = skinDefinition.GetModelName();
            ApplySurfaceDefinitions(skinDefinitionName, modelName, skinDefinition);
        }

        private bool ApplySurfaceDefinitions(string skinDefinitionName, string modelName, SkinDefinition skinDefinition)
        {
            m_PlayerSkinDataRegistry.SkinSurfaceDefinitionsByModel.TryGetValue(modelName, out SkinSurfaceDefinition surfaceDefinitions);
            if (surfaceDefinitions == null)
            {
                Debug.LogWarning($"[PlayerSkinManager] No surface definitions found for model: {modelName}");
                return false;
            }

            Renderer[] allRenderers = m_CurrentPlayerPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (KeyValuePair<string, SkinSurfacePartDefinition> part in surfaceDefinitions.parts)
            {
                string partName = part.Key;
                var surfaces = part.Value.material;
                string textureKey = skinDefinition.GetTextureOrShadowForMaterialDefinitionName(partName);
                if (string.IsNullOrEmpty(textureKey))
                {
                    Debug.LogWarning($"[PlayerSkinManager] No texture/shader-key found for material definition name: '{partName}' in skin definition: '{skinDefinitionName}'");
                    continue;
                }
                TextureData textureData = ServiceLocator.Get<TextureManager>().GetTextureData(textureKey);
                if (textureData == null || !textureData.IsValid())
                {
                    Debug.Log($"[PlayerSkinManager] Search by TextureData by alias for key: '{textureKey}' in skin definition: '{skinDefinitionName}'");
                    textureData = ServiceLocator.Get<TextureManager>().GetTextureDataByAlias(textureKey);
                }
                Debug.Log($"[PlayerSkinManager] TextureData for key '{textureKey}': {textureData}");
                Debug.Log($"[PlayerSkinManager] [{modelName} , {skinDefinitionName}] Processing part '{partName}' with texture/shader: {textureKey}");
                foreach (string surfaceName in surfaces)
                {
                    List<Renderer> matches = FindRenderersForSurface(allRenderers, surfaceName);
                    Debug.Log($"[PlayerSkinManager] Found {matches.Count} renderers for surface '{surfaceName}' in part '{partName} for model '{modelName}'");
                    foreach (var renderer in matches)
                    {
                        ApplyMaterialToRenderer(renderer, textureData.Material, surfaceName);
                        Debug.Log($"[PlayerSkinManager] Applied material to renderer '{renderer.gameObject.name}' for surface '{surfaceName}' in part '{partName}'");
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Wendet ein Material auf einen Renderer an, respektiert dabei Multi-Material Slots
        /// </summary>
        private bool ApplyMaterialToRenderer(Renderer renderer, Material material, string surfaceName)
        {
            var shared = renderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                // Simple case: single material slot
                renderer.material = material;
                return true;
            }

            if (shared.Length == 1)
            {
                // Single material slot
                renderer.material = material;
                return true;
            }

            // Multiple material slots - try to find the right slot
            var newMats = new Material[shared.Length];
            Array.Copy(shared, newMats, shared.Length);

            bool anyReplace = false;
            for (int mi = 0; mi < shared.Length; mi++)
            {
                var slot = shared[mi];
                if (slot == null) continue;

                string baseName = slot.name.Split(' ')[0];
                if (string.Equals(baseName, surfaceName, StringComparison.OrdinalIgnoreCase) ||
                    baseName.IndexOf(surfaceName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    renderer.gameObject.name.IndexOf(surfaceName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    newMats[mi] = material;
                    anyReplace = true;
                }
            }

            if (anyReplace)
            {
                renderer.materials = newMats;
                return true;
            }
            else
            {
                // Fallback: replace first slot
                newMats[0] = material;
                renderer.materials = newMats;
                return true;
            }
        }

        // Suche Renderer die zum Surface passen (exact name, contains, oder renderer.name contains)
        private List<Renderer> FindRenderersForSurface(Renderer[] allRenderers, string surfaceName)
        {
            var list = new List<Renderer>();
            if (string.IsNullOrEmpty(surfaceName)) return list;

            //Debug.Log($"[MyPlayerMaterialAssigner] Searching for renderers matching surface: '{surfaceName}'");

            // 1) exact match by GameObject name
            foreach (var r in allRenderers)
            {
                if (string.Equals(r.gameObject.name, surfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(r);
                    Debug.Log($"[MyPlayerMaterialAssigner] Exact match found: '{r.gameObject.name}'");
                }
            }
            if (list.Count > 0) return list;

            // 2) exact match by GameObject name without suffix (e.g., "cap_fhead_frnt_uppr_r_off_0" matches "cap_fhead_frnt_uppr_r_off")
            foreach (var r in allRenderers)
            {
                string rendererName = r.gameObject.name;
                // Remove common suffixes like "_0", "_1", etc.
                string cleanRendererName = System.Text.RegularExpressions.Regex.Replace(rendererName, @"_\d+$", "");

                if (string.Equals(cleanRendererName, surfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(r);
                    //Debug.Log($"[MyPlayerMaterialAssigner] Clean match found: '{r.gameObject.name}' -> '{cleanRendererName}'");
                }
            }
            if (list.Count > 0) return list;

            // 3) check shared material base names (slot names) - exact match only
            foreach (var r in allRenderers)
            {
                var shared = r.sharedMaterials;
                if (shared == null) continue;
                foreach (var s in shared)
                {
                    if (s == null) continue;
                    var baseName = s.name.Split(' ')[0];
                    if (string.Equals(baseName, surfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(r);
                        Debug.Log($"[MyPlayerMaterialAssigner] Material name match found: '{r.gameObject.name}' (material: '{s.name}')");
                        break;
                    }
                }
            }
            if (list.Count > 0) return list;

            // 4) More specific contains matching - only if the surface name is a significant part
            foreach (var r in allRenderers)
            {
                string rendererName = r.gameObject.name;

                // Only match if the surface name is at least 50% of the renderer name length
                // and the renderer name starts with or contains the surface name as a significant part
                if (surfaceName.Length >= rendererName.Length * 0.5f &&
                    rendererName.IndexOf(surfaceName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add(r);
                    Debug.Log($"[MyPlayerMaterialAssigner] Significant contains match found: '{r.gameObject.name}' contains '{surfaceName}'");
                }
            }
            if (list.Count > 0) return list;

            // 5) Last resort: token-based matching with higher threshold
            var surfaceTokens = surfaceName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (surfaceTokens.Length >= 3) // Only for complex names
            {
                foreach (var r in allRenderers)
                {
                    var rendererTokens = r.gameObject.name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Count matching tokens
                    int matchingTokens = 0;
                    foreach (var surfaceToken in surfaceTokens)
                    {
                        foreach (var rendererToken in rendererTokens)
                        {
                            if (string.Equals(surfaceToken, rendererToken, StringComparison.OrdinalIgnoreCase))
                            {
                                matchingTokens++;
                                break;
                            }
                        }
                    }

                    // Only match if at least 70% of surface tokens match
                    if (matchingTokens >= surfaceTokens.Length * 0.7f)
                    {
                        //list.Add(r);
                        //Debug.Log($"[MyPlayerMaterialAssigner] Token match found: '{r.gameObject.name}' ({matchingTokens}/{surfaceTokens.Length} tokens)");
                    }
                }
            }

            if (list.Count == 0)
            {
                Debug.LogWarning($"[MyPlayerMaterialAssigner] No renderer found for surface: '{surfaceName}'");
            }

            // dedupe
            return list.Distinct().ToList();
        }

        /// <summary>
        /// Load and set the player prefab for the given model name
        /// from Addressables via PrefabManager
        /// TODO: async machen
        /// 
        /// </summary>
        public GameObject LoadPrefabForModel(string prefabPath)
        {
            return ServiceLocator.Get<PrefabManager>().LoadPrefab<GameObject>(prefabPath);
        }
        public RuntimeAnimatorController LoadAnimatorForModel(string prefabPath)
        {
            return ServiceLocator.Get<PrefabManager>().LoadPrefab<RuntimeAnimatorController>(prefabPath);
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
        /// Load the next available skin in the registry 
        /// </summary>
        public void LoadNextSkin()
        {
            string nextName = m_PlayerSkinDataRegistry.GetNextSkinName(m_CurrentSkinName);
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
            string prevName = m_PlayerSkinDataRegistry.GetPreviousSkinName(m_CurrentSkinName);
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
        /// handles skin load failures from the states
        /// </summary>
        public void OnSkinLoadFailure(string message, PlayerSkinStatus status)
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
                skinName = m_CurrentSkinName,
                playerPrefab = m_CurrentPlayerPrefab
            });
        }

        internal void SetCurrentPlayerPrefab(GameObject prefab)
        {
            m_CurrentPlayerPrefab = prefab;
        }
    }
}
