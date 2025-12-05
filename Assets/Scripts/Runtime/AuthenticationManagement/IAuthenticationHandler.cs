namespace Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Interface for states that handle authentication attempts.
    /// </summary>
    public interface IAuthenticationHandler
    {
        void OnAuthenticationAttempt(string username, string password);
    }
}