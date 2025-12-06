
namespace Unity.DedicatedGameServerSample.Runtime
{
    public class PlayerDataModel : Model<MetagameApplication>
    {
        public AuthenticationResponse AuthResponse { get; private set; }

        public string PlayerName => AuthResponse.username;
        public string PlayerId => AuthResponse.playerId;

        public void InitializePlayer(AuthenticationResponse authResponse)
        {
            AuthResponse = authResponse;
        }
    }
}