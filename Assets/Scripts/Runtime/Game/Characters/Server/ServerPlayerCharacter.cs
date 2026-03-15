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

        /// <summary>
        /// Physics BoxCollider fuer server-seitige Player-Player Collision (SoF2 AABB).
        /// Wird bei SetCapsuleDimensions aktualisiert.
        /// </summary>
        private BoxCollider m_PhysicsCollider;

        /// <summary>
        /// Gibt an ob der Server-Character bereit ist Commands zu verarbeiten.
        /// Wird erst gesetzt nachdem die Map geladen und eine Spawn-Position zugewiesen wurde.
        /// </summary>
        private bool m_IsReady;

        /// <summary>
        /// SoF2 Eye-Height Ratio: ViewHeight/TotalHeight = 72/89 ≈ 0.809.
        /// Standing: DEFAULT_VIEWHEIGHT(26) + playerMins.z(46) = 72 von 89 QU.
        /// Gilt proportional auch fuer Crouching.
        /// </summary>
        private const float EYE_HEIGHT_RATIO = 72f / 89f;

        /// <summary>
        /// Berechnet die Eye-Position des Spielers auf dem Server.
        /// Position ist am Fuss (feet), Eye-Height wird proportional aus CapsuleHeight berechnet.
        /// </summary>
        public Vector3 GetEyePosition()
        {
            float eyeHeight = m_Simulation.CapsuleHeight * EYE_HEIGHT_RATIO;
            return transform.position + new Vector3(0f, eyeHeight, 0f);
        }

        /// <summary>
        /// Physics BoxCollider fuer server-seitige Hit-Detection temporaer deaktivieren.
        /// Verhindert Self-Hit bei Raycast.
        /// </summary>
        public void SetPhysicsColliderEnabled(bool enabled)
        {
            if (m_PhysicsCollider != null)
            {
                m_PhysicsCollider.enabled = enabled;
            }
        }

        /// <summary>
        /// Initialisiert die Server-seitige Physik und den Collision-Collider.
        /// Wird von NetworkedPlayerCharacter.OnServerSpawn() aufgerufen,
        /// damit der BoxCollider nur auf dem Server erstellt wird.
        /// </summary>
        public void InitializeServer()
        {
            // Default Capsule-Dimensionen setzen (bis Client aktuelle Werte sendet)
            m_Simulation.SetCapsuleDimensions(
                k_DefaultCapsuleHeight,
                k_DefaultCapsuleRadius,
                k_DefaultCapsuleCenter
            );

            // Physics BoxCollider fuer Player-Player Collision (SoF2 AABB)
            m_PhysicsCollider = gameObject.AddComponent<BoxCollider>();
            m_PhysicsCollider.size = new Vector3(k_DefaultCapsuleRadius * 2f, k_DefaultCapsuleHeight, k_DefaultCapsuleRadius * 2f);
            m_PhysicsCollider.center = k_DefaultCapsuleCenter;
        }

        /// <summary>
        /// Verarbeitet einen PlayerCommand und führt die SoF2-Physik autoritativ aus.
        /// Wird von NetworkedPlayerCharacter aufgerufen wenn ein Command vom Client ankommt.
        /// </summary>
        /// <param name="cmd">Der PlayerCommand vom Client (Input-Daten).</param>
        /// <returns>ServerMovementAck mit autoritativer Position und State.</returns>
        /// <summary>
        /// Markiert den Server-Character als bereit für Command-Verarbeitung.
        /// Wird aufgerufen nachdem die Map geladen und die Spawn-Position zugewiesen wurde.
        /// </summary>
        public void SetReady()
        {
            m_IsReady = true;
        }

        public ServerMovementAck ProcessCommand(PlayerCommand cmd)
        {
            if (!m_IsReady)
            {
                return new ServerMovementAck
                {
                    LastProcessedSequence = cmd.SequenceNumber,
                    Position = transform.position,
                    Velocity = Vector3.zero,
                    IsGrounded = true,
                    IsJumping = false,
                    IsCrouching = false,
                };
            }

            // DeltaTime validieren (Anti-Cheat: unrealistische Werte clampen)
            cmd.DeltaTime = Mathf.Clamp(cmd.DeltaTime, 0f, 0.1f);
            cmd.MoveInput = Vector2.ClampMagnitude(cmd.MoveInput, 1f);

            // Eigenen Collider deaktivieren damit BoxCast sich nicht selbst trifft
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

            // Server-Collider an Crouch-State anpassen
            if (m_PhysicsCollider != null)
            {
                float h = m_Simulation.CapsuleHeight;
                float r = m_Simulation.CapsuleRadius;
                m_PhysicsCollider.size = new Vector3(r * 2f, h, r * 2f);
                m_PhysicsCollider.center = m_Simulation.CapsuleCenter;
            }

            return new ServerMovementAck
            {
                LastProcessedSequence = cmd.SequenceNumber,
                Position = position,
                Velocity = m_Simulation.Velocity,
                IsGrounded = m_Simulation.IsGrounded,
                IsJumping = m_Simulation.IsJumping,
                IsCrouching = m_Simulation.IsCrouching,
            };
        }

        /// <summary>
        /// Setzt Capsule-Dimensionen auf der Server-Simulation.
        /// Wird aufgerufen wenn der Client seine Bone-berechneten Capsule-Daten sendet.
        /// </summary>
        public void SetCapsuleDimensions(float height, float radius, Vector3 center)
        {
            m_Simulation.SetCapsuleDimensions(height, radius, center);

            // Physics BoxCollider aktualisieren
            if (m_PhysicsCollider != null)
            {
                m_PhysicsCollider.size = new Vector3(radius * 2f, height, radius * 2f);
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
