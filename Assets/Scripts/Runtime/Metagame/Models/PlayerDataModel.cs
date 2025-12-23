
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    public class PlayerDataModel : Model<MetagameApplication>
    {
        private AuthenticationResponse m_AuthResponse;

        public string PlayerName => m_AuthResponse.username;
        public string PlayerId => m_AuthResponse.playerId;
        public string CurrentSelectedSkinName => m_AuthResponse.selectedSkinName;
        public void InitializePlayer(AuthenticationResponse authResponse)
        {
            m_AuthResponse = authResponse;
        }
    }
}