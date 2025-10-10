using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class LoginView : View<MetagameApplication>
    {
        Button m_LoginButton;
        Button m_ChangeToRegisterButton;
        Button m_QuitButton;
        TextField m_UsernameTextField;
        TextField m_PasswordTextField;
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

            m_LoginButton.RegisterCallback<ClickEvent>(OnClickLogin);
            m_ChangeToRegisterButton.RegisterCallback<ClickEvent>(OnClickChangeToRegister);
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);

            m_UsernameTextField.RegisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.RegisterValueChangedCallback(OnPasswordChanged);
        }

        void OnDisable()
        {
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
            m_UsernameTextField.UnregisterValueChangedCallback(OnUsernameChanged);
            m_PasswordTextField.UnregisterValueChangedCallback(OnPasswordChanged);
        }

        void OnUsernameChanged(ChangeEvent<string> username)
        {
            ValidateAndSetUsername(username.newValue);
        }
        void OnPasswordChanged(ChangeEvent<string> password)
        {
            ValidateAndSetPassword(password.newValue);
        }

        void ValidateAndSetUsername(string usernameToValidate)
        {
            var username = Sanitize(usernameToValidate);
            m_UsernameTextField.value = username;
        }
        void ValidateAndSetPassword(string passwordToValidate)
        {
            //var password = Sanitize(passwordToValidate);
            m_PasswordTextField.value = passwordToValidate;// password;
        }

        string Sanitize(string input)
        {
            // Example sanitization: remove any non-alphanumeric characters except underscores
            return Regex.Replace(input, @"[^a-zA-Z0-9_]", "");
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
    }
}
