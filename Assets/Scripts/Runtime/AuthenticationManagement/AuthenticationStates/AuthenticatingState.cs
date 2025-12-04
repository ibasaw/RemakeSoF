using System;
using System.Collections;
using System.Text;
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

        public override void Enter()
        {
            var authEvent = new AuthenticationEvent { status = AuthenticationStatus.Authenticating };
            Manager.EventManager.Broadcast(authEvent);

            // Start the login coroutine
            Manager.StartCoroutine(LoginToServer(m_Username, m_Password));
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

        private IEnumerator LoginToServer(string username, string password)
        {
            // Create JSON payload
            var loginData = new AuthenticationPayload
            {
                username = username,
                password = password
            };

            string jsonData = JsonUtility.ToJson(loginData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // Create UnityWebRequest
            using UnityWebRequest request = new("http://localhost:8080/api/loginUser", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send request
            yield return request.SendWebRequest();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse response if needed
                try
                {
                    var response = JsonUtility.FromJson<AuthenticationResponse>(request.downloadHandler.text);
                    Debug.Log($"Server response: {response.message}");

                    // Successful authentication
                    Manager.OnAuthenticationSuccess();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse authentication response: {e.Message}");
                    // Authentication failed - invalid response format
                    OnAuthenticationFailure(AuthenticationStatus.InvalidCredentials);
                }
            }
            else
            {
                // TODO improve error handling based on server response and handle states from AuthenticationStatus enum
                Debug.LogError($"Authentication request failed: {request.error}");
                // Determine failure reason
                AuthenticationStatus status = request.responseCode == 401
                    ? AuthenticationStatus.InvalidCredentials
                    : AuthenticationStatus.NetworkError;

                OnAuthenticationFailure(status);
            }
        }
    }
}
