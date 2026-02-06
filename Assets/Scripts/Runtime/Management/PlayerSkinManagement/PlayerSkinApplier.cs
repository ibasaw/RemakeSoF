using System;
using System.Collections.Generic;
using System.Linq;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Internal service responsible for applying materials and managing renderer state on player prefabs
    /// Pure asset application service without data loading dependencies
    /// </summary>
    internal class PlayerSkinApplier
    {
        public void CreateMaterials(string skinName, SkinDefinition skinDefinition, Dictionary<string, Dictionary<string, ShaderEntry>> allShaderDefinitions)
        {
            if (skinDefinition?.materials == null || skinDefinition.materials.Count == 0)
            {
                Debug.LogError($"[PlayerSkinApplier] Keine 'materials' in skinDefinition JSON vorhanden: {skinName}");
                return;
            }

            ServiceLocator.Get<TextureManager>().CreateMaterialsFromSkinDefinition(allShaderDefinitions, skinDefinition);
        }

        public void ApplyAnimatorController(GameObject prefab, string animatorName)
        {
            RuntimeAnimatorController controller = CreateAnimatorController(animatorName);
            if (!prefab.TryGetComponent(out Animator animator))
            {
                animator = prefab.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
        }

        private RuntimeAnimatorController CreateAnimatorController(string name)
        {
            AnimatorOverrideController controller = new();
            RuntimeAnimatorController baseController = ServiceLocator.Get<PrefabManager>().LoadPrefab<RuntimeAnimatorController>(name);
            controller.runtimeAnimatorController = baseController;
            return controller;
        }

        public void ApplySurfaceDefinitions(GameObject prefab, string skinName, string modelName, SkinDefinition skinDefinition, SkinSurfaceDefinition surfaceDefinitions)
        {
            if (surfaceDefinitions == null)
            {
                Debug.LogWarning($"[PlayerSkinApplier] No surface definitions found for model: {modelName}");
                return;
            }

            Renderer[] allRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (KeyValuePair<string, SkinSurfacePartDefinition> part in surfaceDefinitions.parts)
            {
                string partName = part.Key;
                var surfaces = part.Value.material;
                string textureKey = skinDefinition.GetTextureOrShadowForMaterialDefinitionName(partName);
                if (string.IsNullOrEmpty(textureKey))
                {
                    Debug.LogWarning($"[PlayerSkinApplier] No texture/shader-key found for material definition name: '{partName}' in skin definition: '{skinName}'");
                    continue;
                }

                TextureData textureData = ServiceLocator.Get<TextureManager>().GetTextureData(textureKey);
                if (textureData == null || !textureData.IsValid())
                {
                    Debug.Log($"[PlayerSkinApplier] Search by TextureData by alias for key: '{textureKey}' in skin definition: '{skinName}'");
                    textureData = ServiceLocator.Get<TextureManager>().GetTextureDataByAlias(textureKey);
                }

                if(textureData == null || !textureData.IsValid())
                {
                    Debug.LogError($"[PlayerSkinApplier] No valid TextureData found for key: '{textureKey}' in skin definition: '{skinName}'");
                    continue;
                }

                Debug.Log($"[PlayerSkinApplier] TextureData for key '{textureKey}': {textureData}");
                Debug.Log($"[PlayerSkinApplier] [{modelName} , {skinName}] Processing part '{partName}' with texture/shader: {textureKey}");

                foreach (string surfaceName in surfaces)
                {
                    List<Renderer> matches = FindRenderersForSurface(allRenderers, surfaceName);
                    Debug.Log($"[PlayerSkinApplier] Found {matches.Count} renderers for surface '{surfaceName}' in part '{partName} for model '{modelName}'");
                    foreach (var renderer in matches)
                    {
                        ApplyMaterialToRenderer(renderer, textureData.Material, surfaceName);
                        Debug.Log($"[PlayerSkinApplier] Applied material to renderer '{renderer.gameObject.name}' for surface '{surfaceName}' in part '{partName}'");
                    }
                }
            }
        }

        private bool ApplyMaterialToRenderer(Renderer renderer, Material material, string surfaceName)
        {
            var shared = renderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                renderer.material = material;
                return true;
            }

            if (shared.Length == 1)
            {
                renderer.material = material;
                return true;
            }

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
                newMats[0] = material;
                renderer.materials = newMats;
                return true;
            }
        }

        private List<Renderer> FindRenderersForSurface(Renderer[] allRenderers, string surfaceName)
        {
            var list = new List<Renderer>();
            if (string.IsNullOrEmpty(surfaceName)) return list;

            // 1) exact match by GameObject name
            foreach (var r in allRenderers)
            {
                if (string.Equals(r.gameObject.name, surfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(r);
                    Debug.Log($"[PlayerSkinApplier] Exact match found: '{r.gameObject.name}'");
                }
            }
            if (list.Count > 0) return list;

            // 2) exact match by GameObject name without suffix
            foreach (var r in allRenderers)
            {
                string rendererName = r.gameObject.name;
                string cleanRendererName = System.Text.RegularExpressions.Regex.Replace(rendererName, @"_\d+$", "");

                if (string.Equals(cleanRendererName, surfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(r);
                }
            }
            if (list.Count > 0) return list;

            // 3) check shared material base names
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
                        Debug.Log($"[PlayerSkinApplier] Material name match found: '{r.gameObject.name}' (material: '{s.name}')");
                        break;
                    }
                }
            }
            if (list.Count > 0) return list;

            // 4) More specific contains matching
            foreach (var r in allRenderers)
            {
                string rendererName = r.gameObject.name;

                if (surfaceName.Length >= rendererName.Length * 0.5f &&
                    rendererName.IndexOf(surfaceName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add(r);
                    Debug.Log($"[PlayerSkinApplier] Significant contains match found: '{r.gameObject.name}' contains '{surfaceName}'");
                }
            }
            if (list.Count > 0) return list;

            if (list.Count == 0)
            {
                Debug.LogWarning($"[PlayerSkinApplier] No renderer found for surface: '{surfaceName}'");
            }

            return list.Distinct().ToList();
        }

        public void ResetAllRenderersToActive(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[PlayerSkinApplier] Cannot reset Renderers - no prefab provided");
                return;
            }

            var allRenderers = prefab.GetComponentsInChildren<Renderer>(true);

            int activatedCount = 0;
            foreach (var renderer in allRenderers)
            {
                if (!renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(true);
                    activatedCount++;
                }
            }

            Debug.Log($"[PlayerSkinApplier] Reset {activatedCount} GameObjects to active");
        }

        public void DisableAndEnableSurfaces(GameObject prefab, List<CharacterTemplate> characterTemplates, string skinName)
        {
            List<InventoryItem> allItems = new();

            foreach (CharacterTemplate template in characterTemplates)
            {
                // Sammle Items aus dem CharacterTemplate Inventory
                if (template.Inventory != null && template.Inventory.Items != null)
                {
                    allItems.AddRange(template.Inventory.Items);
                    Debug.Log($"[PlayerSkinApplier] Found {template.Inventory.Items.Count} items in CharacterTemplate '{template.Name}' Inventory");
                }

                // Sammle Items aus allen SkinTemplates
                if (template.SkinTemplates != null)
                {
                    foreach (SkinTemplate skinTemplate in template.SkinTemplates)
                    {
                        if (skinTemplate.Inventory != null && skinTemplate.Inventory.Items != null && skinTemplate.SkinName == skinName)
                        {
                            allItems.AddRange(skinTemplate.Inventory.Items);
                            Debug.Log($"[PlayerSkinApplier] Found {skinTemplate.Inventory.Items.Count} items in SkinTemplate '{skinTemplate.SkinName}' Inventory");
                        }
                    }
                }

                //TODO: anatoli - falls parent templates gebraucht werden:
                //templatesByName.TryGetValue(template.ParentTemplate, out CharacterTemplate parentTemplate);
                //Debug.Log($"[PlayerSkinApplier] ParentTemplate '{parentTemplate?.Name}' for CharacterTemplate '{template.Name}'");
            }

            // TODO: Hier mit den gesammelten Items arbeiten
            allItems = allItems.GroupBy(i => i.Name).Select(g => g.First()).ToList();
            Debug.Log($"[PlayerSkinApplier] Total items collected: {allItems.Count}");
            ItemDefinitionLoader itemLoader = ServiceLocator.Get<ItemDefinitionLoader>();
            //map collected items to item definitions and collect as list of item definitions to apply
            List<Item> itemDefinitionsToApply = new();
            foreach (InventoryItem inventoryItem in allItems)
            {
                if(inventoryItem.Name == null){
                    Debug.LogWarning($"[PlayerSkinApplier] InventoryItem with null name found in CharacterTemplates/SkinTemplates, skipping.");
                    continue;
                }
                Item itemDef = itemLoader.GetByName(inventoryItem.Name);
                if (itemDef != null)
                {
                    itemDefinitionsToApply.Add(itemDef);
                    Debug.Log($"[PlayerSkinApplier] Mapped InventoryItem '{inventoryItem.Name}' to Item definition '{itemDef.name}'");
                }
                else
                {
                    Debug.LogWarning($"[PlayerSkinApplier] No Item definition found for InventoryItem '{inventoryItem.Name}'");
                }
            }

            Renderer[] allRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in allRenderers)
            {
                // check if renderer name equals any item defs activeSurfaces(list) or inactiveSurfaces and set active/deactivate accordingly 
                string rendererName = renderer.gameObject.name;
                string cleanRendererName = System.Text.RegularExpressions.Regex.Replace(rendererName, @"_\d+$", "");
                foreach (Item itemDef in itemDefinitionsToApply)
                {
                    if (itemDef.activeSurfaces != null && itemDef.activeSurfaces.Contains(cleanRendererName))
                    {
                        renderer.gameObject.SetActive(true);
                        Debug.Log($"[PlayerSkinApplier] Activated renderer '{renderer.gameObject.name}' for item '{itemDef.name}' active surface '{cleanRendererName}'");
                    }
                    if (itemDef.inactiveSurfaces != null && itemDef.inactiveSurfaces.Contains(cleanRendererName))
                    {
                        renderer.gameObject.SetActive(false);
                        Debug.Log($"[PlayerSkinApplier] Deactivated renderer '{renderer.gameObject.name}' for item '{itemDef.name}' inactive surface '{cleanRendererName}'");
                    }
                }

            }
        }

        public void DisableAndEnableSurfaces(GameObject prefab, SkinDefinition skinDefinition)
        {
            var allRenderers = prefab.GetComponentsInChildren<Renderer>(true);
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
                if (skinDefinition != null && skinDefinition.prefs != null)
                {
                    string rendererName = renderer.gameObject.name;
                    string cleanRendererName = System.Text.RegularExpressions.Regex.Replace(rendererName, @"_\d+$", "");
                    if (skinDefinition.prefs.surfaces_on != null)
                    {
                        if (skinDefinition.prefs.surfaces_on.ContainsValue(cleanRendererName))
                        {
                            renderer.gameObject.SetActive(true);
                        }
                    }
                    if (skinDefinition.prefs.surfaces_off != null)
                    {
                        if (skinDefinition.prefs.surfaces_off.ContainsValue(cleanRendererName))
                        {
                            renderer.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }
}
