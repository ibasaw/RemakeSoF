using UnityEngine;
using UnityEngine.InputSystem;

namespace Tolik.RemakeSoF.Runtime.ChatManagement
{
    /// <summary>
    /// Controller for the chat system. Handles the ToggleChat input action
    /// and routes events between view and network bridge.
    /// </summary>
    public class ChatController : Controller<ChatManager>
    {
        ChatView View => App.View;

        /// <summary>
        /// Reference to the ToggleChat input action from the AvatarActions asset.
        /// </summary>
        public InputActionReference toggleChatAction;

        void Awake()
        {
            AddListener<ToggleChatEvent>(OnToggleChat);
            AddListener<SubmitChatMessageEvent>(OnSubmitChatMessage);
            AddListener<ChatMessageReceivedEvent>(OnChatMessageReceived);
            AddListener<KillFeedReceivedEvent>(OnKillFeedReceived);
            AddListener<MotdReceivedEvent>(OnMotdReceived);
        }

        void OnEnable()
        {
            toggleChatAction.action.performed += OnToggleChatAction;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
            ApplySceneGate(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void OnDisable()
        {
            toggleChatAction.action.performed -= OnToggleChatAction;
            toggleChatAction.action.Disable();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene next)
        {
            ApplySceneGate(next.name);
        }

        /// <summary>
        /// Chat input only listens during gameplay. Without this gate the ToggleChat action
        /// stays enabled after leaving a gameplay scene — pressing 'T' in any Metagame TextField
        /// would open the chat panel, steal UIToolkit focus and lock the cursor.
        /// </summary>
        void ApplySceneGate(string sceneName)
        {
            if (IsGameplayScene(sceneName))
            {
                toggleChatAction.action.Enable();
            }
            else
            {
                toggleChatAction.action.Disable();
                // Restore cursor in case we left gameplay while chat was open / cursor locked.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// A scene counts as gameplay unless it is one of the explicit menu/bootstrap scenes.
        /// Opt-out by name keeps new gameplay scenes (GameScene02, GameScene03, …) working
        /// without further code changes.
        /// </summary>
        internal static bool IsGameplayScene(string sceneName)
        {
            return sceneName != "StartupScene" && sceneName != "MetagameScene";
        }

        /// <summary>
        /// Input System callback for the ToggleChat action.
        /// </summary>
        void OnToggleChatAction(InputAction.CallbackContext ctx)
        {
            if (!IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                return;
            }
            Broadcast(new ToggleChatEvent());
        }

        internal override void RemoveListeners()
        {
            RemoveListener<ToggleChatEvent>(OnToggleChat);
            RemoveListener<SubmitChatMessageEvent>(OnSubmitChatMessage);
            RemoveListener<ChatMessageReceivedEvent>(OnChatMessageReceived);
            RemoveListener<KillFeedReceivedEvent>(OnKillFeedReceived);
            RemoveListener<MotdReceivedEvent>(OnMotdReceived);
        }

        /// <summary>
        /// Toggles the full chat panel visibility.
        /// </summary>
        void OnToggleChat(ToggleChatEvent evt)
        {
            View.Toggle();
        }

        /// <summary>
        /// Handles a submitted chat message from the view. Forwards to the network bridge.
        /// </summary>
        void OnSubmitChatMessage(SubmitChatMessageEvent evt)
        {
            if (NetworkedChatBridge.Instance != null)
            {
                NetworkedChatBridge.Instance.SendChatMessage(evt.message);
            }
            else
            {
                Debug.LogWarning("[ChatController] No NetworkedChatBridge instance found. Cannot send message.");
                View.AddSystemMessage("Not connected to a server. Message not sent.");
            }
        }

        /// <summary>
        /// Handles an incoming chat message from the server. Adds it to the model and view.
        /// </summary>
        void OnChatMessageReceived(ChatMessageReceivedEvent evt)
        {
            App.Model.AddMessage(evt.senderName, evt.message, Time.time);
            View.AddChatLine(evt.senderName, evt.message);
        }

        /// <summary>
        /// Handles a kill feed event from the server. Formats and displays as a system message.
        /// Format: "victimName ^7killed by killerName ^7with weaponName (REGION)"
        /// </summary>
        void OnKillFeedReceived(KillFeedReceivedEvent evt)
        {
            string killMessage = $"{evt.victimName} ^7killed by {evt.killerName} ^7with ^3{evt.weaponName}";
            if (!string.IsNullOrEmpty(evt.hitRegion))
            {
                killMessage += $" ^7({evt.hitRegion})";
            }

            View.AddSystemMessage(killMessage);
        }

        /// <summary>
        /// Handles the MOTD from the server. Displayed as a system message with full color code support.
        /// </summary>
        void OnMotdReceived(MotdReceivedEvent evt)
        {
            View.AddSystemMessage(evt.message);
        }
    }
}
