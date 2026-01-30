using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Loading state - skin is being loaded
    /// </summary>
    internal class PlayerSkinLoadingState : PlayerSkinState
    {
        private string m_LoadingSkinName;
        private string m_LoadingAnimatorName;

        public void Configure(string skinName, string animatorName)
        {
            m_LoadingSkinName = skinName;
            m_LoadingAnimatorName = animatorName;
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
                if (Manager.TryLoadAndApplySkin(m_LoadingSkinName, m_LoadingAnimatorName, out GameObject prefab))
                {
                    Debug.Log($"[PlayerSkinManager] Successfully loaded skin: {m_LoadingSkinName}");
                    Manager.OnSkinLoadSuccess(m_LoadingSkinName, prefab);
                }
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
