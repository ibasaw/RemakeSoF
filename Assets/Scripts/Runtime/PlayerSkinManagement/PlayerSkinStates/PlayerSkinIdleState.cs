using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Idle state - no skin loaded or waiting for request
    /// </summary>
    internal class PlayerSkinIdleState : PlayerSkinState, IPlayerSkinChangeHandler
    {
        public override void Enter()
        {
            Debug.Log("[PlayerSkinManager] Entered Idle state");
        }

        public override void Exit() { }

        public void OnSkinChangeRequested(string skinName)
        {
            Debug.Log($"[PlayerSkinManager] Skin change requested: {skinName}");
            Manager.m_Loading.Configure(skinName);
            Manager.ChangeState(Manager.m_Loading);
        }
    }
}
