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
            toggleChatAction.action.Enable();
        }

        void OnDisable()
        {
            toggleChatAction.action.performed -= OnToggleChatAction;
            toggleChatAction.action.Disable();
        }

        /// <summary>
        /// Input System callback for the ToggleChat action.
        /// </summary>
        void OnToggleChatAction(InputAction.CallbackContext ctx)
        {
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
