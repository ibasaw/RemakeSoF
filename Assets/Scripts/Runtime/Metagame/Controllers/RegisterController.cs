using System;
using System.Collections;
using System.Text;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class RegisterController : Controller<MetagameApplication>
    {
        RegisterView View => App.View.RegisterView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        void Awake()
        {
            AddListener<PlayerRegisterEvent>(OnPlayerRegister);
            AddListener<ChangeToRegisterEvent>(OnChangeToRegister);
            AddListener<ChangeToLoginEvent>(OnChangeToLogin);
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        // Called when the user clicks the "Register" button on the registration view to attempt to register
        void OnPlayerRegister(PlayerRegisterEvent evt)
        {
            Debug.Log($"Attempting to register user: {evt.username} with email: {evt.email} and password: {evt.password}");
            
            // Validate all registration data
            if (!View.ValidateRegistrationData())
            {
                return; // Validation failed, error message already shown
            }
            
            // Clear any previous status message
            View.ClearStatusMessage();
            
            // Set UI to loading state
            View.SetRegisterInProgress(true);
            
            // Start coroutine to handle HTTP request
            StartCoroutine(RegisterToServer(evt.username, evt.email, evt.password));
        }

        IEnumerator RegisterToServer(string username, string email, string password)
        {
            // Create JSON payload
            var registerData = new RegisterRequest
            {
                username = username,
                email = email,
                password = password
            };
            
            string jsonData = JsonUtility.ToJson(registerData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            // Create UnityWebRequest
            using (UnityWebRequest request = new UnityWebRequest("http://localhost:8080/api/registerUser", "POST"))
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
                        var response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);
                        Debug.Log($"Server response: {response.message}");
                        
                        // Show success message and keep UI disabled (user should proceed to login)
                        View.SetStatusMessage("Registration successful! Please login.", false);
                        View.SetRegisterInProgress(false, false); // false = don't clear status message
                        
                        // Optionally switch to login view after successful registration
                        // Broadcast(new ChangeToLoginEvent());
                    }
                    catch (Exception e)
                    {
                        // Show success message and keep UI disabled
                        View.SetStatusMessage($"Registration failed: {e.Message}", true);
                        View.SetRegisterInProgress(false, false); // false = don't clear status message
                    }
                }
                else
                {
                    View.SetStatusMessage($"Registration failed: {request.error}", true);
                    // Re-enable UI for retry
                    View.SetRegisterInProgress(false, false); // false = don't clear status message
                }
            }
        }

        [System.Serializable]
        public class RegisterRequest
        {
            public string username;
            public string email;
            public string password;
        }

        [System.Serializable]
        public class RegisterResponse
        {
            public bool success;
            public string message;
            public string userId;
        }

        // Called when the user clicks the "Back" button on the registration view to switch back to the login view
        void OnChangeToLogin(ChangeToLoginEvent evt)
        {
            View.Hide();
        }

        // Called when the user clicks the "Register" button on the login view to switch to the registration view
        void OnChangeToRegister(ChangeToRegisterEvent evt)
        {
            View.Show();
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<PlayerRegisterEvent>(OnPlayerRegister);
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
