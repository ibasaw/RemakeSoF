using Tolik.RemakeSoF.Runtime.Core;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
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
        void OnSkinChangeRequested(string skinName, string animatorName);
    }
}
