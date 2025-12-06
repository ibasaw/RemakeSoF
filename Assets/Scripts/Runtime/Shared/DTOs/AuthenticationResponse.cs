using System;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [Serializable]
    public class AuthenticationResponse
    {
        public string message;
        public string playerId;
        public string username;
        public string token;
    }
}
