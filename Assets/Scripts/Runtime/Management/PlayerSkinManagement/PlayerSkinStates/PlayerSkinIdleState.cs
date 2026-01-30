using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
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

        public void OnSkinChangeRequested(string skinName, string animatorName)
        {
            Debug.Log($"[PlayerSkinManager] Skin change requested: {skinName}");
            Manager.m_Loading.Configure(skinName, animatorName);
            Manager.ChangeState(Manager.m_Loading);
        }
    }
}
