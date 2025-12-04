using System;
using System.Collections.Generic;
using Unity.DedicatedGameServerSample.Runtime.Core;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement
{

    /// <summary>
    /// This state machine handles authentication. It is responsible for managing authentication state transitions
    /// and redirecting calls to the current AuthenticationState object.
    /// </summary>
    public class AuthenticationManager : StateMachine<AuthenticationState, AuthenticationManager>
    {
        internal readonly UnauthenticatedState m_Unauthenticated = new();
        internal readonly AuthenticatingState m_Authenticating = new();
        internal readonly AuthenticatedState m_Authenticated = new();
        internal readonly SessionExpiredState m_SessionExpired = new();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<AuthenticationState> states = new() { m_Unauthenticated, m_Authenticating, m_Authenticated, m_SessionExpired };
            InitializeStates(states, m_Unauthenticated);
        }

        /// <summary>
        /// Attempt to authenticate with the provided credentials.
        /// </summary>
        public void Authenticate(string username, string password)
        {
            m_CurrentState.OnAuthenticationAttempt(username, password);
        }

        /// <summary>
        /// Notify the manager that authentication was successful.
        /// </summary>
        public void OnAuthenticationSuccess()
        {
            m_CurrentState.OnAuthenticationSuccess();
        }

        /// <summary>
        /// Notify the manager that authentication failed with a specific status.
        /// </summary>
        public void OnAuthenticationFailure(AuthenticationStatus status)
        {
            m_CurrentState.OnAuthenticationFailure(status);
        }

        /// <summary>
        /// Notify the manager that the session has expired.
        /// </summary>
        public void OnSessionExpired()
        {
            m_CurrentState.OnSessionExpired();
        }

        /// <summary>
        /// Request logout.
        /// </summary>
        public void Logout()
        {
            m_CurrentState.OnUserRequestedLogout();
        }

        /// <summary>
        /// Attempt to refresh the authentication token.
        /// </summary>
        public void RefreshToken()
        {
            m_CurrentState.OnTokenRefresh();
        }
    }
}
