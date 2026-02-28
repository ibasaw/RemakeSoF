using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// Reine Physik-Komponente für Character-Bewegung.
    /// Kein Netcode, kein Input – nur Bewegungslogik.
    /// </summary>
    public class CharacterMotor : MonoBehaviour
    {
        [SerializeField]
        private float m_Speed = 5f;

        [SerializeField]
        private float m_JumpForce = 10f;

        [SerializeField]
        private float m_SprintMultiplier = 1.5f;

        private Vector3 m_Velocity = Vector3.zero;
        private Rigidbody m_RigidBody;
        private bool m_IsGrounded;

        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            if (m_RigidBody == null)
            {
                Debug.LogError("[CharacterMotor] Rigidbody component not found!");
            }
        }

        private void FixedUpdate()
        {
            // Gravity wird bereits von Rigidbody angewendet
            // Nur horizontale Bewegung setzen
        }

        /// <summary>
        /// Bewege Character in eine Richtung.
        /// </summary>
        /// <param name="direction">Bewegungsrichtung (normalisiert).</param>
        /// <param name="sprint">Ob der Character sprintet.</param>
        public void Move(Vector3 direction, bool sprint = false)
        {
            if (m_RigidBody == null)
            {
                return;
            }

            float speed = m_Speed * (sprint ? m_SprintMultiplier : 1f);
            m_Velocity = direction * speed;

            // Y-Komponente von Rigidbody beibehalten (Gravity)
            m_RigidBody.linearVelocity = new Vector3(m_Velocity.x, m_RigidBody.linearVelocity.y, m_Velocity.z);
        }

        /// <summary>
        /// Sprung durchführen (nur wenn am Boden).
        /// </summary>
        /// <returns>True, wenn der Sprung erfolgreich war.</returns>
        public bool Jump()
        {
            if (m_RigidBody == null || !m_IsGrounded)
            {
                return false;
            }

            m_RigidBody.AddForce(Vector3.up * m_JumpForce, ForceMode.Impulse);
            m_IsGrounded = false;
            return true;
        }

        /// <summary>
        /// Drehe Character um die Y-Achse.
        /// </summary>
        /// <param name="angle">Drehwinkel in Grad.</param>
        public void Rotate(float angle)
        {
            transform.Rotate(0, angle, 0);
        }

        /// <summary>
        /// Setze den Character-Zustand zurück.
        /// </summary>
        public void Reset()
        {
            if (m_RigidBody != null)
            {
                m_RigidBody.linearVelocity = Vector3.zero;
                m_RigidBody.angularVelocity = Vector3.zero;
            }

            m_Velocity = Vector3.zero;
        }

        /// <summary>
        /// Überprüfe ob der Character am Boden ist.
        /// </summary>
        /// <param name="groundLayer">Layer-Maske für Boden.</param>
        public void CheckGroundStatus(LayerMask groundLayer)
        {
            if (m_RigidBody == null)
            {
                return;
            }

            // Raycast nach unten
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            m_IsGrounded = Physics.Raycast(rayStart, Vector3.down, 0.2f, groundLayer);
        }
    }
}
