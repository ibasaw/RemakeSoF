using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tolik.RemakeSoF.Runtime.Core;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Manages player skin selection and application across game and metagame scenes.
    /// Uses state machine pattern to handle different skin loading states.
    /// </summary>
    public class PlayerSkinManager : StateMachine<PlayerSkinState, PlayerSkinManager>
    {
        public static PlayerSkinManager Singleton { get; private set; }

        internal readonly PlayerSkinIdleState m_Idle = new();
        internal readonly PlayerSkinLoadingState m_Loading = new();
        internal readonly PlayerSkinAppliedState m_Applied = new();
        internal readonly PlayerSkinErrorState m_Error = new();

        // Current skin data
        private readonly string shaderRenderName = "Universal Render Pipeline/Unlit";
        public string ShaderRenderName => shaderRenderName;

        private string m_CurrentSkinName;
        public string CurrentSkinName => m_CurrentSkinName;

        private GameObject m_PlayerPrefab;
        public GameObject PlayerPrefab => m_PlayerPrefab;
        private Dictionary<string, string[]> m_AvailableSkinsByModel = new();
        private Dictionary<string, SkinSurfaceDefinition> m_SurfaceDefinitions =
            new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, SkinSurfaceDefinition> SurfaceDefinitions => m_SurfaceDefinitions;
        private const string SurfaceDefinitionResourcePath = "Data/NPC_definition";

        // Haupt-Datenstruktur: filename -> (partName -> List<Material>)
        private Dictionary<string, Dictionary<string, List<Material>>> m_MaterialsByFile { get; } =
            new Dictionary<string, Dictionary<string, List<Material>>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Dictionary<string, List<Material>>> MaterialsByFile => m_MaterialsByFile;

        // Caches für Performance
        private Dictionary<string, Texture2D> textureCache = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Material> materialCache = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Texture2D> TextureCache => textureCache;
        public Dictionary<string, Material> MaterialCache => materialCache;

        void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
                return;
            }

            Singleton = this;
            DontDestroyOnLoad(gameObject);

            List<PlayerSkinState> states = new() { m_Idle, m_Loading, m_Applied, m_Error };
            InitializeStates(states, m_Idle);

            // Load available skins on startup
            LoadAvailableSkinsData();

            // Parse surface definition from Resources/Data/NPC_definition.json
            LoadSurfaceDefinitionFromResources();
        }

        void OnDestroy()
        {
            if (Singleton == this)
            {
                Singleton = null;
            }
        }

        /// <summary>
        /// Loads metadata about all available skins from Resources
        /// </summary>
        private void LoadAvailableSkinsData()
        {
            m_AvailableSkinsByModel.Clear();

            var allSkinFiles = Resources.LoadAll<TextAsset>("Data/skin_data");

            foreach (var skinFile in allSkinFiles)
            {
                try
                {
                    var rootJson = Newtonsoft.Json.JsonConvert.DeserializeObject<SkinDefinition>(skinFile.text);
                    //TODO: DERZEIT LADE ICH NUR das erste Model (Key "1") - eventuell erweitern für Mehrfach-Modelle
                    if (rootJson?.prefs?.models != null && rootJson.prefs.models.TryGetValue("1", out string model))
                    {
                        if (!m_AvailableSkinsByModel.ContainsKey(model))
                        {
                            m_AvailableSkinsByModel[model] = new string[0];
                        }

                        var list = new List<string>(m_AvailableSkinsByModel[model]) { skinFile.name };
                        m_AvailableSkinsByModel[model] = list.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlayerSkinManager] Error parsing skin file {skinFile.name}: {ex.Message}");
                }
            }

            Debug.Log($"[PlayerSkinManager] Loaded {m_AvailableSkinsByModel.Count} model types with skins");
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
        /// Get available skins for a specific model type
        /// </summary>
        public string[] GetAvailableSkinsForModel(string modelType)
        {
            if (m_AvailableSkinsByModel.TryGetValue(modelType, out var skins))
            {
                return skins;
            }
            return new string[0];
        }

        /// <summary>
        /// Get all available model types
        /// </summary>
        public string[] GetAvailableModelTypes()
        {
            var models = new string[m_AvailableSkinsByModel.Count];
            m_AvailableSkinsByModel.Keys.CopyTo(models, 0);
            return models;
        }

        /// <summary>
        /// Internal: Update current skin data (called by states)
        /// </summary>
        internal void SetCurrentSkin(string skinName)
        {
            m_CurrentSkinName = skinName;

            // Broadcast event
            EventManager.Broadcast(new PlayerSkinChangedEvent { skinName = skinName });
        }

        /// <summary>
        /// Internal: Signal successful skin load (called by states)
        /// </summary>
        internal void OnSkinLoadSuccess(string skinName)
        {
            SetCurrentSkin(skinName);
            m_CurrentState.OnSkinLoadSuccess();
        }

        /// <summary>
        /// Internal: Signal skin load failure (called by states)
        /// </summary>
        internal void OnSkinLoadFailure(string error, PlayerSkinStatus status = PlayerSkinStatus.GenericError)
        {
            m_CurrentState.OnSkinLoadFailure(error, status);
        }

        /// <summary>
        /// Loads and parses NPC_definition.json from Resources/Data.
        /// </summary>
        private void LoadSurfaceDefinitionFromResources()
        {
            var asset = Resources.Load<TextAsset>(SurfaceDefinitionResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[PlayerSkinManager] surfaceDefinition not found at Resources/{SurfaceDefinitionResourcePath}.json");
                return;
            }

            ParseSurfaceDefinition(asset.text);
        }

        /// <summary>
        /// Parses the surface definition JSON (variant -> parts mapping).
        /// </summary>
        /// <param name="json">Raw JSON from definition file.</param>
        private void ParseSurfaceDefinition(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[PlayerSkinManager] surfaceDefinition json is empty");
                return;
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, SkinSurfaceDefinition>>(json);
                if (parsed == null)
                {
                    Debug.LogWarning("[PlayerSkinManager] surfaceDefinition parsed to null");
                    m_SurfaceDefinitions.Clear();
                    return;
                }

                m_SurfaceDefinitions = new Dictionary<string, SkinSurfaceDefinition>(parsed, StringComparer.OrdinalIgnoreCase);
                Debug.Log($"[PlayerSkinManager] surfaceDefinition parsed. Variants: {m_SurfaceDefinitions.Count}. Keys: {string.Join(", ", m_SurfaceDefinitions.Keys)}");
            }
            catch (Exception ex)
            {
                m_SurfaceDefinitions.Clear();
                Debug.LogError($"[PlayerSkinManager] Error parsing surfaceDefinition: {ex}");
            }
        }
    }
}
