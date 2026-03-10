using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitiger Player-Character Controller.
    /// Führt die SoF2-Physik-Simulation autoritativ aus:
    /// empfängt PlayerCommands vom Client, simuliert identische Physik,
    /// und liefert die autoritative Position zurück.
    /// Fängt AnimationEvents ab die auf dem Server ankommen (wegen NetworkAnimator).
    /// </summary>
    [RequireComponent(typeof(NetworkedPlayerCharacter))]
    public class ServerPlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        private NetworkedPlayerCharacter m_NetworkedPlayerCharacter;

        /// <summary>
        /// Server-seitige Physik-Simulation (identisch mit Client-Prediction).
        /// Direkt serialisiert — alle Physik-Parameter im Inspector.
        /// </summary>
        [SerializeField]
        private PlayerPhysicsSimulation m_Simulation = new();

        /// <summary>Default Capsule-Höhe bis Client aktuelle Werte sendet.</summary>
        private const float k_DefaultCapsuleHeight = 1.8f;

        /// <summary>Default Capsule-Radius bis Client aktuelle Werte sendet.</summary>
        private const float k_DefaultCapsuleRadius = 0.25f;

        /// <summary>Default Capsule-Center bis Client aktuelle Werte sendet.</summary>
        private static readonly Vector3 k_DefaultCapsuleCenter = new(0f, 0.9f, 0f);

        /// <summary>Default Ground-Check-Distanz bis Client aktuelle Werte sendet.</summary>
        private const float k_DefaultGroundCheckDistance = 0.1f;

        /// <summary>
        /// Physics CapsuleCollider fuer server-seitige Player-Player Collision.
        /// Wird bei SetCapsuleDimensions aktualisiert.
        /// </summary>
        private CapsuleCollider m_PhysicsCollider;

        /// <summary>
        /// Initialisiert die Server-seitige Physik und den Collision-Collider.
        /// Wird von NetworkedPlayerCharacter.OnServerSpawn() aufgerufen,
        /// damit der CapsuleCollider nur auf dem Server erstellt wird.
        /// </summary>
        public void InitializeServer()
        {
            // Default Capsule-Dimensionen setzen (bis Client aktuelle Werte sendet)
            m_Simulation.SetCapsuleDimensions(
                k_DefaultCapsuleHeight,
                k_DefaultCapsuleRadius,
                k_DefaultCapsuleCenter,
                k_DefaultGroundCheckDistance
            );

            // Physics CapsuleCollider fuer Player-Player Collision
            m_PhysicsCollider = gameObject.AddComponent<CapsuleCollider>();
            m_PhysicsCollider.height = k_DefaultCapsuleHeight;
            m_PhysicsCollider.radius = k_DefaultCapsuleRadius;
            m_PhysicsCollider.center = k_DefaultCapsuleCenter;
        }

        /// <summary>
        /// Verarbeitet einen PlayerCommand und führt die SoF2-Physik autoritativ aus.
        /// Wird von NetworkedPlayerCharacter aufgerufen wenn ein Command vom Client ankommt.
        /// </summary>
        /// <param name="cmd">Der PlayerCommand vom Client (Input-Daten).</param>
        /// <returns>ServerMovementAck mit autoritativer Position und State.</returns>
        public ServerMovementAck ProcessCommand(PlayerCommand cmd)
        {
            // DeltaTime validieren (Anti-Cheat: unrealistische Werte clampen)
            cmd.DeltaTime = Mathf.Clamp(cmd.DeltaTime, 0f, 0.1f);
            cmd.MoveInput = Vector2.ClampMagnitude(cmd.MoveInput, 1f);

            // Eigenen Collider deaktivieren damit CapsuleCast sich nicht selbst trifft
            if (m_PhysicsCollider != null)
            {
                m_PhysicsCollider.enabled = false;
            }

            Vector3 position = transform.position;
            m_Simulation.Simulate(ref position, cmd);
            transform.position = position;

            // Eigenen Collider wieder aktivieren
            if (m_PhysicsCollider != null)
            {
                m_PhysicsCollider.enabled = true;
            }

            return new ServerMovementAck
            {
                LastProcessedSequence = cmd.SequenceNumber,
                Position = position,
                Velocity = m_Simulation.Velocity,
                IsGrounded = m_Simulation.IsGrounded,
                IsJumping = m_Simulation.IsJumping,
            };
        }

        /// <summary>
        /// Setzt Capsule-Dimensionen auf der Server-Simulation.
        /// Wird aufgerufen wenn der Client seine Bone-berechneten Capsule-Daten sendet.
        /// </summary>
        public void SetCapsuleDimensions(float height, float radius, Vector3 center, float groundCheckDist)
        {
            m_Simulation.SetCapsuleDimensions(height, radius, center, groundCheckDist);

            // Physics CapsuleCollider aktualisieren
            if (m_PhysicsCollider != null)
            {
                m_PhysicsCollider.height = height;
                m_PhysicsCollider.radius = radius;
                m_PhysicsCollider.center = center;
            }

            Debug.Log($"[ServerPlayerCharacter] Capsule-Dimensionen vom Client empfangen — Height: {height:F2}, Radius: {radius:F2}");
        }

        /// <summary>
        /// AnimationEvent: Footstep (wird auf Server ignoriert, aber benötigt wegen NetworkAnimator).
        /// </summary>
        private void OnFootstep(AnimationEvent animationEvent) { }

        /// <summary>
        /// AnimationEvent: Land (wird auf Server ignoriert, aber benötigt wegen NetworkAnimator).
        /// </summary>
        private void OnLand(AnimationEvent animationEvent) { }
    }
}
