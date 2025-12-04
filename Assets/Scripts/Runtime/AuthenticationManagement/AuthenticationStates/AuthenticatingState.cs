namespace Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Authentication state corresponding to when the user is attempting to authenticate.
    /// From this state we can transition to AuthenticatedState on success or back to UnauthenticatedState on failure.
    /// </summary>
    class AuthenticatingState : AuthenticationState
    {
        private string m_Username;
        private string m_Password;

        public void Configure(string username, string password)
        {
            m_Username = username;
            m_Password = password;
        }

        public override void Enter()
        {
            var authEvent = new AuthenticationEvent { status = AuthenticationStatus.Authenticating };
            Manager.EventManager.Broadcast(authEvent);
            
            // Here you would typically call your authentication service
            // For now, this is a placeholder
        }

        public override void Exit() { }

        public override void OnAuthenticationSuccess()
        {
            Manager.ChangeState(Manager.m_Authenticated);
        }

        public override void OnAuthenticationFailure(AuthenticationStatus status)
        {
            var authEvent = new AuthenticationEvent { status = status };
            Manager.EventManager.Broadcast(authEvent);
            Manager.ChangeState(Manager.m_Unauthenticated);
        }
    }
}
