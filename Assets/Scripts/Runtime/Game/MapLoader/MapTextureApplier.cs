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
        /// Prefix für Cull-Properties (cull_0..cull_N). Value "disabled" → kein Culling (beide Seiten).
        /// </summary>
        private const string k_CullPrefix = "cull_";

        /// <summary>
        /// Prefix für Transparenz-Properties (is_transparent_0..is_transparent_N).
        /// </summary>
        private const string k_TransparentPrefix = "is_transparent_";

        /// <summary>
        /// Prefix für Surface-Type-Properties (surface_types_json_0..surface_types_json_N).
        /// </summary>
        private const string k_SurfaceTypesJsonPrefix = "surface_types_json_";

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
                string key = textureSlots[i].key;
                Material material = ResolveMaterial(key, textureManager);

                if (material != null)
                {
                    materials[i] = material;
                }
                else
                {
                    // Bestehende Material beibehalten wenn keine Textur gefunden
                    Material[] existing = renderer.sharedMaterials;
                    materials[i] = i < existing.Length ? existing[i] : existing[0];
                    Debug.LogWarning($"[MapTextureApplier] No texture found for key '{key}' on '{renderer.gameObject.name}'.");
                }
            }

            renderer.materials = materials;

            // Per-Slot Cull und Transparenz anwenden (auf Material-Instanzen des Renderers).
            // renderer.materials gibt jedes Mal neue Kopien zurück — deshalb einmal holen,
            // modifizieren und wieder zuweisen.
            // WICHTIG: Original-Slot-Index nutzen, nicht den komprimierten List-Index.
            Material[] assignedMaterials = renderer.materials;
            for (int i = 0; i < assignedMaterials.Length; i++)
            {
                int slotIndex = textureSlots[i].slotIndex;
                ApplyCullProperty(assignedMaterials[i], meta, slotIndex);
                ApplyTransparencyProperty(assignedMaterials[i], meta, slotIndex);
            }
            renderer.materials = assignedMaterials;
        }

        /// <summary>
        /// Prüft cull_N Property und setzt Culling auf Off (beide Seiten) wenn "disabled", "off" oder "disable".
        /// </summary>
        private void ApplyCullProperty(Material material, Ghoul2Meta meta, int index)
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
                // URP Render Face = Both: _Cull auf 0 (Off) setzen
                material.SetFloat("_Cull", 0f);

                if (material.HasProperty("_CullMode"))
                {
                    material.SetFloat("_CullMode", 0f);
                }

                // Shader-Pass für Double-Sided erzwingen
                material.doubleSidedGI = true;
            }
        }

        /// <summary>
        /// Prüft is_transparent_N Property und setzt das Material auf Transparent-Rendering.
        /// Synchronisiert alle URP Shader Keywords und Render States.
        /// </summary>
        private void ApplyTransparencyProperty(Material material, Ghoul2Meta meta, int index)
        {
            string transparentKey = k_TransparentPrefix + index;
            if (!meta.HasProperty(transparentKey))
            {
                return;
            }

            // Wert prüfen: nur anwenden wenn nicht explizit "0" oder "false"
            string transparentValue = meta.GetString(transparentKey);
            if (transparentValue == "0" || string.Equals(transparentValue, "false", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Surface Type: Transparent
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            // Blend Mode: Alpha (SrcAlpha, OneMinusSrcAlpha)
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            // ZWrite aus für Transparenz
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            // Render Face = Both für Transparenz (Rückseite sichtbar)
            material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_CullMode"))
            {
                material.SetFloat("_CullMode", 0f);
            }
            material.doubleSidedGI = true;

            // Alpha Clipping
            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
                material.EnableKeyword("_ALPHATEST_ON");
            }

            if (material.HasProperty("_Cutoff"))
            {
                material.SetFloat("_Cutoff", 0.5f);
            }

            // URP Shader Keywords synchronisieren
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
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
    }
}
