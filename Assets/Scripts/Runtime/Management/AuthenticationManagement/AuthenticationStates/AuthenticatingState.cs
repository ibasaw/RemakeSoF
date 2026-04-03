using System;
using System.Threading.Tasks;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Authentication state corresponding to when the user is attempting to authenticate.
    /// From this state we can transition to AuthenticatedState on success or back to
    /// UnauthenticatedState on failure.
    /// Delegiert HTTP-Kommunikation an MasterServerService.
    /// </summary>
    class AuthenticatingState : AuthenticationState
    {
        string m_Username;
        string m_Password;

        public void Configure(string username, string password)
        {
            m_Username = username;
            m_Password = password;
        }
        private async Task MockAuthenticationProcess()
        {
            await Task.Delay(2000); // Simulate network delay

            var response = new AuthenticationResponse
            {
                message = "Authentication successful",
                token = "mock-jwt-token",
                playerId = "123-playerID-123542352335325",
                username = m_Username,
                selectedSkinName = "mullins_jungle"
            };
            Manager.m_Authenticated.Configure(response);
            Manager.OnAuthenticationSuccess();
        }

        public override async void Enter()
        {
            AuthenticationEvent authEvent = new() { status = AuthenticationStatus.Authenticating };
            Manager.EventManager.Broadcast(authEvent);

            //await LoginViaMasterServer(m_Username, m_Password);
            await MockAuthenticationProcess();
        }

        public override void Exit() { }

        public override void OnAuthenticationSuccess()
        {
            Manager.ChangeState(Manager.m_Authenticated);
        }

        public override void OnAuthenticationFailure(AuthenticationStatus status)
        {
            AuthenticationEvent authEvent = new() { status = status };
            Manager.EventManager.Broadcast(authEvent);
            Manager.ChangeState(Manager.m_Unauthenticated);
        }

        /// <summary>
        /// Delegiert den Login an den MasterServerService und verarbeitet das Ergebnis.
        /// </summary>
        async Task LoginViaMasterServer(string username, string password)
        {
            MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
            if (masterService == null)
            {
                Debug.LogError("[AuthenticatingState] MasterServerService nicht im ServiceLocator registriert!");
                OnAuthenticationFailure(AuthenticationStatus.ServerError);
                return;
            }

            try
            {
                MasterServerAuthResult result = await masterService.LoginUserAsync(username, password);

                if (result.IsSuccess)
                {
                    Manager.m_Authenticated.Configure(result.Response);
                    Manager.OnAuthenticationSuccess();
                }
                else
                {
                    AuthenticationStatus status = result.ResponseCode switch
                    {
                        401 or 403 => AuthenticationStatus.InvalidCredentials,
                        404 => AuthenticationStatus.ServerError,
                        500 or 502 or 503 => AuthenticationStatus.ServerError,
                        0 => AuthenticationStatus.NetworkError,
                        666 => AuthenticationStatus.AccountDisabled,
                        667 => AuthenticationStatus.UserNotFound,
                        _ => AuthenticationStatus.Undefined
                    };
                    OnAuthenticationFailure(status);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthenticatingState] Login Exception: {e.Message}");
                OnAuthenticationFailure(AuthenticationStatus.NetworkError);
            }
        }
    }
}
