using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Flieht vom sichtbaren Spieler weg.
    /// Vorbedingung: IsPlayerVisible >= 1.
    /// Effekt: Erhoehung von IsSafe.
    /// Wird vom Hider genutzt wenn ein Seeker erkannt wird.
    /// </summary>
    public class FleeAction : GoapActionBase<AIActionData>
    {
        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Start(IMonoAgent agent, AIActionData data) { }

        /// <inheritdoc />
        public override IActionRunState Perform(IMonoAgent agent, AIActionData data, IActionContext context)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller == null || data.Target == null)
            {
                return ActionRunState.Stop;
            }

            controller.SetMoveTarget(data.Target.Position);
            controller.SetShouldJump(true);

            return ActionRunState.ContinueOrResolve;
        }

        /// <inheritdoc />
        public override void End(IMonoAgent agent, AIActionData data)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller != null)
            {
                controller.SetShouldJump(false);
            }
        }
    }
}
