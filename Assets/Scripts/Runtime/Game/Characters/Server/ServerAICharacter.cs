using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitige AI-Logik fuer Bot-Characters.
    /// Wird nur auf dem Server ausgefuehrt.
    /// Steuert Bewegung und Entscheidungen des AI-Bots.
    /// Spaeter: GOAP/EANN Integration fuer intelligente Entscheidungen.
    /// </summary>
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ServerAICharacter : MonoBehaviour
    {
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
        /// Markiert den AI-Character als bereit.
        /// Wird von NetworkedAICharacter nach Spawn-Position-Zuweisung aufgerufen.
        /// </summary>
        public void SetReady()
        {
            m_IsReady = true;
            Debug.Log("[ServerAICharacter] AI-Bot ist bereit.");
        }

        private void Update()
        {
            if (!m_IsReady)
            {
                return;
            }

            // TODO: AI-Logik hier implementieren
            // Phase 1: Idle (steht einfach rum)
            // Phase 2: Random-Walk / Waypoint-Patrol
            // Phase 3: GOAP + EANN Integration
        }
    }
}
