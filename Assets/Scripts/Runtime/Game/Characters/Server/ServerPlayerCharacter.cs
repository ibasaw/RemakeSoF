using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitiger Player-Character Controller.
    /// Fängt AnimationEvents ab die auf dem Server ankommen (wegen NetworkAnimator),
    /// und kann zukünftig erweitert werden für server-seitige Logik.
    /// </summary>
    [RequireComponent(typeof(NetworkedPlayerCharacter))]
    public class ServerPlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        private NetworkedPlayerCharacter m_NetworkedPlayerCharacter;

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
