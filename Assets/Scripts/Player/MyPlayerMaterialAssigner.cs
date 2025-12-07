using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

using SoF2Remake.Utils;
using Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement;

/// <summary>
/// Liest JSON-Dateien wie aaron_wilson.json, erzeugt URP-Materialien für jedes texture1/shader1 group
/// und speichert sie in: Dictionary[fileKey -> Dictionary<partName -> List<Material>>>.
/// Außerdem liest es eine surfaceDefinition (definition.json) und kann die erzeugten Materialien
/// auf die Surface-Names (z.B. "arm_lwr_r") im GameObject anwenden.
/// </summary>
public class MyPlayerMaterialAssigner : MonoBehaviour
{
    [Header("URP Shader (Fallback)")]
    public string urpShaderName = "Universal Render Pipeline/Unlit";

    [Tooltip("Selected default model type for this player (e.g., average_sleeves, suit_long_coat, etc.)")]
    [SerializeField] private string selectedModelName = "average_sleeves";

    [Header("Skin Selection")]
    [Tooltip("Selected skin from available models")]
    [SerializeField] private string selectedSkinName = "";

    [Header("Available Skins (Read-only)")]
    [SerializeField] private string[] availableSkins = new string[0];

    [Header("Surface definition JSON (mydefinition.json)")]
    public TextAsset surfaceDefinition = null;

    // Haupt-Datenstruktur: filename -> (partName -> List<Material>)
    public Dictionary<string, Dictionary<string, List<Material>>> MaterialsByFile { get; } =
        new Dictionary<string, Dictionary<string, List<Material>>>(StringComparer.OrdinalIgnoreCase);

    // Caches für Performance
    private Dictionary<string, Texture2D> textureCache = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Material> materialCache = new(StringComparer.OrdinalIgnoreCase);

    // Geparste Definitionen: variantKey -> VariantDef
    private Dictionary<string, VariantDef> definitions = new Dictionary<string, VariantDef>(StringComparer.OrdinalIgnoreCase);

    #region JSON-Modelle
    [Serializable]
    public class RootJson
    {
        public Prefs prefs;
        public List<MaterialDef> materials;
    }
    [Serializable]
    public class Prefs
    {
        public Dictionary<string, string> models;
        public Dictionary<string, string> surfaces_on;
        public Dictionary<string, string> surfaces_off;
    }
    [Serializable]
    public class MaterialDef
    {
        public string name;
        public List<Group> groups;
    }
    [Serializable]
    public class Group
    {
        public string name;
        public string texture1;
        public string shader1;
    }

    // Definition.json Struktur
    private class VariantDef
    {
        public string description;
        public Dictionary<string, PartDef> parts;
    }

    private class PartDef
    {
        public string naming;
        public string description;
        public bool required;
        public List<string> material; // Surface names
    }
    #endregion

    private void Start()
    {
        // Load available skins on start
        LoadAvailableSkins();

        // falls surfaceDefinition zugewiesen -> parse
        if (surfaceDefinition != null)
        {
            ParseSurfaceDefinition(surfaceDefinition.text);
        }

        if (definitions.Count == 0)
        {
            Debug.LogWarning("[MyPlayerMaterialAssigner] Keine surfaceDefinition geladen oder geparst.");
            return;
        }

        // If a skin is selected, load it
        RootJson selectedSkinData = null;
        if (!string.IsNullOrEmpty(selectedSkinName))
        {
            selectedSkinData = LoadSelectedSkin();
        }

        DisableAndEnableSurfaces(selectedSkinData);

        // Wenn ein Skin geladen wurde und surfaceDefinition gesetzt ist, wende automatisch Varianten an
        if (!string.IsNullOrEmpty(selectedSkinName) && surfaceDefinition != null)
        {
            string fileKey = selectedSkinName;

            Debug.Log($"[MyPlayerMaterialAssigner] === Starting material application for '{fileKey}' ===");

            if (definitions.ContainsKey("default"))
            {
                Debug.Log("[MyPlayerMaterialAssigner] Applying 'default' variant...");
                ApplyMaterialsFromDefinition(fileKey, "default", this.gameObject);
            }
            else
            {
                Debug.LogWarning("[MyPlayerMaterialAssigner] Keine 'default' Variante in surfaceDefinition gefunden.");
            }

            // Try to apply the model-specific variant (get the model from the skin JSON)
            string modelsSafe = GetModelForSkin(selectedSkinName);
            if (!string.IsNullOrEmpty(modelsSafe) && definitions.ContainsKey(modelsSafe))
            {
                Debug.Log("[MyPlayerMaterialAssigner] Applying '" + modelsSafe + "' variant...");
                ApplyMaterialsFromDefinition(fileKey, modelsSafe, this.gameObject);
            }
            else if (!string.IsNullOrEmpty(modelsSafe))
            {
                Debug.LogWarning("[MyPlayerMaterialAssigner] Keine '" + modelsSafe + "' Variante in surfaceDefinition gefunden.");
            }

            Debug.Log($"[MyPlayerMaterialAssigner] === Finished material application for '{fileKey}' ===");
        }
    }

    /// <summary>
    /// Loads all available skins that use the "selectedModelName" model
    /// </summary>
    private void LoadAvailableSkins()
    {
        var skinFiles = new List<string>();

        // Get all JSON files from skin_data directory
        var allSkinFiles = Resources.LoadAll<TextAsset>("Data/skin_data");

        foreach (var skinFile in allSkinFiles)
        {
            try
            {
                var rootJson = JsonConvert.DeserializeObject<RootJson>(skinFile.text);
                //TODO: DERZEIT LADE ICH NUR das erste Model (Key "1") - eventuell erweitern für Mehrfach-Modelle
                if (rootJson?.prefs?.models != null &&
                    rootJson.prefs.models.TryGetValue("1", out string model) &&
                    model == selectedModelName)
                {
                    skinFiles.Add(skinFile.name);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MyPlayerMaterialAssigner] Error parsing skin file {skinFile.name}: {ex.Message}");
            }
        }

        availableSkins = skinFiles.ToArray();
        Debug.Log($"[MyPlayerMaterialAssigner] Found {availableSkins.Length} skins with '{selectedModelName}' model: {string.Join(", ", availableSkins)}");
    }

    /// <summary>
    /// Loads the currently selected skin
    /// </summary>
    private RootJson LoadSelectedSkin()
    {
        if (string.IsNullOrEmpty(selectedSkinName))
        {
            Debug.LogWarning("[MyPlayerMaterialAssigner] No skin selected");
            return null;
        }

        // Load the skin JSON from Resources
        string resourcePath = $"Data/skin_data/{selectedSkinName}";
        TextAsset skinAsset = Resources.Load<TextAsset>(resourcePath);

        if (skinAsset == null)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Skin not found: {resourcePath}");
            return null;
        }

        try
        {
            // Parse the skin JSON
            RootJson rootJson = JsonConvert.DeserializeObject<RootJson>(skinAsset.text);
            string modelsSafe = null;

            if (rootJson?.prefs?.models != null)
            {
                rootJson.prefs.models.TryGetValue("1", out modelsSafe);
            }

            if (modelsSafe != null)
            {
                LoadShaderDefinitionFromResources("Data/shaders/" + modelsSafe);
                CreateMaterialsFromJson(selectedSkinName, skinAsset.text);
                Debug.Log($"[MyPlayerMaterialAssigner] Successfully loaded skin: {selectedSkinName}.json with model: {modelsSafe}.shader");

            }
            else
            {
                Debug.LogError($"[MyPlayerMaterialAssigner] No model found in skin: {selectedSkinName}");
            }
            return rootJson;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Error loading skin {selectedSkinName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the model type for a given skin name
    /// </summary>
    private string GetModelForSkin(string skinName)
    {
        if (string.IsNullOrEmpty(skinName)) return null;

        string resourcePath = $"Data/skin_data/{skinName}";
        TextAsset skinAsset = Resources.Load<TextAsset>(resourcePath);

        if (skinAsset == null) return null;

        try
        {
            var rootJson = JsonConvert.DeserializeObject<RootJson>(skinAsset.text);
            if (rootJson?.prefs?.models != null)
            {
                rootJson.prefs.models.TryGetValue("1", out string model);
                return model;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Error getting model for skin {skinName}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Public method to change skin at runtime
    /// </summary>
    public void ChangeSkin(string newSkinName)
    {
        if (string.IsNullOrEmpty(newSkinName))
        {
            Debug.LogWarning("[MyPlayerMaterialAssigner] Cannot change to empty skin name");
            return;
        }

        // Check if the skin is available
        if (!availableSkins.Contains(newSkinName))
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Skin '{newSkinName}' is not available. Available skins: {string.Join(", ", availableSkins)}");
            return;
        }

        // Reset all GameObjects to active before changing skin
        ResetAllGameObjectsToActive();

        selectedSkinName = newSkinName;
        RootJson selectedSkinData = LoadSelectedSkin();

        // Durchlaufe alle Renderers und deaktiviere GameObjects, deren Name "_off" enthält
        DisableAndEnableSurfaces(selectedSkinData);

        // Reapply materials if surface definition is available
        if (surfaceDefinition != null)
        {
            string fileKey = selectedSkinName;
            string modelsSafe = GetModelForSkin(selectedSkinName);

            Debug.Log($"[MyPlayerMaterialAssigner] === Changing skin to '{fileKey}' ===");

            if (definitions.ContainsKey("default"))
            {
                ApplyMaterialsFromDefinition(fileKey, "default", this.gameObject);
            }

            if (!string.IsNullOrEmpty(modelsSafe) && definitions.ContainsKey(modelsSafe))
            {
                ApplyMaterialsFromDefinition(fileKey, modelsSafe, this.gameObject);
            }

            Debug.Log($"[MyPlayerMaterialAssigner] === Skin changed to '{fileKey}' ===");
        }
    }

    private void DisableAndEnableSurfaces(RootJson selectedSkinData)
    {
        var allRenderers = this.gameObject.GetComponentsInChildren<Renderer>(true);
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
    /// Resets all GameObjects under this transform to active (true)
    /// </summary>
    private void ResetAllGameObjectsToActive()
    {
        // Get all renderers under this GameObject
        var allRenderers = this.gameObject.GetComponentsInChildren<Renderer>(true);

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
    /// Public method to manually reset all GameObjects to active
    /// </summary>
    public void ResetAllGameObjectsToActivePublic()
    {
        ResetAllGameObjectsToActive();
    }

    #region Loading / Creating Materials (unchanged core)

    public void LoadFromResources(string resourceName)
    {
        TextAsset ta = Resources.Load<TextAsset>(resourceName);
        if (ta == null)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Resource not found: {resourceName}");
            return;
        }
        CreateMaterialsFromJson(resourceName, ta.text);
    }

    public void LoadFromFilePath(string filePath)
    {
        if (!File.Exists(filePath)) { Debug.LogError($"File not found: {filePath}"); return; }
        string json = File.ReadAllText(filePath);
        string key = Path.GetFileNameWithoutExtension(filePath);
        CreateMaterialsFromJson(key, json);
    }

    public void LoadShaderDefinitionFromResources(string resourcePath)
    {
        // Erwartet z. B. "Data/shaders/modelsSafe"
        string fullPath = Path.Combine(Application.dataPath, "Resources", resourcePath + ".shader");

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Shader definition file not found at: {fullPath}");
            return;
        }

        try
        {
            string shaderText = File.ReadAllText(fullPath);
            Debug.Log($"[MyPlayerMaterialAssigner] Loaded shader definition manually from: {fullPath}");
            CreateMaterialsForShaderDefinition(shaderText);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Error reading shader file: {ex}");
        }
    }

    private void CreateMaterialsFromJson(string fileKey, string json)
    {
        if (string.IsNullOrEmpty(fileKey)) fileKey = "unnamed";

        RootJson root;
        try
        {
            root = JsonConvert.DeserializeObject<RootJson>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] JSON parse error: {ex}");
            return;
        }

        if (root?.materials == null || root.materials.Count == 0)
        {
            Debug.LogWarning($"[MyPlayerMaterialAssigner] Keine 'materials' in JSON: {fileKey}");
            return;
        }

        var map = new Dictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);

        foreach (var mdef in root.materials)
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
                if (materialCache.TryGetValue(cacheKey, out Material cached))
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
                    Shader shader = Shader.Find(urpShaderName);
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
                    materialCache[cacheKey] = material;
                    list.Add(material);
                }
            }
        }

        MaterialsByFile[fileKey] = map;
        Debug.Log($"[MyPlayerMaterialAssigner] Zugewiesene Materialien für '{fileKey}': {map.Count} parts. Keys: {string.Join(", ", map.Keys)}");
    }

    private Texture2D LoadTextureCached(string partName, string resourcePathWithoutExt)
    {
        if (string.IsNullOrEmpty(resourcePathWithoutExt)) return null;
        if (textureCache.TryGetValue(resourcePathWithoutExt, out var cached)) return cached;

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
                textureCache[resourcePathWithoutExt] = tex;
                //Debug.Log($"[MyPlayerMaterialAssigner] Texture geladen aus Resources: {candidate}");
                return tex;
            }
        }

        Debug.LogWarning($"[MyPlayerMaterialAssigner] partName: {partName}, Texture nicht gefunden in Resources: {resourcePathWithoutExt}");
        return null;
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

    #endregion

    #region Shader Definition Parsing

    /// <summary>
    /// Parst eine Shader-Definition Datei und erstellt Materialien basierend auf den Shader-Einträgen
    /// </summary>
    private void CreateMaterialsForShaderDefinition(string shaderContent)
    {
        if (string.IsNullOrEmpty(shaderContent))
        {
            Debug.LogWarning("[MyPlayerMaterialAssigner] Shader definition content is empty.");
            return;
        }

        Debug.Log("[MyPlayerMaterialAssigner] === Parsing Shader Definition ===");

        var shaderEntries = ShaderDataReader.ParseShaderEntries(shaderContent);

        foreach (var entry in shaderEntries)
        {
            /*Debug.Log($"[MyPlayerMaterialAssigner] Shader Entry: {entry.Key}");
            /Debug.Log($"  - Hit Location: {entry.Value.HitLocation}");
            Debug.Log($"  - Hit Material: {entry.Value.HitMaterial}");
            Debug.Log($"  - Editor Image: {entry.Value.EditorImage}");
            Debug.Log($"  - Cull Disabled: {entry.Value.CullDisabled}");
            Debug.Log($"  - Main Texture: {entry.Value.MainTexture}");*/

            // Create material for this shader entry
            CreateMaterialFromShaderEntry(entry.Key, entry.Value);
        }
        Debug.Log($"[MyPlayerMaterialAssigner] Erzeugte {shaderEntries.Count} Materialien für Shader Definition.");
    }
    private void CreateMaterialFromShaderEntry(string shaderName, ShaderEntry entry)
    {
        string partName = Path.GetFileNameWithoutExtension(shaderName);
        bool isTwoSided = entry.CullDisabled;
        string texturePath = !string.IsNullOrEmpty(entry.MainTexture) ? entry.MainTexture : entry.EditorImage;

        // Shader & Material erzeugen
        Shader shader = Shader.Find(urpShaderName);
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
        materialCache[shaderName] = material;
    }

    #endregion

    #region Definition parsing + assignment

    private void ParseSurfaceDefinition(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            // parse top-level dictionary: variantKey -> variant object
            var raw = JsonConvert.DeserializeObject<Dictionary<string, VariantRaw>>(json);
            definitions.Clear();
            foreach (var kv in raw)
            {
                var variant = new VariantDef();
                variant.description = kv.Value.description;
                variant.parts = new Dictionary<string, PartDef>(StringComparer.OrdinalIgnoreCase);

                if (kv.Value.parts != null)
                {
                    foreach (var p in kv.Value.parts)
                    {
                        var pd = new PartDef
                        {
                            naming = p.Value.naming,
                            description = p.Value.description,
                            required = p.Value.required,
                            material = p.Value.material ?? new List<string>()
                        };
                        variant.parts[p.Key] = pd;
                    }
                }
                definitions[kv.Key] = variant;
            }
            Debug.Log($"[MyPlayerMaterialAssigner] surfaceDefinition parsed. Variants: {definitions.Count}. Keys: {string.Join(", ", definitions.Keys)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Error parsing surfaceDefinition: {ex}");
        }
    }

    // helper types to match JSON shape (for parsing)
    private class VariantRaw
    {
        public string description;
        public Dictionary<string, PartRaw> parts;
    }
    private class PartRaw
    {
        public string naming;
        public string description;
        public bool required;
        public List<string> material;
    }

    /// <summary>
    /// Wendet die erzeugten Materialien (MaterialsByFile[fileKey]) entsprechend der surfaceDefinition-Variante
    /// auf alle Renderers im 'root' GameObject an.
    /// </summary>
    public void ApplyMaterialsFromDefinition(string fileKey, string variantKey, GameObject root)
    {
        if (string.IsNullOrEmpty(fileKey) || string.IsNullOrEmpty(variantKey) || root == null)
        {
            Debug.LogError("[MyPlayerMaterialAssigner] ApplyMaterialsFromDefinition: Argumente ungültig.");
            return;
        }

        if (!MaterialsByFile.TryGetValue(fileKey, out var partToMats))
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Keine Materialien geladen für fileKey '{fileKey}'.");
            return;
        }

        if (!definitions.TryGetValue(variantKey, out var variant))
        {
            Debug.LogError($"[MyPlayerMaterialAssigner] Variante '{variantKey}' nicht in surfaceDefinition vorhanden.");
            return;
        }

        Debug.Log($"[MyPlayerMaterialAssigner] === Applying variant '{variantKey}' to '{root.name}' ===");
        Debug.Log($"[MyPlayerMaterialAssigner] Required materials for '{fileKey}': {string.Join(", ", partToMats.Keys)}");
        Debug.Log($"[MyPlayerMaterialAssigner] Available parts for '{variantKey}': {string.Join(", ", variant.parts.Keys)}");

        // For quick renderer lookup, get all renderers under root
        var allRenderers = root.GetComponentsInChildren<Renderer>(true);

        // Create a mapping of surface names to materials for efficient lookup
        var surfaceToMaterialMap = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        int replaced = 0;

        // First pass: Build surface-to-material mapping
        foreach (var partKvp in variant.parts)
        {
            string partName = partKvp.Key; // e.g. "arms", "avmed", "face", etc.
            var surfaces = partKvp.Value.material; // list of surface names (arm_lwr_l, ...)

            // get material list for this part (may be null)
            partToMats.TryGetValue(partName, out var matsForPart);

            Debug.Log($"[MyPlayerMaterialAssigner] [{variantKey}] Processing part '{partName}' with {surfaces.Count} surfaces, {matsForPart?.Count ?? 0} materials available");

            if (matsForPart != null && matsForPart.Count > 0)
            {
                Material matToAssign = matsForPart[0]; // Use first material for this part

                // Map all surfaces of this part to the same material
                foreach (string surfaceName in surfaces)
                {
                    surfaceToMaterialMap[surfaceName] = matToAssign;
                }
            }
            else
            {
                Debug.LogWarning($"[MyPlayerMaterialAssigner] [{variantKey}] No materials found for part '{partName}', hiding all surfaces. Available parts: {string.Join(", ", partToMats.Keys)}");

                // Hide all surfaces for this part by setting them inactive
                foreach (string surfaceName in surfaces)
                {
                    var matches = FindRenderersForSurface(allRenderers, surfaceName);
                    foreach (var renderer in matches)
                    {
                        renderer.gameObject.SetActive(false);
                        //Debug.Log($"[MyPlayerMaterialAssigner] Hidden surface: '{surfaceName}' (GameObject: '{renderer.gameObject.name}')");
                    }
                }
            }
        }

        // Second pass: Apply materials respecting hierarchy (most specific first)
        var sortedSurfaces = surfaceToMaterialMap.Keys.ToList();
        // Sort by specificity: longer names (more specific) first
        sortedSurfaces.Sort((a, b) => b.Length.CompareTo(a.Length));

        foreach (string surfaceName in sortedSurfaces)
        {
            Material matToAssign = surfaceToMaterialMap[surfaceName];

            // Find renderers matching this surface
            var matches = FindRenderersForSurface(allRenderers, surfaceName);

            foreach (var renderer in matches)
            {
                // Check if this renderer already has a material assigned (from a more specific surface)
                if (HasMaterialAssigned(renderer, surfaceToMaterialMap))
                {
                    Debug.Log($"[MyPlayerMaterialAssigner] Renderer '{renderer.gameObject.name}' already has material assigned, skipping surface '{surfaceName}'");
                    continue;
                }

                // Apply material to this renderer
                if (ApplyMaterialToRenderer(renderer, matToAssign, surfaceName))
                {
                    replaced++;
                    //Debug.Log($"[MyPlayerMaterialAssigner] Applied material '{matToAssign.name}' to renderer '{renderer.gameObject.name}' for surface '{surfaceName}'");
                }
            }
        }

        Debug.Log($"[MyPlayerMaterialAssigner] [{variantKey}] ApplyMaterialsFromDefinition finished. Replaced {replaced} renderer-material associations.");
    }

    /// <summary>
    /// Prüft ob ein Renderer bereits ein Material aus der surfaceToMaterialMap zugewiesen bekommen hat
    /// </summary>
    private bool HasMaterialAssigned(Renderer renderer, Dictionary<string, Material> surfaceToMaterialMap)
    {
        // Check if any of the renderer's materials are from our mapping
        var materials = renderer.sharedMaterials;
        if (materials == null) return false;

        foreach (var mat in materials)
        {
            if (mat == null) continue;

            // Check if this material is from our mapping
            foreach (var kvp in surfaceToMaterialMap)
            {
                if (mat == kvp.Value)
                {
                    return true;
                }
            }
        }
        return false;
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

    #endregion
}
