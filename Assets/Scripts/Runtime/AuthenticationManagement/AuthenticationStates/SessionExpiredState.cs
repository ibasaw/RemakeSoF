namespace Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Authentication state corresponding to when the user's session has expired.
    /// From this state we can transition back to UnauthenticatedState or attempt to re-authenticate.
    /// </summary>
    class SessionExpiredState : AuthenticationState
    {
        private AuthenticationStatus m_ExpireReason;

        public void Configure(AuthenticationStatus reason)
        {
            m_ExpireReason = reason;
        }

        public override void Enter()
        {
            var authEvent = new AuthenticationEvent { status = m_ExpireReason };
            Manager.EventManager.Broadcast(authEvent);
            Manager.EventManager.Broadcast(new SessionExpiredEvent());
        }

        public override void Exit() { }

        public override void OnAuthenticationAttempt(string username, string password)
        {
            Manager.m_Authenticating.Configure(username, password);
            Manager.ChangeState(Manager.m_Authenticating);
        }

        public override void OnUserRequestedLogout()
        {
            Manager.ChangeState(Manager.m_Unauthenticated);
        }
    }
}
