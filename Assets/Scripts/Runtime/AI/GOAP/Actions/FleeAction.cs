using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Flieht vom sichtbaren Spieler weg.
    /// Vorbedingung: IsPlayerVisible >= 1.
    /// Effekt: Erhoehung von IsSafe.
    /// Wird vom Hider genutzt wenn ein Seeker erkannt wird.
    /// Springt periodisch (nicht jeden Frame) um durch Quake/SoF2-Physik schneller zu fluechten.
    /// </summary>
    public class FleeAction : GoapActionBase<AIActionData>
    {
        /// <summary>Minimales Intervall zwischen Flucht-Spruengen in Sekunden.</summary>
        private const float k_FleeJumpIntervalMin = 1.2f;

        /// <summary>Maximales Intervall zwischen Flucht-Spruengen in Sekunden.</summary>
        private const float k_FleeJumpIntervalMax = 2.5f;

        /// <summary>Zeitpunkt des naechsten Flucht-Sprungs (Time.time).</summary>
        private float m_NextJumpTime;

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Start(IMonoAgent agent, AIActionData data)
        {
            m_NextJumpTime = Time.time + Random.Range(k_FleeJumpIntervalMin, k_FleeJumpIntervalMax);
        }

        /// <inheritdoc />
        public override IActionRunState Perform(IMonoAgent agent, AIActionData data, IActionContext context)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller == null || data.Target == null)
            {
                return ActionRunState.Stop;
            }

            controller.SetMoveTarget(data.Target.Position);

            // Periodisch springen — kein dauerhaftes Bunnyhopping
            if (Time.time >= m_NextJumpTime)
            {
                controller.SetShouldJump(true);
                m_NextJumpTime = Time.time + Random.Range(k_FleeJumpIntervalMin, k_FleeJumpIntervalMax);
            }
            else
            {
                controller.SetShouldJump(false);
            }

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
