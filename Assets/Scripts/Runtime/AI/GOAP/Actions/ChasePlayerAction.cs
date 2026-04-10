using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Bewegt den Seeker-Bot auf den sichtbaren Spieler zu.
    /// Vorbedingung: IsPlayerVisible >= 1.
    /// Effekt: Erhoehung von IsPlayerInRange.
    /// Springt periodisch um durch Quake/SoF2-Bewegungsphysik schneller aufzuholen.
    /// </summary>
    public class ChasePlayerAction : GoapActionBase<AIActionData>
    {
        /// <summary>Minimales Intervall zwischen Chase-Spruengen in Sekunden.</summary>
        private const float k_ChaseJumpIntervalMin = 1.5f;

        /// <summary>Maximales Intervall zwischen Chase-Spruengen in Sekunden.</summary>
        private const float k_ChaseJumpIntervalMax = 3.5f;

        /// <summary>Zeitpunkt des naechsten Chase-Sprungs (Time.time).</summary>
        private float m_NextChaseJumpTime;

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Start(IMonoAgent agent, AIActionData data)
        {
            m_NextChaseJumpTime = Time.time + Random.Range(k_ChaseJumpIntervalMin, k_ChaseJumpIntervalMax);
        }

        /// <inheritdoc />
        public override IActionRunState Perform(IMonoAgent agent, AIActionData data, IActionContext context)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller == null)
            {
                return ActionRunState.Stop;
            }

            // Spieler nicht mehr sichtbar und kein Damage-Reaction → zur letzten bekannten Position laufen
            if (!controller.PlayerSensorDetected && !controller.WasDamagedRecently)
            {
                // Letzte bekannte Position vorhanden → als Zwischen-Checkpoint hinlaufen
                if (controller.LastKnownPlayerPosition.HasValue)
                {
                    Vector3 lastKnown = controller.LastKnownPlayerPosition.Value;
                    float distToLastKnown = Vector3.Distance(
                        new Vector3(agent.transform.position.x, 0f, agent.transform.position.z),
                        new Vector3(lastKnown.x, 0f, lastKnown.z));

                    if (distToLastKnown > 2.5f)
                    {
                        controller.SetMoveTarget(lastKnown);
                        controller.SetLookTarget(lastKnown);
                        return ActionRunState.Continue;
                    }
                }

                // Angekommen oder keine Position → zurueck zu Patrol
                return ActionRunState.Completed;
            }

            // Echtzeit-Sensorposition verwenden (nicht gecachtes GOAP-Target)
            controller.FindNearestPlayerBySensors(out Vector3 direction, out float distance, out bool detected);

            Vector3 playerPos;
            if (detected)
            {
                playerPos = controller.EyePosition + direction * distance;
            }
            else if (controller.WasDamagedRecently && controller.LastAttackerPosition.HasValue)
            {
                playerPos = controller.LastAttackerPosition.Value;
            }
            else if (data.Target != null)
            {
                playerPos = data.Target.Position;
            }
            else
            {
                return ActionRunState.Completed;
            }

            controller.SetMoveTarget(playerPos);
            controller.SetLookTarget(playerPos);

            // Spieler-Aktionen spiegeln (Springen, Schiessen — kein Ducken)
            controller.MirrorNearestPlayerActions();

            // Periodischer Chase-Sprung: Quake/SoF2-Physik gibt Sprung-Bonus auf Geschwindigkeit
            if (Time.time >= m_NextChaseJumpTime)
            {
                controller.SetShouldJump(true);
                m_NextChaseJumpTime = Time.time + Random.Range(k_ChaseJumpIntervalMin, k_ChaseJumpIntervalMax);
            }

            // Horizontale XZ-Distanz fuer Reichweite (3D-Distanz ist wegen Augenhoehe groesser)
            if (controller.PlayerSensorDistanceXZ <= controller.WeaponRangeMeters)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        /// <inheritdoc />
        public override void End(IMonoAgent agent, AIActionData data) { }
    }
}
