namespace Tolik.RemakeSoF.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Authentication state corresponding to when the user is not authenticated.
    /// From this state we can transition to the AuthenticatingState when attempting to login.
    /// </summary>
    class UnauthenticatedState : AuthenticationState
    {
        public override void Enter()
        {
            Manager.EventManager.Broadcast(new UserUnauthenticatedEvent { });
        }

        public override void Exit() { }
    }
}
