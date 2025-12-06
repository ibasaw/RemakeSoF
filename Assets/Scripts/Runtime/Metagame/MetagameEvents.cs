namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class EnterMatchmakerQueueEvent : AppEvent
    {
        public string QueueName { get; private set; }

        public EnterMatchmakerQueueEvent(string queueName)
        {
            QueueName = queueName;
        }
    }

    internal class ExitMatchmakerQueueEvent : AppEvent { }
    
    internal class EnterIPConnectionEvent : AppEvent { }

    internal class ExitIPConnectionEvent : AppEvent { }

    /// <summary>
    /// Called when the user clicks the "Register" button on the login view to switch to the registration view
    /// </summary>
    internal class ChangeToRegisterEvent : AppEvent { }
    
    /// <summary>
    /// Called when the user clicks the "Back" button on the registration view to switch back to the login view
    /// </summary>
    internal class ChangeToLoginEvent : AppEvent { }

    internal class JoinThroughDirectIPEvent : AppEvent
    {
        public string ipAddress;
        public ushort port;
    }
    
    internal class CancelConnectionEvent: AppEvent { }

    /// <summary>
    /// Called when a match is entered (I.E: after matchmaking finds enough players)
    /// </summary>
    internal class MatchEnteredEvent : AppEvent { }

    /// <summary>
    /// Called when the user clicks the "Login" button on the login view
    /// </summary>
    internal class PlayerLoginEvent : AppEvent
    {
        public string username;
        public string password;
    }

    /// <summary>
    /// Called when a player hit the toggle console hotkey (default: 'k')
    /// </summary>
    internal class ToggleConsoleEvent : AppEvent { }
    
    /// <summary>
    /// Called when the user submits a command in the console
    /// </summary>
    internal class SubmitConsoleCommandEvent : AppEvent
    {
        public string command;
    }
    
    /// <summary>
    /// Called when the user clicks the "Register" button on the registration view
    /// </summary>
    internal class PlayerRegisterEvent : AppEvent
    {
        public string username;
        public string password;
        public string confirmPassword;
        public string email;
    }
}
