using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Error state - skin loading failed
    /// </summary>
    internal class PlayerSkinErrorState : PlayerSkinState
    {
        public override void Enter()
        {
            Debug.LogWarning("[PlayerSkinManager] Entered Error state");
        }

        public override void Exit() { }

        public void OnSkinChangeRequested(string skinName)
        {
            // Allow retry from error state
            Debug.Log("[PlayerSkinManager] Retrying skin load after error");
            Manager.m_Loading.Configure(skinName);
            Manager.ChangeState(Manager.m_Loading);
        }
    }
}
