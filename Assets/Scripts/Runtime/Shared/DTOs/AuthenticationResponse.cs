using System;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [Serializable]
    public class AuthenticationResponse
    {
        public bool success;
        public string message;
        public string playerId;
    }
}
