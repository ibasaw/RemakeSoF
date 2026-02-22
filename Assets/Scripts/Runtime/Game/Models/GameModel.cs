using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Main model of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameModel : Model<GameApplication>
    {
        [SerializeField]
        NetworkedGameState m_NetworkedGameState;

        public NetworkedGameState NetworkedGameState => m_NetworkedGameState;

        public NetworkVariable<uint> Countdown => m_NetworkedGameState.matchCountdown;

        public NetworkVariable<int> PlayersConnected => m_NetworkedGameState.playersConnected;

        /// <summary>
        /// Öffentlicher Zugriff auf den aktuellen Map-Namen.
        /// </summary>
        public string CurrentMapName => m_NetworkedGameState.currentMapName.Value.ToString();

        public bool MenuVisible { get; set; } = false;
        
        //public ClientPlayerCharacter PlayerCharacter { get; set; }
    }
}
