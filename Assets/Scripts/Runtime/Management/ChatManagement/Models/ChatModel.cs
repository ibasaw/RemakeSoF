using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.ChatManagement
{
    /// <summary>
    /// Data container for chat message history.
    /// </summary>
    public class ChatMessage
    {
        public string SenderName;
        public string Message;
        public float Timestamp;
    }

    /// <summary>
    /// Model for the <see cref="ChatManager"/>. Holds the chat message history.
    /// </summary>
    public class ChatModel : Model<ChatManager>
    {
        /// <summary>
        /// Maximum number of messages kept in history.
        /// </summary>
        const int k_MaxMessages = 100;

        /// <summary>
        /// Duration in seconds that a chat message stays visible on the HUD overlay.
        /// Matches original SoF2 behavior (6000ms).
        /// </summary>
        internal const float MessageDisplayDuration = 6f;

        readonly List<ChatMessage> m_Messages = new();

        /// <summary>
        /// Read-only access to message history.
        /// </summary>
        internal IReadOnlyList<ChatMessage> Messages => m_Messages;

        /// <summary>
        /// Adds a message and trims history to <see cref="k_MaxMessages"/>.
        /// </summary>
        internal void AddMessage(string senderName, string message, float timestamp)
        {
            m_Messages.Add(new ChatMessage
            {
                SenderName = senderName,
                Message = message,
                Timestamp = timestamp
            });

            while (m_Messages.Count > k_MaxMessages)
            {
                m_Messages.RemoveAt(0);
            }
        }
    }
}
