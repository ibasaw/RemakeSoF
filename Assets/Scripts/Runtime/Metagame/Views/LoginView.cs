using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    /// <summary>
    /// Stellt die Login-Benutzeroberfläche bereit und verwaltet Benutzerinteraktionen
    /// mit der Login-Ansicht. Interagiert mit dem LoginController, um
    /// Login-Ereignisse zu verarbeiten.
    /// </summary>
    internal class LoginView : View<MetagameApplication>
    {
        VisualElement m_MainMenu;
        VisualElement m_LogoImage;
        Button m_LoginButton;
        Button m_ChangeToRegisterButton;
        Button m_QuitButton;
        TextField m_UsernameTextField;
        TextField m_PasswordTextField;
        VisualElement m_loginModal;
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
            m_MainMenu = root.Q<VisualElement>("mainMenu");
            m_LogoImage = root.Q<VisualElement>("logoImage");

            m_UsernameTextField = root.Q<TextField>("usernameTextField");
            m_PasswordTextField = root.Q<TextField>("passwordTextField");
            m_StatusLabel = root.Q<Label>("statusLabel");
            m_loginModal = root.Q<VisualElement>("loginModal");

            m_LoginButton.RegisterCallback<ClickEvent>(OnClickLogin);
            m_ChangeToRegisterButton.RegisterCallback<ClickEvent>(OnClickChangeToRegister);
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);

            m_UsernameTextField.RegisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.RegisterValueChangedCallback(OnPasswordChanged);

            // Focus auf usernameTextField beim Start + Cursor blinkt automatisch
            m_UsernameTextField.Focus();

            // Optional: Select all text für bessere UX
            m_UsernameTextField.SelectAll();
        }
        public void SetBackgroundTexture(Texture2D texture)
        {
            if (texture != null)
            {
                m_MainMenu.style.backgroundImage = new StyleBackground(texture);
            }
        }

        public void SetLogoTexture(Texture2D texture)
        {
            if (texture != null)
            {
                m_LogoImage.style.backgroundImage = new StyleBackground(texture);
            }
        }

        public void SetAllTextFieldInputsBackgroundTexture(Texture2D texture)
        {
            if (texture != null)
            {
                var root = m_UIDocument.rootVisualElement;
                var inputs = root.Query(className: "unity-text-field__input").ToList();

                foreach (var input in inputs)
                {
                    input.style.backgroundImage = new StyleBackground(texture);
                }
            }
        }

        public void SetAllButtonBackgroundTexture(Texture2D texture)
        {
            if (texture != null)
            {
                var root = m_UIDocument.rootVisualElement;
                var buttons = root.Query(className: "button").ToList();

                foreach (var button in buttons)
                {
                    button.style.backgroundImage = new StyleBackground(texture);
                }
            }
        }

        void OnDisable()
        {
            m_LoginButton.UnregisterCallback<ClickEvent>(OnClickLogin);
            m_ChangeToRegisterButton.UnregisterCallback<ClickEvent>(OnClickChangeToRegister);
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);

            m_UsernameTextField.UnregisterValueChangedCallback(OnUsernameChanged);
            m_UsernameTextField.UnregisterValueChangedCallback(OnPasswordChanged);
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
                m_LoginButton.AddToClassList("button-disabled");
                m_ChangeToRegisterButton.AddToClassList("button-disabled");
                m_QuitButton.AddToClassList("button-disabled");
                //m_StatusLabel.text = "Logging in...";
                //m_StatusLabel.style.color = new StyleColor(new UnityEngine.Color(0f, 1f, 0f, 1f)); // Green

                // Show authentication modal
                m_loginModal.style.display = DisplayStyle.Flex;
                var spinner = m_loginModal.Q<VisualElement>(className: "spinner");
                if (spinner != null)
                    spinner.AddToClassList("spinning");
            }
            else
            {
                m_LoginButton.SetEnabled(true);
                m_ChangeToRegisterButton.SetEnabled(true);
                m_QuitButton.SetEnabled(true);
                m_UsernameTextField.SetEnabled(true);
                m_PasswordTextField.SetEnabled(true);
                m_LoginButton.RemoveFromClassList("button-disabled");
                m_ChangeToRegisterButton.RemoveFromClassList("button-disabled");
                m_QuitButton.RemoveFromClassList("button-disabled");

                // Hide authentication modal
                var spinner = m_loginModal.Q<VisualElement>(className: "spinner");
                if (spinner != null)
                    spinner.RemoveFromClassList("spinning");
                m_loginModal.style.display = DisplayStyle.None;

                // Only clear status message if explicitly requested
                if (clearStatusMessage)
                {
                    ClearStatusMessage();
                }
                // Focus auf usernameTextField beim Start + Cursor blinkt automatisch
                m_UsernameTextField.Focus();

                // Optional: Select all text für bessere UX
                m_UsernameTextField.SelectAll();
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
            string username = m_UsernameTextField.text.Trim();
            string password = m_PasswordTextField.text;

            // Validate username
            if (string.IsNullOrEmpty(username))
            {
                SetStatusMessage("Username cannot be empty!", true);
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
