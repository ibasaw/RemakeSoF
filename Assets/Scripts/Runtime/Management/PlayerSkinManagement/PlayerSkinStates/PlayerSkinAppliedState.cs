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
            Debug.Log($"[PlayerSkinManager] Entered Applied state - skin ready: {Manager.CurrentSkinName}");
            // Broadcast skin changed event
            Manager.EventManager.Broadcast(new PlayerSkinChangedEvent
            {
                skinName = Manager.CurrentSkinName,
                playerPrefab = Manager.CurrentPlayerPrefab
            });
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
