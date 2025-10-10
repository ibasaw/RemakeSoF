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
    /// Called when the user clicks the "Login" button on the registration view to switch back to the login view
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
    
    internal class PlayerSignedIn : AppEvent
    {
        public bool Success { get; private set; }
        public string PlayerId { get; private set; }

        public PlayerSignedIn(bool success, string playerId)
        {
            Success = success;
            PlayerId = playerId;
        }
    }
}
