namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    /// <summary>
    /// Console state when the console is inactive and not displaying.
    /// In this state, commands are buffered but not executed.
    /// </summary>
    public class ConsoleInactiveState : ConsoleState
    {
        public override void Enter()
        {
            // Console wird inaktiv - Cleanup kann hier stattfinden
        }

        public override void Exit()
        {
            // Wird aufgerufen beim Wechsel aus diesem State
        }

        public override void Activate()
        {
            // Wechsel zu Active State
            Manager.ChangeState(Manager.m_ConsoleActive);
        }

        public override void ExecuteCommand(string command)
        {
            // In inactive state, commands are not executed
            // Optionally, we could buffer them or ignore them
        }

        public override void AppendOutput(string text)
        {
            // Output wird nicht angezeigt wenn inaktiv
        }
    }
}
