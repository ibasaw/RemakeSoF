using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Tolik.RemakeSoF.Runtime.ChatManagement
{
    /// <summary>
    /// View for the chat system. Manages both the full chat panel (input + history)
    /// and the HUD overlay that shows recent messages with fade-out (SoF2-style, 6s duration).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChatView : View<ChatManager>
    {
        UIDocument m_UIDocument;

        VisualElement m_ChatRoot;
        ScrollView m_ChatOutput;
        TextField m_ChatInput;
        VisualElement m_HudOverlay;

        /// <summary>
        /// QuakeColorLabel overlay rendered on top of the hidden TextField text.
        /// Shows typed text with Quake color codes and a blinking cursor.
        /// </summary>
        QuakeColorLabel m_InputOverlay;

        /// <summary>
        /// Whether the full chat panel (with input field) is currently open.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Tracks HUD labels and their creation time for fade-out.
        /// </summary>
        readonly List<(VisualElement label, float time)> m_HudLabels = new();

        bool m_Initialized;

        /// <summary>
        /// Bigchars atlas texture for QuakeColorLabel rendering.
        /// </summary>
        Texture2D m_BigcharsAtlas;

        /// <summary>
        /// Frame counter for deferred focus. We wait 2 frames after opening the chat
        /// so the 'T' keystroke from the InputAction is fully consumed by UIToolkit
        /// before the TextField receives focus.
        /// </summary>
        int m_FocusDelayFrames;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            EnsureInitialized();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
            ApplySceneVisibility(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            if (m_ChatInput != null)
            {
                m_ChatInput.UnregisterCallback<KeyUpEvent>(OnInputSubmit);
                m_ChatInput.UnregisterCallback<KeyDownEvent>(OnInputKeyDown);
                m_ChatInput.UnregisterValueChangedCallback(OnInputValueChanged);
            }
        }

        /// <summary>
        /// Queries all UI elements from the UIDocument. Safe to call multiple times.
        /// Must be called before any UI access because the ChatView GameObject must stay active
        /// (HUD overlay needs to render even when the chat panel is closed).
        /// </summary>
        void EnsureInitialized()
        {
            if (m_Initialized)
            {
                return;
            }

            if (m_UIDocument == null)
            {
                m_UIDocument = GetComponent<UIDocument>();
            }

            VisualElement root = m_UIDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            m_ChatRoot = root.Q<VisualElement>("chat-root");
            m_ChatOutput = root.Q<ScrollView>("chat-output");
            m_ChatInput = root.Q<TextField>("chat-input");
            m_HudOverlay = root.Q<VisualElement>("chat-hud-overlay");

            m_ChatInput.RegisterCallback<KeyUpEvent>(OnInputSubmit);
            m_ChatInput.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
            m_ChatInput.RegisterValueChangedCallback(OnInputValueChanged);

            SetupInputOverlay();

            // Start with panel hidden, HUD overlay visible
            m_ChatRoot.style.display = DisplayStyle.None;
            m_HudOverlay.style.display = DisplayStyle.Flex;

            LoadBigcharsAtlas();

            m_Initialized = true;
        }

        void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene next)
        {
            ApplySceneVisibility(next.name);
        }

        /// <summary>
        /// Chat is a gameplay-only HUD. Hide the entire root in non-game scenes so it does not
        /// bleed onto the Metagame screens (login/register/loadout/main menu).
        /// </summary>
        void ApplySceneVisibility(string sceneName)
        {
            if (m_UIDocument == null)
            {
                return;
            }

            VisualElement root = m_UIDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            bool inGame = ChatController.IsGameplayScene(sceneName);
            root.style.display = inGame ? DisplayStyle.Flex : DisplayStyle.None;

            // Close the chat panel if we leave gameplay while it was open.
            if (!inGame && IsOpen)
            {
                IsOpen = false;
                if (m_ChatRoot != null)
                {
                    m_ChatRoot.style.display = DisplayStyle.None;
                }
            }
        }

        void Update()
        {
            if (m_FocusDelayFrames > 0)
            {
                m_FocusDelayFrames--;
                if (m_FocusDelayFrames == 0)
                {
                    m_ChatInput.SetValueWithoutNotify("");

                    if (m_InputOverlay != null)
                    {
                        m_InputOverlay.Text = "";
                    }

                    m_ChatInput.Focus();
                }
            }

            // Retry atlas loading if it wasn't available during init
            if (m_BigcharsAtlas == null)
            {
                LoadBigcharsAtlas();
            }

            FadeHudMessages();
        }

        /// <summary>
        /// Toggles the full chat panel (history + input field).
        /// </summary>
        public void Toggle()
        {
            if (!ChatController.IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                return;
            }
            EnsureInitialized();
            IsOpen = !IsOpen;

            if (IsOpen)
            {
                m_ChatRoot.style.display = DisplayStyle.Flex;

                // Unlock cursor so the player can interact with the text field
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Defer focus by 2 frames so the 'T' keystroke from the InputAction
                // is fully consumed by UIToolkit before the TextField receives focus.
                m_FocusDelayFrames = 2;
            }
            else
            {
                m_ChatRoot.style.display = DisplayStyle.None;

                // Re-lock cursor for gameplay
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// Closes the chat panel without toggling.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureInitialized();
            IsOpen = false;
            m_ChatRoot.style.display = DisplayStyle.None;

            // Re-lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Handles Escape key to close chat without sending.
        /// </summary>
        void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopPropagation();
                Close();
            }
        }

        /// <summary>
        /// Handles Enter key submission from the chat input field.
        /// </summary>
        void OnInputSubmit(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return)
            {
                return;
            }

            string message = m_ChatInput.value.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                m_ChatInput.SetValueWithoutNotify("");

                if (m_InputOverlay != null)
                {
                    m_InputOverlay.Text = "";
                }

                Broadcast(new SubmitChatMessageEvent { message = message });
            }

            evt.StopPropagation();

            // Close chat after sending (SoF2 behavior)
            Close();
        }

        /// <summary>
        /// Adds a chat line to both the full chat history and the HUD overlay.
        /// </summary>
        internal void AddChatLine(string senderName, string message)
        {
            EnsureInitialized();
            string formatted = $"{senderName}: ^7{message}";

            // Add to full chat history
            QuakeColorLabel historyLabel = CreateQuakeLabel(formatted, 12f, 18f, 630f);
            m_ChatOutput.contentContainer.Add(historyLabel);
            ScrollToBottom();

            // Add to HUD overlay with fade
            QuakeColorLabel hudLabel = CreateQuakeLabel(formatted, 12f, 18f, 610f);
            hudLabel.AddToClassList("chat-hud-message");
            m_HudOverlay.Add(hudLabel);
            m_HudLabels.Add((hudLabel, Time.time));
        }

        /// <summary>
        /// Adds a server/system message (no sender name).
        /// </summary>
        internal void AddSystemMessage(string message)
        {
            EnsureInitialized();

            // Full chat history
            QuakeColorLabel historyLabel = CreateQuakeLabel(message, 12f, 18f, 630f);
            m_ChatOutput.contentContainer.Add(historyLabel);
            ScrollToBottom();

            // HUD overlay
            QuakeColorLabel hudLabel = CreateQuakeLabel(message, 12f, 18f, 610f);
            hudLabel.AddToClassList("chat-hud-message");
            m_HudOverlay.Add(hudLabel);
            m_HudLabels.Add((hudLabel, Time.time));
        }

        /// <summary>
        /// Creates a QuakeColorLabel with the bigchars atlas and specified dimensions.
        /// </summary>
        QuakeColorLabel CreateQuakeLabel(string text, float charWidth, float charHeight, float maxWidth)
        {
            QuakeColorLabel label = new QuakeColorLabel();
            label.CharWidth = charWidth;
            label.CharHeight = charHeight;
            label.MaxWidth = maxWidth;
            label.Text = text;

            if (m_BigcharsAtlas != null)
            {
                label.Atlas = m_BigcharsAtlas;
            }

            return label;
        }

        /// <summary>
        /// Loads the bigchars atlas texture from the TextureManager via ServiceLocator.
        /// </summary>
        void LoadBigcharsAtlas()
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                return;
            }

            TextureConfiguration.MetagameConfiguration.LoadoutTextures config =
                textureManager.Configuration?.metagame?.loadout;
            if (config == null || string.IsNullOrEmpty(config.bigcharsAtlas))
            {
                return;
            }

            TextureData atlasData = textureManager.GetTextureData(config.bigcharsAtlas);
            if (atlasData?.Texture != null)
            {
                m_BigcharsAtlas = atlasData.Texture;

                if (m_InputOverlay != null)
                {
                    m_InputOverlay.Atlas = m_BigcharsAtlas;
                }
            }
        }

        /// <summary>
        /// Creates the QuakeColorLabel overlay on top of the TextField inner input element.
        /// The TextField text is made transparent; the overlay renders Quake-colored text
        /// with a blinking cursor (same pattern as LoadoutView).
        /// </summary>
        void SetupInputOverlay()
        {
            VisualElement inputContainer = m_ChatInput.Q(className: "unity-text-field__input");
            if (inputContainer == null)
            {
                return;
            }

            inputContainer.style.position = Position.Relative;

            m_InputOverlay = new QuakeColorLabel();
            m_InputOverlay.style.position = Position.Absolute;
            m_InputOverlay.style.left = 8;
            m_InputOverlay.style.right = 8;
            m_InputOverlay.style.top = 0;
            m_InputOverlay.style.bottom = 0;
            m_InputOverlay.style.alignItems = Align.Center;
            m_InputOverlay.style.justifyContent = Justify.Center;
            m_InputOverlay.pickingMode = PickingMode.Ignore;
            m_InputOverlay.ShowCursor = true;
            m_InputOverlay.MaxWidth = 610f;
            inputContainer.Add(m_InputOverlay);

            // Hide native TextField text so only the QuakeColorLabel overlay is visible
            VisualElement textElement = inputContainer.Q(className: "unity-text-element");
            if (textElement != null)
            {
                textElement.style.color = new StyleColor(new Color(0, 0, 0, 0));
                textElement.style.unityBackgroundImageTintColor = new StyleColor(new Color(0, 0, 0, 0));
            }

            inputContainer.style.color = new StyleColor(new Color(0, 0, 0, 0));
        }

        /// <summary>
        /// Syncs the QuakeColorLabel input overlay with the current TextField value.
        /// </summary>
        void OnInputValueChanged(ChangeEvent<string> evt)
        {
            if (m_InputOverlay != null)
            {
                m_InputOverlay.Text = evt.newValue ?? "";
            }
        }

        void FadeHudMessages()
        {
            for (int i = m_HudLabels.Count - 1; i >= 0; i--)
            {
                (VisualElement label, float time) = m_HudLabels[i];
                float elapsed = Time.time - time;

                if (elapsed >= ChatModel.MessageDisplayDuration)
                {
                    // Remove expired message
                    m_HudOverlay.Remove(label);
                    m_HudLabels.RemoveAt(i);
                }
                else if (elapsed >= ChatModel.MessageDisplayDuration - 1f)
                {
                    // Fade out during last second
                    float alpha = (ChatModel.MessageDisplayDuration - elapsed) / 1f;
                    label.style.opacity = alpha;
                }
            }
        }

        /// <summary>
        /// Scrolls the chat output to the bottom after a new message is added.
        /// </summary>
        void ScrollToBottom()
        {
            void ScrollCallback(GeometryChangedEvent evt)
            {
                VisualElement container = evt.target as VisualElement;
                if (container == null || container.childCount == 0)
                {
                    return;
                }

                VisualElement last = container.ElementAt(container.childCount - 1);
                m_ChatOutput.ScrollTo(last);
                container.UnregisterCallback<GeometryChangedEvent>(ScrollCallback);
            }

            m_ChatOutput.contentContainer.RegisterCallback<GeometryChangedEvent>(ScrollCallback);
        }
    }
}
