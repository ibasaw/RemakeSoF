namespace Tolik.RemakeSoF.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Authentication state corresponding to when the user is successfully authenticated.
    /// From this state we can transition to UnauthenticatedState on logout or SessionExpiredState if session expires.
    /// </summary>
    class AuthenticatedState : AuthenticationState, IAuthenticationHandler
    {
        private AuthenticationResponse m_AuthResponse;

        public void Configure(AuthenticationResponse response)
        {
            m_AuthResponse = response;
        }

        public override void Enter()
        {
            var authEvent = new AuthenticationEvent { status = AuthenticationStatus.Success };
            Manager.EventManager.Broadcast(authEvent);
            Manager.EventManager.Broadcast(new UserAuthenticatedEvent(m_AuthResponse));
        }

        public override void Exit() { }

        public override void OnUserRequestedLogout()
        {
            Manager.ChangeState(Manager.m_Unauthenticated);
        }

        public override void OnSessionExpired()
        {
            Manager.m_SessionExpired.Configure(AuthenticationStatus.SessionExpired);
            Manager.ChangeState(Manager.m_SessionExpired);
        }

        public override void OnTokenRefresh()
        {
            // Implement token refresh logic here
            // If refresh succeeds, remain in authenticated state
            // If refresh fails, transition to SessionExpiredState
        }
    }
}
