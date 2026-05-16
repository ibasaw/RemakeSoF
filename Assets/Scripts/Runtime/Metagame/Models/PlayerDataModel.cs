
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    public class PlayerDataModel : Model<MetagameApplication>
    {
        private AuthenticationResponse m_AuthResponse;

        public bool IsInitialized => m_AuthResponse != null;
        public string PlayerName => m_AuthResponse?.username;
        public string PlayerId => m_AuthResponse?.playerId;
        public string CurrentSelectedSkinName => m_AuthResponse?.selectedSkinName;
        public void InitializePlayer(AuthenticationResponse authResponse)
        {
            m_AuthResponse = authResponse;
        }

        /// <summary>
        /// Drops the cached authenticated identity. Called by MainMenuController on logout
        /// so a subsequent login as a different user does not briefly render the previous skin / name.
        /// </summary>
        public void Reset()
        {
            m_AuthResponse = null;
        }
    }
}