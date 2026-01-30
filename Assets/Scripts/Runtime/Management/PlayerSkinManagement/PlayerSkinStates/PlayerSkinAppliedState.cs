using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Applied state - skin is loaded and ready to be applied to characters
    /// </summary>
    internal class PlayerSkinAppliedState : PlayerSkinState, IPlayerSkinChangeHandler
    {
        public override void Enter()
        {
            // Notify manager to broadcast skin applied event (best practice)
            Manager.OnSkinApplied();
        }

        public override void Exit() { }

        public void OnSkinChangeRequested(string skinName, string animatorName)
        {
            Debug.Log($"[PlayerSkinAppliedState] Changing to {skinName}");
            Manager.m_Loading.Configure(skinName, animatorName);
            Manager.ChangeState(Manager.m_Loading);
        }
    }
}
