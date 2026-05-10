using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Bewegt den Hider zu einem Cover-Punkt ohne Sichtlinie zum Spieler.
    /// Vorbedingung: IsPlayerVisible >= 1 (nur Cover suchen wenn Bedrohung bekannt).
    /// Effekt: Erhoehung von IsSafe.
    /// Niedrigere Cost als FleeAction → wird bevorzugt wenn ein Cover-Ziel verfuegbar ist.
    /// Faellt zurueck auf FleeAction wenn der CoverTargetSensor kein gueltiges Ziel liefert.
    /// </summary>
    public class TakeCoverAction : GoapActionBase<AIActionData>
    {
        /// <summary>Ankunftsschwelle am Cover-Punkt (Meter).</summary>
        private const float k_ArrivalThreshold = 1.5f;

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

            Vector3 botPos = agent.Transform.position;
            float distXZ = Mathf.Sqrt(
                (botPos.x - targetPos.x) * (botPos.x - targetPos.x)
                + (botPos.z - targetPos.z) * (botPos.z - targetPos.z));

            if (distXZ <= k_ArrivalThreshold)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.ContinueOrResolve;
        }

        /// <inheritdoc />
        public override void End(IMonoAgent agent, AIActionData data) { }
    }
}
