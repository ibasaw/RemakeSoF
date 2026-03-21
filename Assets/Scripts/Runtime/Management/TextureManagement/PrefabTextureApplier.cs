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
        private const string k_ShaderName = "Universal Render Pipeline/Unlit";

        /// <summary>
        /// Wendet Texturen auf alle Renderer im GameObject-Baum an via Ghoul2Meta (mapped_texture_0..N).
        /// </summary>
        /// <param name="instance">Das instanziierte Prefab-GameObject.</param>
        public static void ApplyTextures(GameObject instance)
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

            foreach (Renderer renderer in renderers)
            {
                if (renderer.TryGetComponent(out Ghoul2Meta meta))
                {
                    List<string> textureKeys = CollectTextureKeys(meta);
                    if (textureKeys.Count > 0)
                    {
                        ApplyMaterialsFromGhoul2(renderer, textureKeys, meta, textureManager);
                    }
                }
            }
        }

        /// <summary>
        /// Sammelt alle Textur-Keys (mapped_texture_0..N) aus den Ghoul2Meta-Properties.
        /// </summary>
        private static List<string> CollectTextureKeys(Ghoul2Meta meta)
        {
            List<string> keys = new();

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
                    keys.Add(value);
                }
            }

            return keys;
        }

        /// <summary>
        /// Erzeugt und setzt Materialien basierend auf Ghoul2Meta Textur-Keys.
        /// Wendet Cull-Properties an (cull_0..N).
        /// </summary>
        private static void ApplyMaterialsFromGhoul2(Renderer renderer, List<string> textureKeys, Ghoul2Meta meta, TextureManager textureManager)
        {
            Material[] materials = new Material[textureKeys.Count];

            for (int i = 0; i < textureKeys.Count; i++)
            {
                Material material = ResolveMaterial(textureKeys[i], textureManager);

                if (material != null)
                {
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

            // Per-Slot Cull anwenden
            Material[] assigned = renderer.materials;
            for (int i = 0; i < assigned.Length; i++)
            {
                ApplyCullProperty(assigned[i], meta, i);
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
    }
}
