namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    /// <summary>
    /// Base class representing a console state.
    /// </summary>
    public abstract class ConsoleState : Core.State<ConsoleManager>
    {
        public override abstract void Enter();

        public override abstract void Exit();

        /// <summary>
        /// Called when a command should be executed.
        /// </summary>
        public virtual void ExecuteCommand(string command) { }

        /// <summary>
        /// Called when the console should be activated.
        /// </summary>
        public virtual void Activate() { }

        /// <summary>
        /// Called when the console should be deactivated.
        /// </summary>
        public virtual void Deactivate() { }

        /// <summary>
        /// Called to append text to the console output.
        /// </summary>
        public virtual void AppendOutput(string text) { }
    }
}
