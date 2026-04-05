using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitige AI-Logik fuer Bot-Characters.
    /// Wird nur auf dem Server ausgefuehrt.
    /// Steuert Bewegung und Entscheidungen des AI-Bots.
    /// Nutzt dieselbe PlayerPhysicsSimulation wie menschliche Spieler (SoF2/Quake3 Physik).
    /// Physics-Collider wird von ClientColliderSystem bereitgestellt (identisch auf Server und Client).
    /// Spaeter: GOAP/EANN Integration fuer intelligente Entscheidungen.
    /// </summary>
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ServerAICharacter : MonoBehaviour
    {
        /// <summary>SoF2/Quake3 Physik-Simulation (Gravity, Ground-Trace, Friction, etc.).</summary>
        [SerializeField]
        private PlayerPhysicsSimulation m_Simulation = new();

        /// <summary>
        /// Referenz auf das ClientColliderSystem, das den Physics-BoxCollider verwaltet.
        /// Wird von NetworkedAICharacter nach Visual-Load gesetzt.
        /// </summary>
        private ClientColliderSystem m_ColliderSystem;

        /// <summary>
        /// Referenz auf die vernetzte AI-Character-Komponente.
        /// </summary>
        [SerializeField]
        private NetworkedAICharacter m_NetworkedAICharacter;

        /// <summary>
        /// Ob der AI-Character bereit ist zu agieren (Map geladen, Spawn-Position gesetzt).
        /// </summary>
        private bool m_IsReady;

        /// <summary>
        /// Initialisiert die Physik-Simulation.
        /// Capsule-Dimensionen werden spaeter vom ClientColliderSystem uebernommen.
        /// </summary>
        private void InitializePhysics()
        {
            m_Simulation.GroundMask = ~0;
        }

        /// <summary>
        /// Setzt die Referenz auf das ClientColliderSystem und uebernimmt dessen Capsule-Dimensionen
        /// fuer die Physik-Simulation. Wird von NetworkedAICharacter nach Visual-Load aufgerufen.
        /// </summary>
        public void SetColliderSystem(ClientColliderSystem colliderSystem)
        {
            m_ColliderSystem = colliderSystem;

            if (m_ColliderSystem != null)
            {
                m_Simulation.SetCapsuleDimensions(
                    m_ColliderSystem.GetCurrentCapsuleHeight(),
                    m_ColliderSystem.GetCurrentCapsuleRadius(),
                    m_ColliderSystem.GetCurrentCapsuleCenter()
                );
            }
        }

        /// <summary>
        /// Markiert den AI-Character als bereit.
        /// Wird von NetworkedAICharacter nach Spawn-Position-Zuweisung aufgerufen.
        /// </summary>
        public void SetReady()
        {
            if (!m_IsReady)
            {
                InitializePhysics();
            }

            m_IsReady = true;
            m_Simulation.SetState(Vector3.zero, true, false, false);
            Debug.Log("[ServerAICharacter] AI-Bot ist bereit.");
        }

        private void Update()
        {
            if (!m_IsReady)
            {
                return;
            }

            SimulatePhysics();

            // TODO: AI-Logik hier implementieren
            // Phase 1: Idle (steht einfach rum)
            // Phase 2: Random-Walk / Waypoint-Patrol
            // Phase 3: GOAP + EANN Integration
        }

        /// <summary>
        /// Fuehrt einen Physik-Schritt mit leerem PlayerCommand aus.
        /// Identische Physik wie menschliche Spieler (Gravity, Ground-Trace, Friction).
        /// Spaeter: AI-Logik fuellt MoveInput/Buttons fuer Bewegung.
        /// </summary>
        private void SimulatePhysics()
        {
            PlayerCommand cmd = new()
            {
                MoveInput = Vector2.zero,
                YawAngle = transform.eulerAngles.y,
                PitchAngle = 0f,
                MoveYawAngle = transform.eulerAngles.y,
                Buttons = 0,
                DeltaTime = Time.deltaTime,
                SequenceNumber = 0,
            };

            BoxCollider physicsCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;

            if (physicsCollider != null)
            {
                physicsCollider.enabled = false;
            }

            Vector3 position = transform.position;
            m_Simulation.Simulate(ref position, cmd);
            transform.position = position;

            if (physicsCollider != null)
            {
                physicsCollider.enabled = true;
            }
        }
    }
}
