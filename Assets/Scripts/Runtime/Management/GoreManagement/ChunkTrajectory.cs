using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// SoF2-Style TR_GRAVITY Trajectory fuer Gore-Chunks.
    /// Ersetzt Unity-Rigidbody durch manuelle Parabel-Physik (identisch zum SoF2 Original):
    /// position += velocity * dt, velocity.y -= gravity * dt, Raycast fuer Bodenkollision,
    /// Bounce mit bounceFactor. Kein Rigidbody, kein Unity-Physics.
    ///
    /// SoF2 cg_gore.c CG_ProcessChunk:
    ///   trType = TR_GRAVITY
    ///   bounceFactor = 0.2
    ///   Evaluation: BG_EvaluateTrajectory (bg_misc.c)
    /// </summary>
    public class ChunkTrajectory : MonoBehaviour
    {
        /// <summary>Aktuelle Velocity (Welt-Raum).</summary>
        private Vector3 m_Velocity;

        /// <summary>SoF2 Gravitation: 800 QU/s² × 0.0254 = 20.32 m/s².</summary>
        private const float GRAVITY = 20.32f;

        /// <summary>SoF2 bounceFactor fuer Chunks (cg_gore.c).</summary>
        private const float BOUNCE_FACTOR = 0.2f;

        /// <summary>Minimale Velocity-Magnitude bei der der Chunk zur Ruhe kommt.</summary>
        private const float REST_THRESHOLD = 0.1f;

        /// <summary>LayerMask fuer Welt-Kollision (BrushCollision = Layer 9).</summary>
        private static int s_CollisionMask = -1;

        /// <summary>Ob der Chunk zur Ruhe gekommen ist.</summary>
        private bool m_AtRest;

        /// <summary>
        /// Initialisiert die Trajectory mit Start-Velocity.
        /// </summary>
        public void Initialize(Vector3 velocity)
        {
            m_Velocity = velocity;
            m_AtRest = false;

            if (s_CollisionMask < 0)
            {
                // Nur gegen Welt-Geometrie kollidieren (BrushCollision + Default)
                int brushLayer = LayerMask.NameToLayer("BrushCollision");
                if (brushLayer >= 0)
                {
                    s_CollisionMask = 1 << brushLayer;
                }
                else
                {
                    // Fallback: Default layer
                    s_CollisionMask = 1;
                }
            }
        }

        private void Update()
        {
            if (m_AtRest)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            // SoF2 TR_GRAVITY: velocity.y -= gravity * dt (Q3 Half-Step nicht noetig fuer Debris)
            m_Velocity.y -= GRAVITY * dt;

            Vector3 movement = m_Velocity * dt;
            float distance = movement.magnitude;

            if (distance < 0.0001f)
            {
                m_AtRest = true;
                return;
            }

            Vector3 direction = movement / distance;

            // Raycast fuer Kollision mit Welt-Geometrie
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, s_CollisionMask))
            {
                // Bewege bis kurz vor den Aufprall
                transform.position = hit.point + hit.normal * 0.01f;

                // SoF2 Bounce: reflektiere Velocity an der Oberflaeche, skaliere mit bounceFactor
                Vector3 reflected = Vector3.Reflect(m_Velocity, hit.normal);
                m_Velocity = reflected * BOUNCE_FACTOR;

                // Zur Ruhe kommen wenn Velocity zu klein
                if (m_Velocity.sqrMagnitude < REST_THRESHOLD * REST_THRESHOLD)
                {
                    m_Velocity = Vector3.zero;
                    m_AtRest = true;
                }
            }
            else
            {
                // Kein Treffer: frei bewegen
                transform.position += movement;
            }
        }
    }
}
