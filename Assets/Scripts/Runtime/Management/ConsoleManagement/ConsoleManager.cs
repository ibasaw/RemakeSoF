using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.Core;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    /// <summary>
    /// This state machine handles the console and its command execution.
    /// It is responsible for managing the console state (active/inactive) and
    /// coordinating command execution.
    /// </summary>
    public class ConsoleManager : StateMachine<ConsoleState, ConsoleManager>
    {
        internal readonly ConsoleInactiveState m_ConsoleInactive = new();
        internal readonly ConsoleActiveState m_ConsoleActive = new();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<ConsoleState> states = new() { m_ConsoleInactive, m_ConsoleActive };
            InitializeStates(states, m_ConsoleInactive);
        }

        /// <summary>
        /// Executes a command in the current console state.
        /// </summary>
        public void ExecuteCommand(string command)
        {
            m_CurrentState.ExecuteCommand(command);
        }

        /// <summary>
        /// Activates the console.
        /// </summary>
        public void Activate()
        {
            m_CurrentState.Activate();
        }

        /// <summary>
        /// Deactivates the console.
        /// </summary>
        public void Deactivate()
        {
            m_CurrentState.Deactivate();
        }

        /// <summary>
        /// Appends text to the console output.
        /// </summary>
        public void AppendOutput(string text)
        {
            m_CurrentState.AppendOutput(text);
        }
    }
}
