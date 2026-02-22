using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    public class ConsoleController : Controller<ConsoleManager>
    {
        ConsoleView View => App.View;

        public InputActionReference toggleConsoleAction;

        void Awake()
        {
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

            // Forward all commands to the server via NetworkedCommandBridge
            if (NetworkedCommandBridge.Instance != null)
            {
                NetworkedCommandBridge.Instance.SendCommandToServer(cmd);
            }
            else
            {
                Debug.LogWarning("[ConsoleController] No NetworkedCommandBridge instance found. Cannot send command to server.");
                View.AddWarningOutput("Not connected to a server. Command not sent.");
            }
        }      
    }
}
