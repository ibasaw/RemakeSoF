using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ApplicationLifecycle
{
    /// <summary>
    /// Pure service that listens for commands on the standard input (System.Console) in a background thread.
    /// Queues incoming commands and dispatches them on the Unity main thread via <see cref="ProcessPendingCommands"/>.
    /// Designed for headless/dedicated server builds where no GUI is available.
    /// </summary>
    public class ServerCommandListener
    {
        readonly ConcurrentQueue<string> m_PendingCommands = new();
        Thread m_ListenerThread;
        volatile bool m_IsRunning;

        /// <summary>
        /// Invoked on the main thread when a command is processed.
        /// </summary>
        public event Action<string> OnCommandReceived;

        /// <summary>
        /// Starts the background thread that reads from System.Console.
        /// </summary>
        public void Start()
        {
            if (m_IsRunning)
            {
                return;
            }

            m_IsRunning = true;
            m_ListenerThread = new Thread(ListenForInput)
            {
                IsBackground = true,
                Name = "ServerCommandListener"
            };
            m_ListenerThread.Start();
            Debug.Log("[ServerCommandListener] Started listening for console input.");
        }

        /// <summary>
        /// Stops the background listener thread.
        /// </summary>
        public void Stop()
        {
            m_IsRunning = false;
            Debug.Log("[ServerCommandListener] Stopped listening for console input.");
        }

        /// <summary>
        /// Must be called from the Unity main thread (e.g. in Update) to process queued commands.
        /// </summary>
        public void ProcessPendingCommands()
        {
            while (m_PendingCommands.TryDequeue(out string command))
            {
                _ = ExecuteCommand(command);
            }
        }

        /// <summary>
        /// Executes a single command string. Can be called directly for programmatic command execution.
        /// Returns the result string of the command.
        /// </summary>
        public string ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return string.Empty;
            }

            string trimmed = command.Trim();
            string lower = trimmed.ToLower();

            string result;
            switch (lower)
            {
                case "help":
                    result = PrintHelp();
                    break;
                case "status":
                    result = PrintStatus();
                    break;
                case "quit":
                case "exit":
                    result = HandleQuit();
                    break;
                case "players":
                    result = PrintPlayerCount();
                    break;
                case "shutdown":
                    result = HandleShutdown();
                    break;
                default:
                    result = $"Unknown command: '{trimmed}'. Type 'help' for a list of commands.";
                    OnCommandReceived?.Invoke(trimmed);
                    break;
            }

            Debug.Log($"[ServerCommandListener] {result}");
            return result;
        }

        void ListenForInput()
        {
            try
            {
                while (m_IsRunning)
                {
                    string line = Console.ReadLine();
                    if (line != null)
                    {
                        m_PendingCommands.Enqueue(line);
                    }
                    else
                    {
                        // Console.ReadLine returns null when input stream is closed
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerCommandListener] Console read thread stopped: {ex.Message}");
            }
        }

        string PrintHelp()
        {
            return "Available commands:\n  help      - Show this help message\n  status    - Show server status\n  players   - Show connected player count\n  shutdown  - Gracefully shut down the server\n  quit/exit - Quit the application";
        }

        string PrintStatus()
        {
            bool isServerRunning = Unity.Netcode.NetworkManager.Singleton != null
                && Unity.Netcode.NetworkManager.Singleton.IsServer;
            return $"Server running: {isServerRunning} | Target framerate: {Application.targetFrameRate}";
        }

        string PrintPlayerCount()
        {
            Unity.Netcode.NetworkManager networkManager = Unity.Netcode.NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsServer)
            {
                int count = networkManager.ConnectedClientsIds.Count;
                return $"Connected players: {count}";
            }

            return "Server is not running.";
        }

        string HandleShutdown()
        {
            ApplicationEntryPoint appEntry = ApplicationEntryPoint.Singleton;
            if (appEntry != null)
            {
                appEntry.ConnectionManager.RequestShutdown();
                return "Initiating graceful server shutdown...";
            }

            return "ApplicationEntryPoint not found, cannot shutdown gracefully.";
        }

        string HandleQuit()
        {
            Stop();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return "Quitting application...";
        }

        /// <summary>
        /// Clears pending commands and stops the listener.
        /// </summary>
        public void ClearCache()
        {
            Stop();
            while (m_PendingCommands.TryDequeue(out _)) { }
        }
    }
}
