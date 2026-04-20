using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Wendet initiale Velocity und AngularVelocity auf einen Gore-Chunk-Rigidbody an.
    /// AddComponent&lt;Rigidbody&gt; im selben Frame wie AddForce/linearVelocity ist unzuverlaessig,
    /// weil PhysX den Rigidbody erst im naechsten FixedUpdate registriert.
    /// Dieses MonoBehaviour garantiert, dass die Velocity gesetzt wird, wenn der Rigidbody
    /// vollstaendig im Physics-Backend initialisiert ist.
    /// </summary>
    public class ChunkPhysicsInitializer : MonoBehaviour
    {
        private Vector3 m_Velocity;
        private Vector3 m_AngularVelocity;

        /// <summary>
        /// Setzt die initiale Velocity und AngularVelocity, die im naechsten FixedUpdate angewendet werden.
        /// </summary>
        public void Initialize(Vector3 velocity, Vector3 angularVelocity)
        {
            m_Velocity = velocity;
            m_AngularVelocity = angularVelocity;
        }

        private void FixedUpdate()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = m_Velocity;
                rb.angularVelocity = m_AngularVelocity;
            }

            // Einmal-Anwendung: Component entfernen
            Destroy(this);
        }
    }
}
