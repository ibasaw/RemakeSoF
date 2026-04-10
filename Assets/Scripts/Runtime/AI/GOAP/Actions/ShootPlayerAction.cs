using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Schiesst auf den sichtbaren Spieler in Waffenreichweite.
    /// Vorbedingungen: IsPlayerVisible >= 1, IsPlayerInRange >= 1, HasAmmo >= 1.
    /// Effekt: Erhoehung von EnemyDown.
    /// Bewegt den Bot weiterhin auf den Spieler zu waehrend er feuert.
    /// </summary>
    public class ShootPlayerAction : GoapActionBase<AIActionData>
    {
        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Start(IMonoAgent agent, AIActionData data) { }

        /// <inheritdoc />
        public override IActionRunState Perform(IMonoAgent agent, AIActionData data, IActionContext context)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller == null)
            {
                return ActionRunState.Stop;
            }

            // Spieler nicht mehr sichtbar und nicht im Gedaechtnis → zur letzten bekannten Position laufen
            if (!controller.PlayerSensorDetected && !controller.WasDamagedRecently)
            {
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

                return ActionRunState.Completed;
            }

            // Echtzeit-Sensorposition verwenden fuer praezises Zielen
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

            controller.SetLookTarget(playerPos);
            controller.SetMoveTarget(playerPos);

            // Nur feuern wenn der Spieler direkt sichtbar ist (nicht nur im Gedaechtnis)
            // Sonst treffen die Kugeln nur Waende
            if (controller.PlayerDirectlyVisible)
            {
                controller.SetShouldAttack(true);
            }

            // Spieler-Aktionen spiegeln (Springen, Ducken)
            controller.MirrorNearestPlayerActions();

            return ActionRunState.Continue;
        }

        /// <inheritdoc />
        public override void End(IMonoAgent agent, AIActionData data)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller != null)
            {
                controller.SetShouldAttack(false);
            }
        }
    }
}
