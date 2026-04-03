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
        /// SoF2 Eye-Height Ratio: ViewHeight/TotalHeight = 72/89.
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

            // GroundMask: Player + Hitbox Layer ausschliessen.
            // Player-Layer wuerde Self-Collision verursachen (eigener BoxCollider wird von
            // OverlapBox/BoxCast erkannt → ResolvePenetration drueckt Spieler weg).
            // Hitbox-Layer sind zwar Trigger (QueryTriggerInteraction.Ignore filtert),
            // aber expliziter Ausschluss ist sicherer.
            m_Simulation.GroundMask = ~LayerMask.GetMask("Player");

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

        /// <summary>
        /// Setzt serverseitige Bewegungswerte fuer einen sauberen Respawn zurueck.
        /// Verhindert, dass alte Fall-/Sprung-Velocity in die neue Runde uebernommen wird.
        /// </summary>
        public void ResetForRespawn()
        {
            m_Simulation.SetState(Vector3.zero, true, false, false, 0f);
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
                KnockbackTime = m_Simulation.KnockbackTime,
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

        // ===== SoF2 Knockback (g_combat.c:543) =====

        /// <summary>
        /// SoF2 g_knockback Cvar Default (700 QU/s Basisgeschwindigkeit).
        /// </summary>
        private const float SOF2_KNOCKBACK = 700f;

        /// <summary>
        /// SoF2 Spieler-Masse Default (200 Units).
        /// </summary>
        private const float SOF2_MASS = 200f;

        /// <summary>
        /// SoF2 Unit-Konvertierung (1 QU = 0.0254m).
        /// </summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>
        /// SoF2 Schwerkraft-Skalierung fuer Knockback (0.8 bei g_gravity > 0).
        /// </summary>
        private const float SOF2_KNOCKBACK_GRAVITY_SCALE = 0.8f;

        /// <summary>
        /// Wendet SoF2-authentischen Knockback auf den Spieler an.
        /// Formel: kvel = dir * g_knockback * knockback / mass(200) * 0.8
        /// Setzt pm_time fuer Knockback-Schutz (Spieler kann Momentum nicht sofort canceln).
        /// Velocity-Aenderung wird beim naechsten ServerMovementAck an den Client propagiert.
        /// </summary>
        /// <param name="direction">Normalisierte Richtung der Kraft (Explosion → Spieler).</param>
        /// <param name="knockback">Knockback-Wert (bereits auf 200 geclampt).</param>
        /// <param name="gKnockback">SoF2 g_knockback Cvar (default 700). Konfigurierbar pro Waffe.</param>
        public void ApplyKnockback(Vector3 direction, float knockback, float gKnockback = SOF2_KNOCKBACK)
        {
            // SoF2: VectorScale(newDir, g_knockback * knockback / mass * 0.8, kvel)
            // Ergebnis ist in QU/s → konvertieren zu Unity m/s
            Vector3 kvel = direction * (gKnockback * knockback / SOF2_MASS * SOF2_KNOCKBACK_GRAVITY_SCALE * SOF2_UNIT_SCALE);

            m_Simulation.Velocity += kvel;

            // SoF2 g_combat.c:580 — set PMF_TIME_KNOCKBACK timer
            // Duration: knockback * 2 ms, clamped to [50, 200] ms
            // Prevents player from cancelling knockback momentum via friction
            if (m_Simulation.KnockbackTime <= 0f)
            {
                float knockbackTimeMs = Mathf.Clamp(knockback * 2f, 50f, 200f);
                m_Simulation.KnockbackTime = knockbackTimeMs * 0.001f;
            }

            Debug.Log($"[Knockback] dir={direction} kb={knockback} kvel={kvel} " +
                       $"vel={m_Simulation.Velocity} kbTime={m_Simulation.KnockbackTime:F3}s");
        }
    }
}
