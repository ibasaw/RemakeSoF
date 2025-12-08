using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    /// <summary>
    /// Console state when the console is active and processing commands.
    /// In this state, commands are executed and output is displayed.
    /// </summary>
    public class ConsoleActiveState : ConsoleState
    {
        private readonly List<string> m_OutputHistory = new();

        public override void Enter()
        {
            Manager.EventManager.Broadcast(new ConsoleActivatedEvent());
        }

        public override void Exit()
        {
            // Wird aufgerufen beim Wechsel aus diesem State
        }

        public override void Deactivate()
        {
            // Wechsel zu Inactive State
            Manager.ChangeState(Manager.m_ConsoleInactive);
            Manager.EventManager.Broadcast(new ConsoleDeactivatedEvent());
        }

        public override void ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            // Command wird ausgeführt
            ProcessCommand(command);
        }

        public override void AppendOutput(string text)
        {
            // Output wird zur History hinzugefügt
            m_OutputHistory.Add(text);
            Manager.EventManager.Broadcast(new ConsoleOutputEvent { Output = text });
        }

        private void ProcessCommand(string command)
        {
            // Hier kann die Command-Verarbeitung stattfinden
            // Beispiel-Implementierung:
            var parts = command.Split(' ');
            var commandName = parts[0].ToLower();

            switch (commandName)
            {
                case "help":
                    Manager.AppendOutput("Available commands: help, clear, exit");
                    break;
                case "clear":
                    m_OutputHistory.Clear();
                    Manager.AppendOutput("Console cleared.");
                    break;
                case "exit":
                    Deactivate();
                    break;
                default:
                    Manager.AppendOutput($"Unknown command: {commandName}");
                    break;
            }
        }
    }
}
