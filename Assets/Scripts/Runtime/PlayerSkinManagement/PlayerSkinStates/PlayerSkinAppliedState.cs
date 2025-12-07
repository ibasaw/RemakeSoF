using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Applied state - skin is loaded and ready to be applied to characters
    /// </summary>
    internal class PlayerSkinAppliedState : PlayerSkinState, IPlayerSkinChangeHandler
    {
        public override void Enter()
        {
            Debug.Log($"[PlayerSkinManager] Entered Applied state - skin ready: {Manager.CurrentSkinName}");
        }

        public override void Exit() { }

        public void OnSkinChangeRequested(string skinName)
        {
            Debug.Log($"[PlayerSkinManager] Changing from {Manager.CurrentSkinName} to {skinName}");
            Manager.ChangeState(Manager.m_Loading);
            
            if (Manager.m_Loading is IPlayerSkinChangeHandler handler)
            {
                handler.OnSkinChangeRequested(skinName);
            }
        }
    }
}
