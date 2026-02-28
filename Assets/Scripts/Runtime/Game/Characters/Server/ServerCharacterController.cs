using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitige Orchestrierung für Character-Kampflogik.
    /// Delegiert Health-/State-Management an <see cref="NetworkedCharacterState"/> (Single Source of Truth).
    /// Keine eigene State-Haltung — nur Logik und Timing.
    /// </summary>
    public class ServerCharacterController : MonoBehaviour
    {
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        [SerializeField]
        private int m_MaxHealth = 100;

        [SerializeField]
        private float m_RespawnTime = 5f;

        /// <summary>
        /// Aktuelle Health aus dem NetworkedCharacterState.
        /// </summary>
        public int CurrentHealth => m_CharacterState.Health;

        /// <summary>
        /// Ist der Character am Leben? (aus NetworkedCharacterState).
        /// </summary>
        public bool IsAlive => m_CharacterState.IsAlive;

        /// <summary>
        /// Server: Schaden anwenden.
        /// Nutzt <see cref="NetworkedCharacterState.SetHealth"/> als Single Source of Truth.
        /// </summary>
        /// <param name="damageAmount">Schaden-Menge.</param>
        /// <param name="attackerId">Die Client-ID des Angreifers (optional).</param>
        public void ApplyDamage(int damageAmount, ulong attackerId = ulong.MaxValue)
        {
            if (!m_CharacterState.IsAlive)
            {
                return;
            }

            int newHealth = Mathf.Max(0, m_CharacterState.Health - damageAmount);
            m_CharacterState.SetHealth(newHealth);
            Debug.Log($"[ServerCharacterController] Character took {damageAmount} damage. Health: {newHealth}");

            if (newHealth <= 0)
            {
                KillCharacter(attackerId);
            }
        }

        /// <summary>
        /// Server: Character töten.
        /// Setzt IsAlive auf false, addiert Death und plant Respawn.
        /// Client-Benachrichtigung erfolgt automatisch via NetworkVariable-Change in NetworkedCharacterState.
        /// </summary>
        /// <param name="attackerId">Die Client-ID des Killers (optional).</param>
        private void KillCharacter(ulong attackerId)
        {
            m_CharacterState.SetIsAlive(false);
            m_CharacterState.AddDeath();
            Debug.Log($"[ServerCharacterController] Character died. Killed by {attackerId}");

            // Schedule respawn
            Invoke(nameof(RespawnCharacter), m_RespawnTime);
        }

        /// <summary>
        /// Server: Character respawnen.
        /// Setzt Health auf Max und IsAlive auf true.
        /// Client-Benachrichtigung erfolgt automatisch via NetworkVariable-Change.
        /// </summary>
        private void RespawnCharacter()
        {
            m_CharacterState.SetHealth(m_MaxHealth);
            m_CharacterState.SetIsAlive(true);
            Debug.Log($"[ServerCharacterController] Character respawned. Health: {m_MaxHealth}");

            // TODO: Spawn-Position zurücksetzen via NetworkedPlayerCharacter
        }

        /// <summary>
        /// Server: Heilen.
        /// </summary>
        /// <param name="healAmount">Heilungs-Menge.</param>
        public void Heal(int healAmount)
        {
            if (!m_CharacterState.IsAlive)
            {
                return;
            }

            int newHealth = Mathf.Min(m_MaxHealth, m_CharacterState.Health + healAmount);
            m_CharacterState.SetHealth(newHealth);
            Debug.Log($"[ServerCharacterController] Character healed by {healAmount}. Health: {newHealth}");
        }
    }
}
