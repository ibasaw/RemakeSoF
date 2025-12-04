using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    /// <summary>
    /// Stellt die Login-Benutzeroberfläche bereit und verwaltet Benutzerinteraktionen
    /// mit der Login-Ansicht. Interagiert mit dem LoginController, um
    /// Login-Ereignisse zu verarbeiten.
    /// </summary>
    internal class LoginView : View<MetagameApplication>
    {
        Button m_LoginButton;
        Button m_ChangeToRegisterButton;
        Button m_QuitButton;
        TextField m_UsernameTextField;
        TextField m_PasswordTextField;
        Label m_StatusLabel;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_LoginButton = root.Q<Button>("loginButton");
            m_ChangeToRegisterButton = root.Q<Button>("registerButton");
            m_QuitButton = root.Q<Button>("quitButton");

            m_UsernameTextField = root.Q<TextField>("usernameTextField");
            m_PasswordTextField = root.Q<TextField>("passwordTextField");
            m_StatusLabel = root.Q<Label>("statusLabel");

            m_LoginButton.RegisterCallback<ClickEvent>(OnClickLogin);
            m_ChangeToRegisterButton.RegisterCallback<ClickEvent>(OnClickChangeToRegister);
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);

            m_UsernameTextField.RegisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.RegisterValueChangedCallback(OnPasswordChanged);
        }

        void OnDisable()
        {
            m_LoginButton.UnregisterCallback<ClickEvent>(OnClickLogin);
            m_ChangeToRegisterButton.UnregisterCallback<ClickEvent>(OnClickChangeToRegister);
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
            
            m_UsernameTextField.UnregisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.UnregisterValueChangedCallback(OnPasswordChanged);
        }

        void OnUsernameChanged(ChangeEvent<string> username)
        {
           m_UsernameTextField.value = username.newValue;
        }
        void OnPasswordChanged(ChangeEvent<string> password)
        {
           m_PasswordTextField.value = password.newValue;
        }

        void OnClickQuit(ClickEvent evt)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        void OnClickLogin(ClickEvent evt)
        {
            Broadcast(new PlayerLoginEvent
            {
                username = m_UsernameTextField.text,
                password = m_PasswordTextField.text
            });
        }
        void OnClickChangeToRegister(ClickEvent evt)
        {
            Broadcast(new ChangeToRegisterEvent());
        }

        public void SetLoginInProgress(bool inProgress, bool clearStatusMessage = true)
        {
            if (inProgress)
            {
                m_LoginButton.SetEnabled(false);
                m_ChangeToRegisterButton.SetEnabled(false);
                m_QuitButton.SetEnabled(false);
                m_UsernameTextField.SetEnabled(false);
                m_PasswordTextField.SetEnabled(false);
                m_StatusLabel.text = "Logging in...";
                m_StatusLabel.style.color = new StyleColor(new UnityEngine.Color(0f, 1f, 0f, 1f)); // Green
            }
            else
            {
                m_LoginButton.SetEnabled(true);
                m_ChangeToRegisterButton.SetEnabled(true);
                m_QuitButton.SetEnabled(true);
                m_UsernameTextField.SetEnabled(true);
                m_PasswordTextField.SetEnabled(true);
                
                // Only clear status message if explicitly requested
                if (clearStatusMessage)
                {
                    m_StatusLabel.text = "";
                }
            }
        }

        public void SetStatusMessage(string message, bool isError = false)
        {
            m_StatusLabel.text = message;
            if (isError)
            {
                m_StatusLabel.style.color = new StyleColor(new UnityEngine.Color(1f, 0f, 0f, 1f)); // Red
            }
            else
            {
                m_StatusLabel.style.color = new StyleColor(new UnityEngine.Color(0f, 1f, 0f, 1f)); // Green
            }
        }

        public void ClearStatusMessage()
        {
            m_StatusLabel.text = "";
        }

        public bool ValidateLoginData()
        {
            string email = m_UsernameTextField.text.Trim();
            string password = m_PasswordTextField.text;

            // Validate email
            if (string.IsNullOrEmpty(email))
            {
                SetStatusMessage("Email cannot be empty!", true);
                return false;
            }

            // Validate password
            if (string.IsNullOrEmpty(password))
            {
                SetStatusMessage("Password cannot be empty!", true);
                return false;
            }

            return true;
        }
    }
}
