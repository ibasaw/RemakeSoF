using Unity.DedicatedGameServerSample.Runtime.Core;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Base state for player skin management
    /// </summary>
    public abstract class PlayerSkinState : State<PlayerSkinManager>
    {
        public override abstract void Enter();
        
        public override abstract void Exit();
        
        public virtual void OnSkinLoadSuccess() { }
        
        public virtual void OnSkinLoadFailure(string error, PlayerSkinStatus status) { }
    }

    /// <summary>
    /// Handler interface for skin change requests
    /// </summary>
    internal interface IPlayerSkinChangeHandler
    {
        void OnSkinChangeRequested(string skinName);
    }

    /// <summary>
    /// Handler interface for applying skins to characters
    /// </summary>
    internal interface IPlayerSkinApplicationHandler
    {
        void OnApplySkinToCharacter(GameObject character, string skinName);
    }
}
