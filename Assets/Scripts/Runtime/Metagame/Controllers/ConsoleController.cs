using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class ConsoleController : Controller<MetagameApplication>
    {
        ConsoleView View => App.View.ConsoleView;

        public InputActionReference toggleConsoleAction;

        void Awake()
        {
            Debug.Log("ConsoleController Awake");
            AddListener<SubmitConsoleCommandEvent>(OnConsoleCommand);
            AddListener<ToggleConsoleEvent>(OnToggleConsole);
        }

        void OnEnable()
        {
            // Use the existing InputAction from your Input Actions Asset
            toggleConsoleAction.action.performed += OnToggleConsoleAction;
            toggleConsoleAction.action.Enable();
        }

        void OnDisable()
        {
            toggleConsoleAction.action.performed -= OnToggleConsoleAction;
            toggleConsoleAction.action.Disable();
        }

        private void OnToggleConsoleAction(InputAction.CallbackContext ctx)
        {
            Broadcast(new ToggleConsoleEvent());
        }

        internal override void RemoveListeners()
        {
            RemoveListener<SubmitConsoleCommandEvent>(OnConsoleCommand);
            RemoveListener<ToggleConsoleEvent>(OnToggleConsole);
        }

        private void OnToggleConsole(ToggleConsoleEvent evt)
        {
            // Optional: Logging or state changes
            Debug.Log("Console toggled.");
            View.Toggle();
        }

        private void OnConsoleCommand(SubmitConsoleCommandEvent evt)
        {
            string cmd = evt.command;

            View.AddOutput($"> {cmd}");

            switch (cmd.ToLower())
            {
                case "help":
                    View.AddOutput("Commands: help, ping, clear");
                    break;

                case "ping":
                    View.AddOutput("pong");
                    break;

                case "clear":
                    View.ClearConsole();
                    break;

                default:
                    View.AddOutput($"Unknown command: '{cmd}'");
                    break;
            }
        }      
    }
}
