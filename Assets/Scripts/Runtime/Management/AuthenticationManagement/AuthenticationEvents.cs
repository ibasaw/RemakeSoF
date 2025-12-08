namespace Tolik.RemakeSoF.Runtime.AuthenticationManagement
{
    public enum AuthenticationStatus
    {
        /// <summary>
        /// Status is not defined. This likely means an unexpected error occurred.
        /// </summary>
        Undefined,
        /// <summary>
        /// User is attempting to authenticate.
        /// </summary>
        Authenticating,
        /// <summary>
        /// User successfully authenticated.
        /// </summary>
        Success,
        /// <summary>
        /// Invalid credentials provided.
        /// </summary>
        InvalidCredentials,
        /// <summary>
        /// User account not found.
        /// </summary>
        UserNotFound,
        /// <summary>
        /// User account is disabled or locked.
        /// </summary>
        AccountDisabled,
        /// <summary>
        /// Session has expired.
        /// </summary>
        SessionExpired,
        /// <summary>
        /// User initiated logout.
        /// </summary>
        UserRequestedLogout,
        /// <summary>
        /// Network error during authentication.
        /// </summary>
        NetworkError,
        /// <summary>
        /// Server error during authentication.
        /// </summary>
        ServerError
    }

    public class AuthenticationEvent : AppEvent
    {
        public AuthenticationStatus status;
    }

    public class UserAuthenticatedEvent : AppEvent
    {
        public AuthenticationResponse AuthResponse { get; }

        public UserAuthenticatedEvent(AuthenticationResponse response)
        {
            AuthResponse = response;
        }
    }

    public class UserUnauthenticatedEvent : AppEvent { }

    public class SessionExpiredEvent : AppEvent { }
}
