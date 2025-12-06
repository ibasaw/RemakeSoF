using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement
{
    /// <summary>
    /// Authentication state corresponding to when the user is attempting to authenticate.
    /// From this state we can transition to AuthenticatedState on success or back to
    /// UnauthenticatedState on failure.
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

        public override async void Enter()
        {
            var authEvent = new AuthenticationEvent { status = AuthenticationStatus.Authenticating };
            Manager.EventManager.Broadcast(authEvent);

            await LoginToServer(m_Username, m_Password);
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

        private async Task LoginToServer(string username, string password)
        {
            var loginData = new AuthenticationPayload
            {
                username = username,
                password = password
            };

            string jsonData = JsonUtility.ToJson(loginData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using var request = new UnityWebRequest("http://localhost:8000/api/loginUser", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<AuthenticationResponse>(request.downloadHandler.text);
                    Debug.Log($"Server response: {response.message}");
                    Manager.m_Authenticated.Configure(response);
                    Manager.OnAuthenticationSuccess();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse response: {e.Message}");
                    OnAuthenticationFailure(AuthenticationStatus.Undefined);
                }
            }
            else
            {
                AuthenticationStatus status = request.responseCode switch
                {
                    401 or 403 => AuthenticationStatus.InvalidCredentials,
                    404 or 404 => AuthenticationStatus.ServerError,
                    500 or 502 or 503 => AuthenticationStatus.ServerError,
                    0 => AuthenticationStatus.NetworkError,
                    666 => AuthenticationStatus.AccountDisabled,   // Custom
                    667 => AuthenticationStatus.UserNotFound,   // Custom
                    _ => AuthenticationStatus.Undefined
                };
                OnAuthenticationFailure(status);
            }
        }
    }
}
