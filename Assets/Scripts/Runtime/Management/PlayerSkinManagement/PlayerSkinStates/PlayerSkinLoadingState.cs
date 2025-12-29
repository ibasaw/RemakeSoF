using System;
using System.Collections.Generic;
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
            try
            {
                // Validate skin exists
                if (string.IsNullOrEmpty(m_LoadingSkinName))
                {
                    Manager.OnSkinLoadFailure("Skin name is empty", PlayerSkinStatus.InvalidSkinName);
                    return;
                }
                // Try to load skin data from Registry
                SkinDefinition skinDefinition = Manager.PlayerSkinDataRegistry.GetSkinByName(m_LoadingSkinName);
                if (skinDefinition == null)
                {
                    Manager.OnSkinLoadFailure($"Skin definition file not found: {m_LoadingSkinName}", PlayerSkinStatus.SkinNotFound);
                    return;
                }

                string modelName = skinDefinition.GetModelName();
                if (string.IsNullOrEmpty(modelName))
                {
                    Manager.OnSkinLoadFailure($"No valid model name found in skin definition file: {m_LoadingSkinName}",
                    PlayerSkinStatus.ModelNameParseError);
                    return;
                }

                // Load prefab through manager
                string prefabPath = $"characters/models/{modelName}";
                GameObject prefab = Manager.LoadPrefabForModel(prefabPath);
                if (prefab == null)
                {
                    Manager.OnSkinLoadFailure($"Failed to load prefab for model: {modelName}", PlayerSkinStatus.PrefabNotFound);
                    return;
                }
                RuntimeAnimatorController controller = GetControllerWithClip("models/animator/loadout_preview");
                if (!prefab.TryGetComponent<Animator>(out var animator))
                {
                    animator = prefab.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[PlayerSkinManager] Added Animator component to prefab for preview animation.");
                }
                else
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[PlayerSkinManager] Assigned controller to existing Animator for preview animation.");
                }
                Manager.SetCurrentPlayerPrefab(prefab);

                // Reset GameObjects through manager
                Manager.ResetAllRenderersInCurrentPlayerPrefabToActive();

                // Load shader definition file and create materials for it
                Dictionary<string, ShaderEntry> shaderDefinition = Manager.PlayerSkinDataRegistry.GetLegacyShaderDefinitionForModel(modelName);
                Manager.CreateMaterialsFromSkinDefinition(m_LoadingSkinName, shaderDefinition, skinDefinition);

                Manager.DisableAndEnableSurfacesForCurrentPlayerPrefab(skinDefinition);
                Manager.ApplyMaterialsToCurrentPlayerPrefab(m_LoadingSkinName, skinDefinition);

                Debug.Log($"[PlayerSkinManager] Successfully loaded skin: {m_LoadingSkinName}.json with model: {modelName}.shader");
                Manager.OnSkinLoadSuccess(m_LoadingSkinName, prefab);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerSkinManager] Error loading skin: {ex.Message}");
                Manager.OnSkinLoadFailure($"Error loading skin: {ex.Message}", PlayerSkinStatus.GenericError);
                return;
            }
        }
        public RuntimeAnimatorController GetControllerWithClip(string name = "models/animator/loadout_preview")
        {
            var controller = new AnimatorOverrideController();
            RuntimeAnimatorController baseController = Manager.LoadAnimatorForModel(name);
            controller.runtimeAnimatorController = baseController;
            return controller;
        }

        public override void OnSkinLoadSuccess()
        {
            Manager.ChangeState(Manager.m_Applied);
        }
    }
}
