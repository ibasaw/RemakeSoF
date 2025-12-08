using Tolik.RemakeSoF.Runtime.Core;

namespace Tolik.RemakeSoF.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Base class representing an authentication state.
    /// </summary>
    public abstract class AuthenticationState : State<AuthenticationManager>
    {
        public override abstract void Enter();

        public override abstract void Exit();

        public void OnAuthenticationAttempt(string username, string password)
        {
            Manager.m_Authenticating.Configure(username, password);
            Manager.ChangeState(Manager.m_Authenticating);
        }

        public virtual void OnAuthenticationSuccess() { }

        public virtual void OnAuthenticationFailure(AuthenticationStatus status) { }

        public virtual void OnSessionExpired() { }

        public virtual void OnUserRequestedLogout() { }

        public virtual void OnTokenRefresh() { }
    }
}
