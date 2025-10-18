using System;
using System.Collections;
using System.Text;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class LoginController : Controller<MetagameApplication>
    {
        LoginView View => App.View.LoginView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        void Awake()
        {
            AddListener<PlayerLoginEvent>(OnPlayerLogin);
            AddListener<ChangeToRegisterEvent>(OnChangeToRegister);
            AddListener<ChangeToLoginEvent>(OnChangeToLogin);
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        // Called when the user clicks the "Login" button on the login view to attempt to log in
        void OnPlayerLogin(PlayerLoginEvent evt)
        {
            Debug.Log($"Attempting to log in user: {evt.username} with password: {evt.password}");
            
            // Validate all login data
            if (!View.ValidateLoginData())
            {
                return; // Validation failed, error message already shown
            }
            
            // Clear any previous status message
            View.ClearStatusMessage();
            
            // Set UI to loading state
            View.SetLoginInProgress(true);
            
            // Start coroutine to handle HTTP request
            StartCoroutine(LoginToServer(evt.username, evt.password));
        }

        IEnumerator LoginToServer(string username, string password)
        {
            // Create JSON payload
            var loginData = new LoginRequest
            {
                username = username,
                email = null,
                password = password
            };
            
            string jsonData = JsonUtility.ToJson(loginData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            // Create UnityWebRequest
            using (UnityWebRequest request = new UnityWebRequest("http://localhost:8080/api/loginUser", "POST"))
            {
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
                        var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                        Debug.Log($"Server response: {response.message}");
                        
                        // Broadcast successful login event
                        //Broadcast(new PlayerSignedIn(true, response.playerId ?? username));
                        
                        // Show success message and keep UI disabled (user should proceed to next screen)
                        View.SetStatusMessage("Login successful!", false);
                        View.SetLoginInProgress(false, false); // false = don't clear status message
                    }
                    catch (Exception e)
                    {
                        //Broadcast(new PlayerSignedIn(true, username));
                        // Show success message and keep UI disabled
                        View.SetStatusMessage($"Login failed: {e.Message}", true);
                        View.SetLoginInProgress(false, false); // false = don't clear status message
                    }
                }
                else
                {
                    View.SetStatusMessage($"Login failed: {request.error}", true);
                    //Broadcast(new PlayerSignedIn(false, ""));
                    // Re-enable UI for retry
                    View.SetLoginInProgress(false, false); // true = clear status message
                }
            }
        }

        [System.Serializable]
        public class LoginRequest
        {
            public string username;
            public string email;
            public string password;
        }

        [System.Serializable]
        public class LoginResponse
        {
            public bool success;
            public string message;
            public string playerId;
        }
        
        // Called when the user clicks the "Login" button on the registration view to switch back to the login view
        void OnChangeToLogin(ChangeToLoginEvent evt)
        {
            View.Show();
        }

        // Called when the user clicks the "Register" button on the login view to switch to the registration view
        void OnChangeToRegister(ChangeToRegisterEvent evt)
        {
            View.Hide();
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<PlayerLoginEvent>(OnPlayerLogin);
            RemoveListener<ChangeToRegisterEvent>(OnChangeToRegister);
            RemoveListener<ChangeToLoginEvent>(OnChangeToLogin);
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            if (evt.status == ConnectStatus.Connecting)
            {
                View.Hide();
            }
        }
    }
}
