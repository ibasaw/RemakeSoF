using UnityEngine.UIElements;
using System.Text.RegularExpressions;

namespace Tolik.RemakeSoF.Runtime
{
    internal class RegisterView : View<MetagameApplication>
    {
        Button m_BackToLoginButton;
        Button m_RegisterButton;
        TextField m_UsernameTextField;
        TextField m_EmailTextField;
        TextField m_PasswordTextField;
        TextField m_ConfirmPasswordTextField;
        Label m_StatusLabel;
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_BackToLoginButton = root.Q<Button>("backButton");
            m_RegisterButton = root.Q<Button>("registerButton");

            m_UsernameTextField = root.Q<TextField>("usernameTextField");
            m_PasswordTextField = root.Q<TextField>("passwordTextField");
            m_ConfirmPasswordTextField = root.Q<TextField>("repeatPasswordTextField");
            m_EmailTextField = root.Q<TextField>("emailTextField");
            m_StatusLabel = root.Q<Label>("statusLabel");

            m_BackToLoginButton.RegisterCallback<ClickEvent>(OnClickBackToLogin);
            m_RegisterButton.RegisterCallback<ClickEvent>(OnClickRegister);

            m_BackToLoginButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
            m_RegisterButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));

            m_UsernameTextField.RegisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.RegisterValueChangedCallback(OnPasswordChanged);
            m_ConfirmPasswordTextField.RegisterValueChangedCallback(OnConfirmPasswordChanged);
            m_EmailTextField.RegisterValueChangedCallback(OnEmailChanged);
        }

        void OnDisable()
        {
            m_BackToLoginButton.UnregisterCallback<ClickEvent>(OnClickBackToLogin);
            m_RegisterButton.UnregisterCallback<ClickEvent>(OnClickRegister);
            
            m_UsernameTextField.UnregisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.UnregisterValueChangedCallback(OnPasswordChanged);
            m_ConfirmPasswordTextField.UnregisterValueChangedCallback(OnConfirmPasswordChanged);
            m_EmailTextField.UnregisterValueChangedCallback(OnEmailChanged);
        }

        void OnUsernameChanged(ChangeEvent<string> username)
        {
            m_UsernameTextField.value = username.newValue;
        }
        void OnPasswordChanged(ChangeEvent<string> password)
        {
            m_PasswordTextField.value = password.newValue;
        }
        void OnConfirmPasswordChanged(ChangeEvent<string> password)
        {
            m_ConfirmPasswordTextField.value = password.newValue;
        }
        void OnEmailChanged(ChangeEvent<string> email)
        {
            m_EmailTextField.value = email.newValue;
        }

        void OnClickBackToLogin(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Click);
            Broadcast(new ChangeToLoginEvent());
        }

        void OnClickRegister(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.ApplyChanges);
            Broadcast(new PlayerRegisterEvent
            {
                username = m_UsernameTextField.text,
                email = m_EmailTextField.text,
                password = m_PasswordTextField.text,
                confirmPassword = m_ConfirmPasswordTextField.text
            });
        }

        public void SetRegisterInProgress(bool inProgress, bool clearStatusMessage = true)
        {
            if (inProgress)
            {
                m_RegisterButton.SetEnabled(false);
                m_BackToLoginButton.SetEnabled(false);
                m_UsernameTextField.SetEnabled(false);
                m_EmailTextField.SetEnabled(false);
                m_PasswordTextField.SetEnabled(false);
                m_ConfirmPasswordTextField.SetEnabled(false);
                m_StatusLabel.text = "Registering...";
                m_StatusLabel.style.color = new StyleColor(new UnityEngine.Color(0f, 1f, 0f, 1f)); // Green
            }
            else
            {
                m_RegisterButton.SetEnabled(true);
                m_BackToLoginButton.SetEnabled(true);
                m_UsernameTextField.SetEnabled(true);
                m_EmailTextField.SetEnabled(true);
                m_PasswordTextField.SetEnabled(true);
                m_ConfirmPasswordTextField.SetEnabled(true);
                
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

        public bool ValidateRegistrationData()
        {
            string username = m_UsernameTextField.text.Trim();
            string email = m_EmailTextField.text.Trim();
            string password = m_PasswordTextField.text;
            string confirmPassword = m_ConfirmPasswordTextField.text;

            // Validate username
            if (string.IsNullOrEmpty(username))
            {
                SetStatusMessage("Username cannot be empty!", true);
                return false;
            }

            if (username.Length < 3)
            {
                SetStatusMessage("Username must be at least 3 characters long!", true);
                return false;
            }

            // Validate email
            if (string.IsNullOrEmpty(email))
            {
                SetStatusMessage("Email cannot be empty!", true);
                return false;
            }

            if (!IsValidEmail(email))
            {
                SetStatusMessage("Please enter a valid email address!", true);
                return false;
            }

            // Validate password
            if (string.IsNullOrEmpty(password))
            {
                SetStatusMessage("Password cannot be empty!", true);
                return false;
            }

            // Validate password confirmation
            if (password != confirmPassword)
            {
                SetStatusMessage("Passwords do not match!", true);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                // Simple email validation regex
                var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
