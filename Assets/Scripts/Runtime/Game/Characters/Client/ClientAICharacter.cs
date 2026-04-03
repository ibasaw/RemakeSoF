using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Client-seitige Komponente fuer AI-Bot-Characters.
    /// Wird auf allen Clients instanziiert (auch Host).
    /// Verantwortlich fuer visuelle Darstellung, Interpolation und
    /// spaeter: Animationssteuerung basierend auf NetworkVariables.
    /// Server ignoriert diese Komponente.
    /// </summary>
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ClientAICharacter : MonoBehaviour
    {
        /// <summary>
        /// Referenz auf die vernetzte AI-Character-Komponente.
        /// </summary>
        [SerializeField]
        private NetworkedAICharacter m_NetworkedAICharacter;

        /// <summary>
        /// Referenz auf den NetworkedCharacterState fuer Events (Health, Skin, etc.).
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        private void OnEnable()
        {
            if (m_CharacterState != null)
            {
                m_CharacterState.OnCharacterDied += OnBotDied;
                m_CharacterState.OnCharacterRespawned += OnBotRespawned;
            }
        }

        private void OnDisable()
        {
            if (m_CharacterState != null)
            {
                m_CharacterState.OnCharacterDied -= OnBotDied;
                m_CharacterState.OnCharacterRespawned -= OnBotRespawned;
            }
        }

        /// <summary>
        /// Callback: Bot ist gestorben. Spaeter: Ragdoll/Death-Animation ausloesen.
        /// </summary>
        private void OnBotDied()
        {
            Debug.Log($"[ClientAICharacter] Bot gestorben: {m_CharacterState.CharacterName}");
        }

        /// <summary>
        /// Callback: Bot ist respawned. Spaeter: Visual zuruecksetzen.
        /// </summary>
        private void OnBotRespawned()
        {
            Debug.Log($"[ClientAICharacter] Bot respawned: {m_CharacterState.CharacterName}");
        }
    }
}
