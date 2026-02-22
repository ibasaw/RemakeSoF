using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Linq;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;

namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    [RequireComponent(typeof(UIDocument))]
    public class ConsoleView : View<ConsoleManager>
    {
        UIDocument m_UIDocument;
        
        VisualElement m_ConsoleRoot;
        VisualElement m_Glowline;
        VisualElement m_GlowlineWide;

        ScrollView m_Output;
        TextField m_Input;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;

            m_ConsoleRoot = root.Q<VisualElement>("console-root");
            m_Glowline = root.Q<VisualElement>("glowline");
            m_GlowlineWide = root.Q<VisualElement>("glowline-wide");
            m_Output = root.Q<ScrollView>("console-output");
            m_Input = root.Q<TextField>("console-input");

            Debug.Log("ConsoleView Enabled");

            TextureConfiguration configuration = ServiceLocator.Get<TextureManager>().Configuration;
            m_Glowline.style.backgroundImage = new StyleBackground(ServiceLocator.Get<TextureManager>().GetTextureData(configuration.console.glowline)?.Texture);
            m_GlowlineWide.style.backgroundImage = new StyleBackground(ServiceLocator.Get<TextureManager>().GetTextureData(configuration.console.glowlineWide)?.Texture);
            m_ConsoleRoot.style.backgroundImage = new StyleBackground(ServiceLocator.Get<TextureManager>().GetTextureData(configuration.console.background)?.Texture);

            m_Input.RegisterValueChangedCallback(OnCommandChanged);
            m_Input.RegisterCallback<KeyUpEvent>(OnInputSubmit);
        }

        void OnCommandChanged(ChangeEvent<string> command)
        {
            //m_Input.value = command.newValue;
        }

        void OnDisable()
        {
            m_Input.UnregisterCallback<KeyUpEvent>(OnInputSubmit);
            m_Input.UnregisterValueChangedCallback(OnCommandChanged);
            Debug.Log("ConsoleView Disabled");
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;

            if (IsOpen)
            {
                Show();
                m_Input.Focus();
            }
            else
            {
                Hide();
            }
        }

        private void OnInputSubmit(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return)
                return;

            string cmd = m_Input.value.Trim();
            if (!string.IsNullOrEmpty(cmd))
            {
                Debug.Log($"Command sent: {evt.keyCode} cmd: {cmd}");
                m_Input.value = "";
                Broadcast(new SubmitConsoleCommandEvent { command = cmd });
            }
            // verhindern, dass Enter den Fokus entfernt
            evt.StopPropagation();
            m_Input.Focus();
        }

        public void AddOutput(string msg)
        {
            var label = new Label(msg);
            label.style.color = Color.white; // Standardfarbe
            m_Output.contentContainer.Add(label);
            ScrollToBottom();
            m_Input.Focus();
        }

        public void AddWarningOutput(string msg)
        {
            var label = new Label(msg);
            label.style.color = Color.yellow; // Warnungen gelb
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_Output.contentContainer.Add(label);
            ScrollToBottom();
            m_Input.Focus();
        }

        public void AddErrorOutput(string msg)
        {
            var label = new Label(msg);
            label.style.color = Color.red; // Fehler rot
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_Output.contentContainer.Add(label);
            ScrollToBottom();
            m_Input.Focus();
        }

        public void ClearConsole()
        {
            Debug.Log("ClearConsole size = " + m_Output.contentContainer.childCount);
            m_Output.contentContainer.Clear();
        }

        private void ScrollToBottom()
        {
            void ScrollCallback(GeometryChangedEvent evt)
            {
                // evt.target auf VisualElement casten
                var container = evt.target as VisualElement;
                if (container == null) return;

                if (container.childCount == 0) return;

                // Letztes Kind holen
                var last = container.ElementAt(container.childCount - 1);

                // Scrollen
                m_Output.ScrollTo(last);

                // Callback wieder abmelden
                container.UnregisterCallback<GeometryChangedEvent>(ScrollCallback);
            }

            m_Output.contentContainer.RegisterCallback<GeometryChangedEvent>(ScrollCallback);
        }


    }
}
