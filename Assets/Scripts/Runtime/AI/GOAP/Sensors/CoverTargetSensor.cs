using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Findet eine NavMesh-Position in einem Ring um den Bot,
    /// von der aus keine Sichtlinie zur aktuellen oder zuletzt bekannten Spielerposition besteht.
    /// Wird vom Hider genutzt um sich aktiv hinter Geometrie zu verstecken statt einfach wegzulaufen.
    /// Liefert kein Ziel zurueck wenn kein Spieler bekannt ist oder kein Cover gefunden wird —
    /// der Planner faellt dann auf FleeAction / WanderAction zurueck.
    /// </summary>
    public class CoverTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Augenhoehen-Offset fuer Line-of-Sight-Test (passt zu k_SensorHeightOffset im Controller).</summary>
        private const float k_EyeHeightOffset = 1.83f;

        /// <summary>Minimaler Suchradius fuer Cover-Kandidaten (Meter).</summary>
        private const float k_MinSearchRadius = 5f;

        /// <summary>Maximaler Suchradius fuer Cover-Kandidaten (Meter).</summary>
        private const float k_MaxSearchRadius = 14f;

        /// <summary>Anzahl Kandidaten-Winkel pro Tick.</summary>
        private const int k_CandidateCount = 8;

        /// <summary>Maximaler vertikaler NavMesh-Sample-Radius in Metern.</summary>
        private const float k_NavMeshSampleHeight = 5f;

        /// <summary>Spieler-Layer (passt zu k_PlayerLayer im AIBotController).</summary>
        private const int k_PlayerLayer = 7;

        /// <summary>Sensor-Timer: 5 Hz reicht, Cover-Suche ist nicht zeitkritisch.</summary>
        public override ISensorTimer Timer => SensorTimer.Interval(0.2f);

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Update() { }

        /// <inheritdoc />
        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            AIBotController controller = references.GetCachedComponent<AIBotController>();
            if (controller == null)
            {
                return existingTarget;
            }

            controller.EnsureSensorsTicked();

            // Bedrohungsposition ermitteln: aktive Sensor-Erkennung > Last-Known > Letzter Angreifer
            if (!TryResolveThreatPosition(controller, out Vector3 threatPos))
            {
                return existingTarget;
            }

            Vector3 botPos = controller.transform.position;
            Vector3 threatEye = threatPos + new Vector3(0f, k_EyeHeightOffset, 0f);

            // Ring-Sampling um den Bot herum, bevorzugt Kandidaten weg vom Spieler
            Vector3 awayDir = (botPos - threatPos);
            awayDir.y = 0f;
            float baseAngleDeg = awayDir.sqrMagnitude > 0.01f
                ? Mathf.Atan2(awayDir.x, awayDir.z) * Mathf.Rad2Deg
                : Random.Range(0f, 360f);

            int playerLayerMask = 1 << k_PlayerLayer;

            for (int i = 0; i < k_CandidateCount; i++)
            {
                // Verteile Kandidaten in einem 240deg-Bogen rund um die "weg-vom-Spieler"-Richtung
                float spread = ((i / (float)(k_CandidateCount - 1)) - 0.5f) * 240f;
                float angleDeg = baseAngleDeg + spread;
                float radius = Random.Range(k_MinSearchRadius, k_MaxSearchRadius);

                Vector3 dir = new(Mathf.Sin(angleDeg * Mathf.Deg2Rad), 0f, Mathf.Cos(angleDeg * Mathf.Deg2Rad));
                Vector3 candidate = botPos + dir * radius;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, k_NavMeshSampleHeight, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 candidateEye = hit.position + new Vector3(0f, k_EyeHeightOffset, 0f);

                // Cover gilt wenn die Linie zur Spieler-Augenhoehe von Geometrie blockiert wird
                // (alle Layer ausser Spieler) — Trigger werden ignoriert
                if (Physics.Linecast(candidateEye, threatEye, ~playerLayerMask, QueryTriggerInteraction.Ignore))
                {
                    return UpdateOrCreate(existingTarget, hit.position);
                }
            }

            // Kein Cover gefunden → kein neues Ziel; Planner waehlt FleeAction
            return existingTarget;
        }

        /// <summary>Ermittelt die aktuell zu vermeidende Spielerposition.</summary>
        private static bool TryResolveThreatPosition(AIBotController controller, out Vector3 position)
        {
            controller.FindNearestPlayerBySensors(out Vector3 direction, out float distance, out bool detected);
            if (detected)
            {
                position = controller.EyePosition + direction * distance;
                return true;
            }

            if (controller.LastKnownPlayerPosition.HasValue)
            {
                position = controller.LastKnownPlayerPosition.Value;
                return true;
            }

            if (controller.WasDamagedRecently && controller.LastAttackerPosition.HasValue)
            {
                position = controller.LastAttackerPosition.Value;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        /// <summary>Wiederverwendet oder erstellt ein PositionTarget.</summary>
        private static ITarget UpdateOrCreate(ITarget existing, Vector3 position)
        {
            if (existing is PositionTarget posTarget)
            {
                posTarget.SetPosition(position);
                return posTarget;
            }

            return new PositionTarget(position);
        }
    }
}
