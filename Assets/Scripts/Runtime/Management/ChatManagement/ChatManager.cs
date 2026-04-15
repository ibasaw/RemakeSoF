using Tolik.RemakeSoF.Runtime.Core;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ChatManagement
{
    /// <summary>
    /// Root application for the chat system. Manages the MVC lifecycle
    /// and provides a singleton instance for network bridge access.
    /// </summary>
    public class ChatManager : BaseApplication<ChatModel, ChatView, ChatController>
    {
        internal new static ChatManager Instance { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
}
