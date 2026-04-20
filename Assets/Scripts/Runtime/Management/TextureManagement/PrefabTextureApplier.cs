using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Statischer Helper zum Anwenden von Texturen auf instanziierte Prefabs.
    /// Nutzt Ghoul2Meta-Properties (mapped_texture_0..N) via TextureManager.
    /// Wird nach LoadPrefab + Instantiate aufgerufen (Shells, Projektile, Chunks).
    /// </summary>
    public static class PrefabTextureApplier
    {
        private const int k_MaxTextureSlots = 32;
        private const string k_MappedTexturePrefix = "mapped_texture_";
        private const string k_CullPrefix = "cull_";
        private const string k_TransparentPrefix = "is_transparent_";
        private const string k_ShaderName = "SoF2/MapSurface";

        /// <summary>
        /// Wendet Texturen auf alle Renderer im GameObject-Baum an via Ghoul2Meta (mapped_texture_0..N).
        /// </summary>
        /// <param name="instance">Das instanziierte Prefab-GameObject.</param>
        public static void ApplyTextures(GameObject instance)
        {
            ApplyTextures(instance, null);
        }

        /// <summary>
        /// Wendet Texturen auf alle Renderer im GameObject-Baum an.
        /// Primaer via Ghoul2Meta (mapped_texture_0..N).
        /// Fallback fuer Prefabs ohne Ghoul2Meta: Textur-Key wird aus dem Model-Pfad abgeleitet
        /// (ohne Dateiendung .glm/.md3) und via TextureManager mit LegacyShaderLoader-Fallback aufgeloest.
        /// </summary>
        /// <param name="instance">Das instanziierte Prefab-GameObject.</param>
        /// <param name="modelKey">Optionaler Addressable-Key / Model-Pfad fuer Fallback-Aufloesung.</param>
        public static void ApplyTextures(GameObject instance, string modelKey)
        {
            if (instance == null)
            {
                return;
            }

            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                return;
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            bool hasGhoul2 = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer.TryGetComponent(out Ghoul2Meta meta))
                {
                    hasGhoul2 = true;
                    List<(int slotIndex, string key)> textureSlots = CollectTextureKeys(meta);
                    if (textureSlots.Count > 0)
                    {
                        ApplyMaterialsFromGhoul2(renderer, textureSlots, meta, textureManager);
                    }
                }
            }

            // Fallback: Kein Ghoul2Meta vorhanden (z.B. Gore-Prefabs, einfache 3D-Modelle).
            // Textur-Key aus Model-Pfad ableiten und auf alle Renderer anwenden.
            if (!hasGhoul2 && !string.IsNullOrEmpty(modelKey))
            {
                ApplyTextureFromModelKey(renderers, modelKey, textureManager);
            }
        }

        /// <summary>
        /// Fallback-Textur-Anwendung fuer Prefabs ohne Ghoul2Meta.
        /// Leitet den Textur-Key aus dem Model-Pfad ab (ohne .glm/.md3 Endung)
        /// und loest ueber TextureManager + LegacyShaderLoader auf.
        /// </summary>
        private static void ApplyTextureFromModelKey(Renderer[] renderers, string modelKey, TextureManager textureManager)
        {
            // Textur-Key: Model-Pfad ohne Dateiendung (.glm/.md3)
            string textureKey = modelKey;
            int extensionIndex = textureKey.LastIndexOf('.');
            if (extensionIndex >= 0)
            {
                textureKey = textureKey.Substring(0, extensionIndex);
            }

            TextureData textureData = textureManager.GetTextureData(textureKey);
            if (textureData == null || !textureData.IsValid())
            {
                return;
            }

            Material material = textureData.Material;
            if (material == null && textureData.HasTexture())
            {
                Shader shader = Shader.Find(k_ShaderName);
                if (shader != null)
                {
                    material = new Material(shader) { name = textureKey };
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

                    // Doppelseitig (SoF2 Default fuer Gore-Pieces)
                    material.SetFloat("_Cull", 0f);
                    if (material.HasProperty("_CullMode"))
                    {
                        material.SetFloat("_CullMode", 0f);
                    }
                    material.doubleSidedGI = true;
                }
            }

            if (material == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Sammelt alle Textur-Keys (mapped_texture_0..N) aus den Ghoul2Meta-Properties.
        /// Gibt Tuples (Original-Slot-Index, Textur-Key) zurueck, damit cull_N und
        /// is_transparent_N den korrekten Slot referenzieren — auch bei Luecken.
        /// </summary>
        private static List<(int slotIndex, string key)> CollectTextureKeys(Ghoul2Meta meta)
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
        /// Erzeugt und setzt Materialien basierend auf Ghoul2Meta Textur-Keys.
        /// Wendet Cull- und Transparenz-Properties an (cull_N, is_transparent_N).
        /// Nutzt den Original-Slot-Index fuer korrekte Meta-Property-Zuordnung.
        /// </summary>
        private static void ApplyMaterialsFromGhoul2(Renderer renderer, List<(int slotIndex, string key)> textureSlots, Ghoul2Meta meta, TextureManager textureManager)
        {
            Material[] materials = new Material[textureSlots.Count];

            for (int i = 0; i < textureSlots.Count; i++)
            {
                int slotIndex = textureSlots[i].slotIndex;
                string key = textureSlots[i].key;
                bool isTransparent = HasTransparency(meta, slotIndex);

                Material baseMaterial = ResolveMaterial(key, textureManager);

                if (baseMaterial != null)
                {
                    // Bei Transparenz: Klon erzeugen, damit das Original nicht mutiert wird.
                    Material material = isTransparent ? new Material(baseMaterial) { name = baseMaterial.name } : baseMaterial;

                    if (isTransparent)
                    {
                        ApplyAlphaCutout(material);
                    }

                    materials[i] = material;
                }
                else
                {
                    Material[] existing = renderer.sharedMaterials;
                    materials[i] = i < existing.Length ? existing[i] : existing[0];
                }
            }

            // Alte Instanz-Materialien freigeben bevor neue zugewiesen werden
            Material[] oldMaterials = renderer.materials;
            foreach (Material oldMat in oldMaterials)
            {
                if (oldMat != null)
                {
                    Object.Destroy(oldMat);
                }
            }

            renderer.materials = materials;

            // Per-Slot Cull anwenden (mit Original-Slot-Index)
            Material[] assigned = renderer.materials;
            for (int i = 0; i < assigned.Length; i++)
            {
                ApplyCullProperty(assigned[i], meta, textureSlots[i].slotIndex);
            }
            renderer.materials = assigned;
        }

        /// <summary>
        /// Loest einen Textur-Key zu einem URP/Unlit-Material auf.
        /// Versucht zuerst direkten Key, dann Alias.
        /// </summary>
        private static Material ResolveMaterial(string key, TextureManager textureManager)
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

            if (textureData.HasMaterial())
            {
                return textureData.Material;
            }

            if (!textureData.HasTexture())
            {
                return null;
            }

            Shader shader = Shader.Find(k_ShaderName);
            if (shader == null)
            {
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

            if (material.HasProperty("_LightBlend"))
            {
                material.SetFloat("_LightBlend", 0.35f);
            }

            // SoF2/idTech3-Default: kein Backface-Culling (doppelseitig)
            material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_CullMode"))
            {
                material.SetFloat("_CullMode", 0f);
            }
            material.doubleSidedGI = true;

            return material;
        }

        /// <summary>
        /// Setzt Culling auf Off wenn Ghoul2Meta cull_N "disabled"/"off"/"disable" ist.
        /// </summary>
        private static void ApplyCullProperty(Material material, Ghoul2Meta meta, int index)
        {
            string cullKey = k_CullPrefix + index;
            if (!meta.HasProperty(cullKey))
            {
                return;
            }

            string cullValue = meta.GetString(cullKey);
            if (cullValue.Contains("disabled", System.StringComparison.OrdinalIgnoreCase) ||
                cullValue.Contains("off", System.StringComparison.OrdinalIgnoreCase) ||
                cullValue.Contains("disable", System.StringComparison.OrdinalIgnoreCase))
            {
                material.SetFloat("_Cull", 0f);
                if (material.HasProperty("_CullMode"))
                {
                    material.SetFloat("_CullMode", 0f);
                }
                material.doubleSidedGI = true;
            }
        }

        /// <summary>
        /// Prueft ob der Slot als transparent markiert ist (is_transparent_N).
        /// </summary>
        private static bool HasTransparency(Ghoul2Meta meta, int slotIndex)
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
        /// Wendet Alpha-Cutout-Rendering auf ein Material an.
        /// Identisch zu MapTextureApplier.ApplyAlphaCutout().
        /// </summary>
        private static void ApplyAlphaCutout(Material material)
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
    }
}
