using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Loading state - skin is being loaded
    /// </summary>
    internal class PlayerSkinLoadingState : PlayerSkinState
    {
        private string m_LoadingSkinName;

        public void Configure(string skinName)
        {
            m_LoadingSkinName = skinName;
        }

        public override void Enter()
        {
            Debug.Log("[PlayerSkinManager] Entered Loading state");
            LoadSkin();
        }

        public override void Exit() { }

        public void LoadSkin()
        {
            // Validate skin exists
            if (string.IsNullOrEmpty(m_LoadingSkinName))
            {
                Manager.OnSkinLoadFailure("Skin name is empty", PlayerSkinStatus.InvalidSkinName);
                return;
            }

            try
            {
                // Reset all GameObjects to active before changing skin
                ResetAllGameObjectsToActive();

                // Try to load skin data from Resources
                string resourcePath = $"Data/skin_data/{m_LoadingSkinName}";
                SkinDefinition skinDefinition = ParseSkinDefinitionForPath(resourcePath);
                if (skinDefinition == null) return;

                string modelName = skinDefinition.prefs.models["1"];
                LoadShaderDefinitionFromResources($"Data/shaders/{modelName}");
                CreateMaterialsFromSkinDefinition(m_LoadingSkinName, skinDefinition);
                Debug.Log($"[PlayerSkinManager] Successfully loaded skin: {m_LoadingSkinName}.json with model: {modelName}.shader");

                Manager.OnSkinLoadSuccess(m_LoadingSkinName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerSkinManager] Error loading skin: {ex.Message}");
                Manager.OnSkinLoadFailure($"Error loading skin: {ex.Message}", PlayerSkinStatus.GenericError);
                return;
            }
        }

        /// <summary>
        /// Resets all GameObjects under this transform to active (true)
        /// </summary>
        private void ResetAllGameObjectsToActive()
        {
            // Get all renderers under this GameObject
            var allRenderers = Manager.PlayerPrefab.GetComponentsInChildren<Renderer>(true);

            int activatedCount = 0;
            foreach (var renderer in allRenderers)
            {
                if (!renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(true);
                    activatedCount++;
                }
            }

            Debug.Log($"[MyPlayerMaterialAssigner] Reset {activatedCount} GameObjects to active before skin change");
        }

        /// <summary>
        /// Manually load original SoF2 shader definition from Resources folder
        /// </summary>
        public void LoadShaderDefinitionFromResources(string resourcePath)
        {
            // Erwartet z. B. "Data/shaders/modelsSafe"
            string fullPath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".shader");

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[PlayerSkinManager] Shader definition file not found at: {fullPath}");
                Manager.OnSkinLoadFailure($"Shader definition file not found at: {fullPath}", PlayerSkinStatus.ShaderFileNotFound);
                return;
            }

            try
            {
                string shaderText = File.ReadAllText(fullPath);
                Debug.Log($"[PlayerSkinManager] Loaded shader definition manually from: {fullPath}");
                CreateMaterialsForShaderDefinition(shaderText);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSkinManager] Error reading shader file: {ex}");
                Manager.OnSkinLoadFailure($"Error reading shader file: {ex.Message}", PlayerSkinStatus.ShaderFileParseError);
            }
        }

        /// <summary>
        /// Parst eine Shader-Definition Datei und erstellt Materialien basierend auf den Shader-Einträgen
        /// </summary>
        private void CreateMaterialsForShaderDefinition(string shaderContent)
        {
            if (string.IsNullOrEmpty(shaderContent))
            {
                //TODO CUSTOM EXCEPTIONs
                throw new System.ArgumentException("Shader definition content is empty.");
            }

            var shaderEntries = ShaderDataReader.ParseShaderEntries(shaderContent);

            foreach (var entry in shaderEntries)
            {
                // Create material for this shader entry
                CreateMaterialFromShaderEntry(entry.Key, entry.Value);
            }
            Debug.Log($"[PlayerSkinManager] Erzeugte {shaderEntries.Count} Materialien für Shader Definition.");
        }

        private void CreateMaterialFromShaderEntry(string shaderName, ShaderEntry entry)
        {
            string partName = Path.GetFileNameWithoutExtension(shaderName);
            bool isTwoSided = entry.CullDisabled;
            string texturePath = !string.IsNullOrEmpty(entry.MainTexture) ? entry.MainTexture : entry.EditorImage;

            // Shader & Material erzeugen
            Shader shader = Shader.Find(Manager.ShaderRenderName);
            var material = new Material(shader) { name = partName };

            // Textur setzen
            if (!string.IsNullOrEmpty(texturePath))
            {
                var texture = LoadTextureCached(partName, texturePath);
                if (texture != null)
                {
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", texture);
                    else
                        material.mainTexture = texture;
                }
            }

            // Workflow Mode -> Specular
            //if (material.HasProperty("_WorkflowMode"))
            //    material.SetFloat("_WorkflowMode", 1.0f); // 0 = Metallic, 1 = Specular

            // Smoothness -> 0.0
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.0f);

            // Zwei-seitig rendern
            if (isTwoSided)
            {
                if (material.HasProperty("_Cull"))
                    material.SetFloat("_Cull", 0.0f); // 0 = Both

                // Optional auch Keyword aktivieren, falls Shader es nutzt
                material.EnableKeyword("_DOUBLESIDED_ON");
            }

            // In Cache speichern
            Manager.MaterialCache[shaderName] = material;
        }

        private Texture2D LoadTextureCached(string partName, string resourcePathWithoutExt)
        {
            if (string.IsNullOrEmpty(resourcePathWithoutExt)) return null;
            if (Manager.TextureCache.TryGetValue(resourcePathWithoutExt, out var cached)) return cached;

            // Try different paths in Resources folder
            string[] basePaths = { "uQuake/" };

            foreach (var basePath in basePaths)
            {
                // Resources.Load expects path WITHOUT file extension
                string candidate = basePath + resourcePathWithoutExt;

                // Try loading from Resources (Unity automatically handles .png, .jpg, etc.)
                var tex = Resources.Load<Texture2D>(candidate);
                if (tex != null)
                {
                    Manager.TextureCache[resourcePathWithoutExt] = tex;
                    //Debug.Log($"[MyPlayerMaterialAssigner] Texture geladen aus Resources: {candidate}");
                    return tex;
                }
            }

            Debug.LogWarning($"[MyPlayerMaterialAssigner] partName: {partName}, Texture nicht gefunden in Resources: {resourcePathWithoutExt}");
            return null;
        }

        private SkinDefinition ParseSkinDefinitionForPath(string resourcePath)
        {
            TextAsset skinAsset = Resources.Load<TextAsset>(resourcePath);
            string modelType = null;

            if (skinAsset == null)
            {
                Manager.OnSkinLoadFailure($"Skin Definition not found: {resourcePath}", PlayerSkinStatus.SkinNotFound);
                return null;
            }

            // If model type not specified, try to get it from the skin data
            if (string.IsNullOrEmpty(modelType))
            {
                try
                {
                    var rootJson = Newtonsoft.Json.JsonConvert.DeserializeObject<SkinDefinition>(skinAsset.text);
                    if (rootJson?.prefs?.models != null)
                    {
                        rootJson.prefs.models.TryGetValue("1", out modelType);
                    }
                    return rootJson;
                }
                catch (System.Exception ex)
                {
                    Manager.OnSkinLoadFailure($"Failed to parse skin data: {ex.Message}", PlayerSkinStatus.ModelNameParseError);
                    return null;
                }
            }
            return null;
        }

        public override void OnSkinLoadSuccess()
        {
            Manager.ChangeState(Manager.m_Applied);
        }

        public override void OnSkinLoadFailure(string error, PlayerSkinStatus status)
        {
            Debug.LogError($"[PlayerSkinManager] Skin load failed: {error}");
            Manager.EventManager.Broadcast(new PlayerSkinErrorEvent { error = error, status = status });
            Manager.ChangeState(Manager.m_Error);
        }


        private void CreateMaterialsFromSkinDefinition(string fileKey, SkinDefinition skinDefinition)
        {
            if (string.IsNullOrEmpty(fileKey)) fileKey = "unnamed";

            if (skinDefinition?.materials == null || skinDefinition.materials.Count == 0)
            {
                Debug.LogError($"[PlayerSkinManager] Keine 'materials' in JSON: {fileKey}");
                Manager.OnSkinLoadFailure($"No 'materials' in skin definition: {fileKey}", PlayerSkinStatus.InvalidSkinDefinition);
                return;
            }

            var map = new Dictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);

            foreach (var mdef in skinDefinition.materials)
            {
                string partName = mdef.name ?? "unnamed_part";

                if (!map.TryGetValue(partName, out List<Material> list))
                {
                    list = new List<Material>();
                    map[partName] = list;
                }

                foreach (var g in mdef.groups)
                {
                    string cacheKey = g.texture1 == null || g.texture1.Length == 0 ? g.shader1 : g.texture1;
                    if (Manager.MaterialCache.TryGetValue(cacheKey, out Material cached))
                    {
                        list.Add(cached);
                        continue;
                    }
                    else
                    {
                        /*definitions.TryGetValue("default", out var defaultDefinitionVariant);
                        definitions.TryGetValue(selectedModelName, out var modelSpecificVariant);

                        PartDef foundPartDef = null;
                        if (defaultDefinitionVariant != null)
                        {
                            defaultDefinitionVariant.parts.TryGetValue(partName, out foundPartDef);
                        }
                        if (modelSpecificVariant != null && foundPartDef == null)
                        {
                            modelSpecificVariant.parts.TryGetValue(partName, out foundPartDef);
                        }*/

                        //TODO: Create material based on cacheKey
                        //analog ähnlich in CreateMaterialFromShaderEntry()
                        Shader shader = Shader.Find(Manager.ShaderRenderName);
                        var material = new Material(shader) { name = $"{partName}_{cacheKey}" };
                        // Load texture
                        var texture = LoadTextureCached(partName, cacheKey);
                        if (texture != null)
                        {
                            if (material.HasProperty("_BaseMap"))
                                material.SetTexture("_BaseMap", texture);
                            else
                                material.mainTexture = texture;
                        }
                        // Smoothness -> 0.0
                        if (material.HasProperty("_Smoothness"))
                            material.SetFloat("_Smoothness", 0.0f);

                        if (partName.IndexOf("2sided", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            SetTwoSidedURP(material, true);
                        }
                        Manager.MaterialCache[cacheKey] = material;
                        list.Add(material);
                    }
                }
            }

            Manager.MaterialsByFile[fileKey] = map;
            Debug.Log($"[PlayerSkinManager] Zugewiesene Materialien für '{fileKey}': {map.Count} parts. Keys: {string.Join(", ", map.Keys)}");
        }
        private void SetTwoSidedURP(Material mat, bool twoSided)
        {
            if (mat == null) return;
            if (mat.HasProperty("_CullMode"))
                mat.SetInt("_CullMode", twoSided ? 0 : 2);

            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0.0f); // 0 = Both

            // Optional auch Keyword aktivieren, falls Shader es nutzt
            mat.EnableKeyword("_DOUBLESIDED_ON");
        }
    }
}
