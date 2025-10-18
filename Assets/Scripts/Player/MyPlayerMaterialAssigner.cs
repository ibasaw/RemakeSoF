using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Liest JSON-Dateien wie aaron_wilson.json, erzeugt URP-Materialien für jedes texture1/shader1 group
/// und speichert sie in: Dictionary[fileKey -> Dictionary<partName -> List<Material>>>.
/// Außerdem liest es eine surfaceDefinition (definition.json) und kann die erzeugten Materialien
/// auf die Surface-Names (z.B. "arm_lwr_r") im GameObject anwenden.
/// </summary>
public class MyPlayerMaterialAssigner : MonoBehaviour
{
    [Header("URP Shader (Fallback)")]
    public string urpShaderName = "Universal Render Pipeline/Lit";

    [Header("Optional: load a JSON from Resources on Start (TextAsset)")]
    public TextAsset loadResourceOnStart = null;

    [Header("Surface definition JSON (mydefinition.json)")]
    public TextAsset surfaceDefinition = null;

    // Haupt-Datenstruktur: filename -> (partName -> List<Material>)
    public Dictionary<string, Dictionary<string, List<Material>>> MaterialsByFile { get; } =
        new Dictionary<string, Dictionary<string, List<Material>>>(StringComparer.OrdinalIgnoreCase);

    // Caches für Performance
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Material> materialCache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

    // Geparste Definitionen: variantKey -> VariantDef
    private Dictionary<string, VariantDef> definitions = new Dictionary<string, VariantDef>(StringComparer.OrdinalIgnoreCase);

    #region JSON-Modelle
    [Serializable]
    private class RootJson
    {
        public Prefs prefs;
        public List<MaterialDef> materials;
    }
    [Serializable]
    private class Prefs { public Dictionary<string, string> models; public Dictionary<string, string> surfaces_on; public Dictionary<string, string> surfaces_off; }
    [Serializable]
    private class MaterialDef
    {
        public string name;
        public List<Group> groups;
    }
    [Serializable]
    private class Group
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
        // falls materials.json definierter TextAsset im Inspector gesetzt ist -> lade
        if (loadResourceOnStart != null)
        {
            CreateMaterialsFromJson(loadResourceOnStart.name, loadResourceOnStart.text);
        }

        // Load shader definition from Resources
        LoadShaderDefinitionFromResources("uQuake/shaders/average_sleeves");

        // falls surfaceDefinition zugewiesen -> parse
        if (surfaceDefinition != null)
        {
            ParseSurfaceDefinition(surfaceDefinition.text);
        }

        // Wenn beides gesetzt ist, wende automatisch 'default' Variante auf dieses GameObject an
        if (loadResourceOnStart != null && surfaceDefinition != null)
        {
            string fileKey = loadResourceOnStart.name;
            
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
            
            if (definitions.ContainsKey("average_sleeves"))
            {
                Debug.Log("[MyPlayerMaterialAssigner] Applying 'average_sleeves' variant...");
                ApplyMaterialsFromDefinition(fileKey, "average_sleeves", this.gameObject);
            }
            else
            {
                Debug.LogWarning("[MyPlayerMaterialAssigner] Keine 'average_sleeves' Variante in surfaceDefinition gefunden.");
            }
            
            Debug.Log($"[MyPlayerMaterialAssigner] === Finished material application for '{fileKey}' ===");
        }
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
    // Erwartet z. B. "uQuake/shaders/average_sleeves"
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


    /// <summary>
    /// Public method to load shader definition manually
    /// </summary>
    public void LoadAverageSleevesShader()
    {
        LoadShaderDefinitionFromResources("uQuake/shaders/average_sleeves");
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

            if (mdef.groups == null || mdef.groups.Count == 0)
            {
                var emptyMat = CreateMaterialInstance(fileKey, partName, "default");
                var emptyMatAverageSleeves = CreateMaterialInstance(fileKey, partName, "average_sleeves");
                list.Add(emptyMat);
                list.Add(emptyMatAverageSleeves);
                continue;
            }

            int groupIndex = 0;
            foreach (var g in mdef.groups)
            {
                string groupName = string.IsNullOrEmpty(g.name) ? $"g{groupIndex}" : g.name;
                string cacheKey = $"{fileKey}_{partName}_{groupName}";

                if (materialCache.TryGetValue(cacheKey, out Material cached))
                {
                    list.Add(cached);
                    groupIndex++;
                    continue;
                }

                Shader shader = Shader.Find(urpShaderName);
                if (!string.IsNullOrEmpty(g.shader1) && !g.shader1.Contains("/"))
                {
                    var s = Shader.Find(g.shader1);
                    if (s != null) shader = s;
                }

                var mat = new Material(shader) { name = cacheKey };

                Texture2D tex = null;
                if (!string.IsNullOrEmpty(g.texture1))
                {
                    tex = LoadTextureCached(partName, g.texture1);
                }
                else if (!string.IsNullOrEmpty(g.shader1) && g.shader1.Contains("/"))
                {
                    tex = LoadTextureCached(partName, g.shader1);
                }

                if (tex != null)
                {
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", tex);
                    else
                        mat.mainTexture = tex;
                }

                bool twoSided = (partName.IndexOf("2sided", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (groupName.IndexOf("2sided", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (mdef.name != null && mdef.name.IndexOf("2sided", StringComparison.OrdinalIgnoreCase) >= 0);

                if (twoSided) SetTwoSidedURP(mat, true);

                materialCache[cacheKey] = mat;
                list.Add(mat);

                groupIndex++;
            }
        }

        MaterialsByFile[fileKey] = map;
        Debug.Log($"[MyPlayerMaterialAssigner] Erzeugt Materialien für '{fileKey}': {map.Count} parts.");
        
        // Debug: Zeige alle erstellten Parts und ihre Materialien
        /*foreach (var partKvp in map)
        {
            Debug.Log($"[MyPlayerMaterialAssigner] Part '{partKvp.Key}': {partKvp.Value.Count} materials");
            foreach (var mat in partKvp.Value)
            {
                Debug.Log($"[MyPlayerMaterialAssigner]   - Material: {mat.name}, Shader: {mat.shader.name}, HasTexture: {mat.mainTexture != null}");
            }
        }*/
    }

    private Material CreateMaterialInstance(string fileKey, string partName, string groupName)
    {
        if (string.IsNullOrEmpty(fileKey)) fileKey = "unnamed";
        if (string.IsNullOrEmpty(partName)) partName = "part";
        if (string.IsNullOrEmpty(groupName)) groupName = "g";

        string cacheKey = $"{fileKey}_{partName}_{groupName}";

        if (materialCache.TryGetValue(cacheKey, out var cachedMat))
            return cachedMat;

        Shader shader = Shader.Find(urpShaderName);
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[MyPlayerMaterialAssigner] URP Shader nicht gefunden, benutze Standard Shader.");
                shader = Shader.Find("Standard");
            }
        }

        var mat = new Material(shader) { name = cacheKey };
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", null);
        else
            mat.mainTexture = null;

        materialCache[cacheKey] = mat;
        return mat;
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

        var shaderEntries = ParseShaderEntries(shaderContent);
        
        foreach (var entry in shaderEntries)
        {
            Debug.Log($"[MyPlayerMaterialAssigner] Shader Entry: {entry.Key}");
            Debug.Log($"  - Hit Location: {entry.Value.HitLocation}");
            Debug.Log($"  - Editor Image: {entry.Value.EditorImage}");
            Debug.Log($"  - Cull Disabled: {entry.Value.CullDisabled}");
            Debug.Log($"  - Main Texture: {entry.Value.MainTexture}");
            
            // Create material for this shader entry
            CreateMaterialFromShaderEntry(entry.Key, entry.Value);
        }
    }

    private class ShaderEntry
    {
        public string HitLocation { get; set; }
        public string EditorImage { get; set; }
        public bool CullDisabled { get; set; }
        public string MainTexture { get; set; }
    }

    private Dictionary<string, ShaderEntry> ParseShaderEntries(string content)
    {
        var entries = new Dictionary<string, ShaderEntry>();
        var lines = content.Split('\n');
        
        string currentShader = null;
        ShaderEntry currentEntry = null;
        bool inShaderBlock = false;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                continue;
            
            // Check if this is a shader name (starts with models/ and ends with {)
            if (line.StartsWith("models/") && line.EndsWith("{"))
            {
                // Save previous entry if exists
                if (currentShader != null && currentEntry != null)
                {
                    entries[currentShader] = currentEntry;
                }
                
                // Start new shader entry
                currentShader = line.Substring(0, line.Length - 1).Trim();
                currentEntry = new ShaderEntry();
                inShaderBlock = true;
                continue;
            }
            
            // Check if we're ending a shader block
            if (line == "}" && inShaderBlock)
            {
                if (currentShader != null && currentEntry != null)
                {
                    entries[currentShader] = currentEntry;
                }
                currentShader = null;
                currentEntry = null;
                inShaderBlock = false;
                continue;
            }
            
            // Parse shader properties
            if (inShaderBlock && currentEntry != null)
            {
                ParseShaderProperty(line, currentEntry);
            }
        }
        
        // Don't forget the last entry
        if (currentShader != null && currentEntry != null)
        {
            entries[currentShader] = currentEntry;
        }
        
        return entries;
    }

    private void ParseShaderProperty(string line, ShaderEntry entry)
    {
        // Parse hitLocation
        if (line.StartsWith("hitLocation"))
        {
            var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                entry.HitLocation = parts[1];
            }
        }
        
        // Parse qer_editorimage
        if (line.StartsWith("qer_editorimage"))
        {
            var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                entry.EditorImage = parts[1];
            }
        }
        
        // Parse cull disable
        if (line.Trim() == "cull\tdisable" || line.Trim() == "cull disable")
        {
            entry.CullDisabled = true;
        }
        
        // Parse map (main texture)
        if (line.StartsWith("map"))
        {
            var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                entry.MainTexture = parts[1];
            }
        }
    }

    private void CreateMaterialFromShaderEntry(string shaderName, ShaderEntry entry)
    {
        // Extract part name from shader name (e.g., "models/characters/average_face/f_czech_serg_w1" -> "f_czech_serg_w1")
        string partName = Path.GetFileNameWithoutExtension(shaderName);
        
        // Determine material properties
        bool isTwoSided = entry.CullDisabled;
        string texturePath = !string.IsNullOrEmpty(entry.MainTexture) ? entry.MainTexture : entry.EditorImage;
        
        // Create material
        Shader shader = Shader.Find(urpShaderName);
        var material = new Material(shader) { name = $"shader_{partName}" };
        
        // Load texture
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
        
        // Set two-sided if needed
        if (isTwoSided)
        {
            SetTwoSidedURP(material, true);
        }
        
        // Store in materials cache
        string cacheKey = $"shader_{partName}";
        materialCache[cacheKey] = material;
        
        Debug.Log($"[MyPlayerMaterialAssigner] Created material '{material.name}' from shader '{shaderName}' (TwoSided: {isTwoSided})");
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
            Debug.Log($"[MyPlayerMaterialAssigner] surfaceDefinition parsed. Variants: {definitions.Count}");
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
        Debug.Log($"[MyPlayerMaterialAssigner] Available materials for '{fileKey}': {string.Join(", ", partToMats.Keys)}");
        Debug.Log($"[MyPlayerMaterialAssigner] Required parts for '{variantKey}': {string.Join(", ", variant.parts.Keys)}");

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
                Debug.LogWarning($"[MyPlayerMaterialAssigner] [{variantKey}] No materials found for part '{partName}', skipping all surfaces. Available parts: {string.Join(", ", partToMats.Keys)}");
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

    #region Helper-API

    public List<Material> GetMaterialsFor(string fileKey, string partName)
    {
        if (MaterialsByFile.TryGetValue(fileKey, out var map) && map.TryGetValue(partName, out var list))
            return list;
        return null;
    }

    /// <summary>
    /// Public helper: ruft ApplyMaterialsFromDefinition für das gegebene root auf.
    /// </summary>
    public void ApplyMaterialsForLoadedFile(string fileKey, string variantKey, GameObject root)
    {
        ApplyMaterialsFromDefinition(fileKey, variantKey, root);
    }

    /// <summary>
    /// Debug helper: Zeigt alle verfügbaren Parts und Materialien für eine Datei
    /// </summary>
    public void DebugShowAvailableMaterials(string fileKey)
    {
        if (!MaterialsByFile.TryGetValue(fileKey, out var map))
        {
            Debug.LogWarning($"[MyPlayerMaterialAssigner] Keine Materialien für '{fileKey}' gefunden.");
            return;
        }

        Debug.Log($"[MyPlayerMaterialAssigner] === Verfügbare Materialien für '{fileKey}' ===");
        foreach (var partKvp in map)
        {
            Debug.Log($"[MyPlayerMaterialAssigner] Part: '{partKvp.Key}' ({partKvp.Value.Count} materials)");
            for (int i = 0; i < partKvp.Value.Count; i++)
            {
                var mat = partKvp.Value[i];
                Debug.Log($"[MyPlayerMaterialAssigner]   [{i}] {mat.name} (Shader: {mat.shader.name}, Texture: {(mat.mainTexture != null ? mat.mainTexture.name : "NULL")})");
            }
        }
    }

    /// <summary>
    /// Debug helper: Zeigt alle verfügbaren Varianten in der surfaceDefinition
    /// </summary>
    public void DebugShowAvailableVariants()
    {
        Debug.Log($"[MyPlayerMaterialAssigner] === Verfügbare Varianten ===");
        foreach (var variantKvp in definitions)
        {
            Debug.Log($"[MyPlayerMaterialAssigner] Variant: '{variantKvp.Key}' - {variantKvp.Value.description}");
            foreach (var partKvp in variantKvp.Value.parts)
            {
                Debug.Log($"[MyPlayerMaterialAssigner]   Part: '{partKvp.Key}' ({partKvp.Value.material.Count} surfaces)");
            }
        }
    }

    /// <summary>
    /// Debug helper: Zeigt alle Renderer in einem GameObject
    /// </summary>
    public void DebugShowAllRenderers(GameObject root)
    {
        if (root == null)
        {
            Debug.LogWarning("[MyPlayerMaterialAssigner] Root GameObject ist null!");
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[MyPlayerMaterialAssigner] === Alle Renderer in '{root.name}' ===");
        Debug.Log($"[MyPlayerMaterialAssigner] Gefunden: {renderers.Length} Renderer");
        
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            Debug.Log($"[MyPlayerMaterialAssigner] Renderer: '{renderer.gameObject.name}' ({materials?.Length ?? 0} materials)");
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    Debug.Log($"[MyPlayerMaterialAssigner]   [{i}] {(mat != null ? mat.name : "NULL")}");
                }
            }
        }
    }

    /// <summary>
    /// Debug helper: Zeigt alle geparsten Shader-Einträge
    /// </summary>
    public void DebugShowShaderEntries()
    {
        string resourcePath = "uQuake/shaders/average_sleeves";
        TextAsset shaderAsset = Resources.Load<TextAsset>(resourcePath);
        
        if (shaderAsset == null)
        {
            Debug.LogWarning($"[MyPlayerMaterialAssigner] Shader-Definition nicht gefunden in Resources: {resourcePath}");
            return;
        }

        Debug.Log("[MyPlayerMaterialAssigner] === Parsing Shader Definition for Debug ===");
        var shaderEntries = ParseShaderEntries(shaderAsset.text);
        
        Debug.Log($"[MyPlayerMaterialAssigner] Gefunden: {shaderEntries.Count} Shader-Einträge");
        
        foreach (var entry in shaderEntries)
        {
            Debug.Log($"[MyPlayerMaterialAssigner] Shader: {entry.Key}");
            Debug.Log($"  - Hit Location: {entry.Value.HitLocation ?? "NULL"}");
            Debug.Log($"  - Editor Image: {entry.Value.EditorImage ?? "NULL"}");
            Debug.Log($"  - Cull Disabled: {entry.Value.CullDisabled}");
            Debug.Log($"  - Main Texture: {entry.Value.MainTexture ?? "NULL"}");
        }
    }

    #endregion
}
