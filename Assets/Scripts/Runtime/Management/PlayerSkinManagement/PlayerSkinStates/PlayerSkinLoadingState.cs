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
                GameObject prefab = Manager.LoadPrefabForModel(modelName);
                if (prefab == null)
                {
                    Manager.OnSkinLoadFailure($"Failed to load prefab for model: {modelName}", PlayerSkinStatus.PrefabNotFound);
                    return;
                }
                Manager.SetCurrentPlayerPrefab(prefab);

                // Reset GameObjects through manager
                Manager.ResetAllRenderersInCurrentPlayerPrefabToActive();

                // Load shader definition file and create materials for it
                //TODO das muss eventuell preloaded zum start werden anstatt zur laufzeit
                //Dictionary<string, ShaderEntry> shaderDefinition = Manager.LoadLegacyShaderDefinitionForModel($"Data/shaders/{modelName}");
                //Manager.CreateMaterialsForShaderDefinition(shaderDefinition);
                //Manager.CreateMaterialsFromSkinDefinition(m_LoadingSkinName, skinDefinition);
                Debug.Log($"[PlayerSkinManager] Successfully loaded skin: {m_LoadingSkinName}.json with model: {modelName}.shader");

                Manager.DisableAndEnableSurfacesForCurrentPlayerPrefab(skinDefinition);
                //TODO apply materials to playerprefab

                Manager.OnSkinLoadSuccess(m_LoadingSkinName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerSkinManager] Error loading skin: {ex.Message}");
                Manager.OnSkinLoadFailure($"Error loading skin: {ex.Message}", PlayerSkinStatus.GenericError);
                return;
            }
        }

        public override void OnSkinLoadSuccess()
        {
            Manager.ChangeState(Manager.m_Applied);
        }
    }
}
