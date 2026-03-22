using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Wendet Texturen auf Map-Elemente an, basierend auf Ghoul2Meta-Properties.
    /// Liest mapped_texture_0..N und löst Texturen über den TextureManager auf.
    /// </summary>
    public class MapTextureApplier
    {
        private const int k_MaxTextureSlots = 32; // Max Anzahl der Textur-Slots (mapped_texture_0..31)

        /// <summary>
        /// Prefix für die Textur-Properties in Ghoul2Meta.
        /// </summary>
        private const string k_MappedTexturePrefix = "mapped_texture_";

        /// <summary>
        /// URP Unlit Shader-Name für erzeugte Materialien.
        /// </summary>
        private const string k_ShaderName = "Universal Render Pipeline/Unlit";



        /// <summary>
        /// Prefix für Transparenz-Properties (is_transparent_0..is_transparent_N).
        /// </summary>
        private const string k_TransparentPrefix = "is_transparent_";

        /// <summary>
        /// Prefix für Surface-Type-Properties (surface_types_json_0..surface_types_json_N).
        /// </summary>
        private const string k_SurfaceTypesJsonPrefix = "surface_types_json_";

        /// <summary>
        /// Cache fuer Material-Varianten. Key = Textur-Key + Varianten-Flags (transparent).
        /// Alle Renderer mit gleicher Konfiguration teilen sich dasselbe Material.
        /// </summary>
        private readonly Dictionary<string, Material> m_MaterialCache = new();

        /// <summary>
        /// Gibt alle geklonten Material-Varianten frei und leert den Cache.
        /// Wird beim Map-Wechsel aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            foreach (KeyValuePair<string, Material> entry in m_MaterialCache)
            {
                // Nur Varianten-Klone zerstoeren, nicht die Basis-Materialien aus dem TextureManager
                if (entry.Value != null)
                {
                    Object.Destroy(entry.Value);
                }
            }

            m_MaterialCache.Clear();
        }

        /// <summary>
        /// Wendet Texturen auf alle Ghoul2Meta-Objekte in der Map-Instanz an.
        /// </summary>
        /// <param name="mapInstance">Die instanziierte Map.</param>
        public void ApplyTextures(GameObject mapInstance)
        {
            if (mapInstance == null)
            {
                return;
            }

            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                Debug.LogError("[MapTextureApplier] TextureManager not available via ServiceLocator.");
                return;
            }

            Renderer[] allRenderers = mapInstance.GetComponentsInChildren<Renderer>(true);
            int appliedCount = 0;

            foreach (Renderer renderer in allRenderers)
            {
                if (!renderer.TryGetComponent(out Ghoul2Meta meta))
                {
                    continue;
                }

                // Skybox-Surfaces: Renderer unsichtbar machen, Collider bleibt aktiv
                if (IsSkyboxSurface(meta))
                {
                    renderer.enabled = false;
                    continue;
                }

                // BSP-Brush-Volumes (COL_*N): keine Texturen anwenden,
                // werden vom MapColliderApplier als ShadowsOnly konfiguriert.
                if (IsBrushVolume(renderer.gameObject) || IsClipVolume(renderer.gameObject))
                {
                    continue;
                }

                List<(int slotIndex, string key)> textureSlots = CollectTextureKeys(meta);
                if (textureSlots.Count == 0)
                {
                    continue;
                }

                ApplyMaterials(renderer, textureSlots, meta, textureManager);
                appliedCount++;
            }

            Debug.Log($"[MapTextureApplier] Applied textures to {appliedCount} of {allRenderers.Length} Renderer objects.");
        }

        /// <summary>
        /// Sammelt alle Textur-Keys (mapped_texture_0..N) aus den Ghoul2Meta-Properties.
        /// Gibt Tuples (Original-Slot-Index, Textur-Key) zurück, damit cull_N und
        /// is_transparent_N den korrekten Slot referenzieren — auch bei Lücken.
        /// </summary>
        private List<(int slotIndex, string key)> CollectTextureKeys(Ghoul2Meta meta)
        {
            List<(int slotIndex, string key)> keys = new();

            for (int i = 0; i < k_MaxTextureSlots; i++)
            {
                string propertyName = k_MappedTexturePrefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    continue;
                }

                string value = meta.GetString(propertyName);
                if (!string.IsNullOrEmpty(value))
                {
                    keys.Add((i, value));
                }
            }

            return keys;
        }

        /// <summary>
        /// Erzeugt und setzt Materialien auf dem Renderer basierend auf den Textur-Slots.
        /// Wendet anschließend per-Slot Cull- und Transparenz-Properties an.
        /// Nutzt den Original-Slot-Index für cull_N / is_transparent_N Zuordnung.
        /// </summary>
        private void ApplyMaterials(Renderer renderer, List<(int slotIndex, string key)> textureSlots, Ghoul2Meta meta, TextureManager textureManager)
        {
            Material[] materials = new Material[textureSlots.Count];

            for (int i = 0; i < textureSlots.Count; i++)
            {
                int slotIndex = textureSlots[i].slotIndex;
                string key = textureSlots[i].key;

                bool isTransparent = HasTransparency(meta, slotIndex);
                string variantKey = BuildVariantKey(key, isTransparent);

                if (m_MaterialCache.TryGetValue(variantKey, out Material cached))
                {
                    materials[i] = cached;
                    continue;
                }

                Material baseMaterial = ResolveMaterial(key, textureManager);
                if (baseMaterial == null)
                {
                    Material[] existing = renderer.sharedMaterials;
                    materials[i] = i < existing.Length ? existing[i] : existing[0];
                    Debug.LogWarning($"[MapTextureApplier] No texture found for key '{key}' on '{renderer.gameObject.name}'.");
                    continue;
                }

                // Bei Varianten mit Flags: Klon erzeugen, damit das Original
                // im TextureManager/ResolveMaterial nicht mutiert wird.
                Material material = isTransparent
                    ? new Material(baseMaterial) { name = baseMaterial.name }
                    : baseMaterial;

                if (isTransparent)
                {
                    ApplyAlphaCutout(material);
                }

                m_MaterialCache[variantKey] = material;
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }

        /// <summary>
        /// Erzeugt einen eindeutigen Cache-Key fuer eine Material-Variante.
        /// </summary>
        private string BuildVariantKey(string textureKey, bool transparent)
        {
            return transparent ? $"{textureKey}|t" : textureKey;
        }

        /// <summary>
        /// Prueft ob der Slot als transparent markiert ist.
        /// </summary>
        private bool HasTransparency(Ghoul2Meta meta, int slotIndex)
        {
            string transparentKey = k_TransparentPrefix + slotIndex;
            if (!meta.HasProperty(transparentKey))
            {
                return false;
            }

            string transparentValue = meta.GetString(transparentKey);
            return transparentValue != "0" && !string.Equals(transparentValue, "false", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Erzwingt Render Face Both fuer URP-Materialien.
        /// </summary>
        private void SetRenderFaceBoth(Material material)
        {
            // URP Render Face = Both: _Cull auf 0 (Off) setzen
            material.SetFloat("_Cull", 0f);

            if (material.HasProperty("_CullMode"))
            {
                material.SetFloat("_CullMode", 0f);
            }

            // Shader-Pass fuer Double-Sided erzwingen
            material.doubleSidedGI = true;
        }

        /// <summary>
        /// Wendet Alpha-Cutout-Rendering auf ein Material an.
        /// </summary>
        private void ApplyAlphaCutout(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 1f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
                material.EnableKeyword("_ALPHATEST_ON");
            }

            if (material.HasProperty("_Cutoff"))
            {
                material.SetFloat("_Cutoff", 0.5f);
            }

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");

            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }

        /// <summary>
        /// Löst einen Textur-Key zu einem Material auf. Nutzt TextureManager-Cache oder erzeugt ein neues Material.
        /// </summary>
        private Material ResolveMaterial(string key, TextureManager textureManager)
        {
            TextureData textureData = textureManager.GetTextureData(key);

            if (textureData == null)
            {
                textureData = textureManager.GetTextureDataByAlias(key);
            }

            if (textureData == null || !textureData.IsValid())
            {
                return null;
            }

            // Material bereits im Cache vorhanden
            if (textureData.HasMaterial())
            {
                return textureData.Material;
            }

            // Textur laden und Material erzeugen
            if (!textureData.HasTexture())
            {
                return null;
            }

            Shader shader = Shader.Find(k_ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[MapTextureApplier] Shader '{k_ShaderName}' not found.");
                return null;
            }

            Material material = new(shader) { name = key };
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", textureData.Texture);
            }
            else
            {
                material.mainTexture = textureData.Texture;
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0f);
            }

            // SoF2/idTech3-Default: kein Backface-Culling (doppelseitig)
            SetRenderFaceBoth(material);

            return material;
        }

        /// <summary>
        /// Prüft ob dieser Renderer ein Skybox-Surface ist (surface_types_json_N enthält "sky").
        /// </summary>
        private bool IsSkyboxSurface(Ghoul2Meta meta)
        {
            for (int i = 0; i < k_MaxTextureSlots; i++)
            {
                string propertyName = k_SurfaceTypesJsonPrefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    continue;
                }

                string jsonValue = meta.GetString(propertyName);
                if (!string.IsNullOrEmpty(jsonValue) && jsonValue.Contains("sky", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Prueft ob das GameObject ein BSP-Brush-Volume ist (Name beginnt mit COL_ gefolgt von Ziffern).
        /// </summary>
        private bool IsBrushVolume(GameObject go)
        {
            string name = go.name;
            if (!name.StartsWith("COL_", System.StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 4; i < name.Length; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    return false;
                }
            }

            return name.Length > 4;
        }

        /// <summary>
        /// Prueft ob das GameObject ein Clip-Volume ist (Name: COL_*N_clip).
        /// </summary>
        private bool IsClipVolume(GameObject go)
        {
            string name = go.name;
            if (!name.StartsWith("COL_*", System.StringComparison.Ordinal) ||
                !name.EndsWith("_clip", System.StringComparison.Ordinal))
            {
                return false;
            }

            int digitStart = 5; // "COL_*".Length
            int digitEnd = name.Length - 5; // "_clip".Length
            if (digitEnd <= digitStart)
            {
                return false;
            }

            for (int i = digitStart; i < digitEnd; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
