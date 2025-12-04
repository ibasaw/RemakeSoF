using System;

namespace Unity.DedicatedGameServerSample.Runtime
{
    [Serializable]
    public class AuthenticationPayload
    {
        public string username;
        public string password;
        public string email;
    }
}
