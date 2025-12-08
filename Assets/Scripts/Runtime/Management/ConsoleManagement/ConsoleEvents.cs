namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    public class ConsoleEvent : AppEvent { }

    public class ConsoleActivatedEvent : ConsoleEvent { }

    public class ConsoleDeactivatedEvent : ConsoleEvent { }

    public class ConsoleOutputEvent : ConsoleEvent
    {
        public string Output { get; set; }
    }

    public class ConsoleCommandExecutedEvent : ConsoleEvent
    {
        public string Command { get; set; }
    }
}
