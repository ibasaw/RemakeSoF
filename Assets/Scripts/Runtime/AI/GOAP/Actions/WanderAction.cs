using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Wandert zu einem zufaelligen Zielpunkt.
    /// Keine Vorbedingungen — dient als Fallback-Exploration fuer den Hider.
    /// Effekt: Erhoehung von IsSafe (herumwandern erhoeht Sicherheit).
    /// </summary>
    public class WanderAction : GoapActionBase<AIActionData>
    {
        /// <summary>Ankunftsschwelle in Metern.</summary>
        private const float k_ArrivalThreshold = 3f;

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

            Vector3 targetPos = data.Target.Position;
            controller.SetMoveTarget(targetPos);

            // XZ-Distanz verwenden — Y kann abweichen (Ziel-Hoehe vs Bot-Bodenhoehe)
            Vector3 agentPos = agent.Transform.position;
            float distance = Mathf.Sqrt(
                (agentPos.x - targetPos.x) * (agentPos.x - targetPos.x)
                + (agentPos.z - targetPos.z) * (agentPos.z - targetPos.z));
            if (distance < k_ArrivalThreshold)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.ContinueOrResolve;
        }

        /// <inheritdoc />
        public override void End(IMonoAgent agent, AIActionData data) { }
    }
}
