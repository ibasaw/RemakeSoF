using System;

namespace Unity.DedicatedGameServerSample.Runtime.Core
{
    /// <summary>
    /// Generic base class for states used by a manager class.
    /// TManager is the type of the owning manager (e.g. ConnectionManager, AuthenticationManager)
    /// </summary>
    public abstract class State<TManager>
    {
        public TManager Manager { get; set; }

        public abstract void Enter();

        public abstract void Exit();

    }
}
