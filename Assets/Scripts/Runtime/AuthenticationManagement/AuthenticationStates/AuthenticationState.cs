using Unity.DedicatedGameServerSample.Runtime.Core;

namespace Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Base class representing an authentication state.
    /// </summary>
    public abstract class AuthenticationState : State<AuthenticationManager>
    {
        public override abstract void Enter();

        public override abstract void Exit();

        public virtual void OnAuthenticationAttempt(string username, string password) { }

        public virtual void OnAuthenticationSuccess() { }

        public virtual void OnAuthenticationFailure(AuthenticationStatus status) { }

        public virtual void OnSessionExpired() { }

        public virtual void OnUserRequestedLogout() { }

        public virtual void OnTokenRefresh() { }
    }
}
