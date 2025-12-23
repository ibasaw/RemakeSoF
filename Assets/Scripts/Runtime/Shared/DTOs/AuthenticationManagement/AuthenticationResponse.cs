using System;

namespace Tolik.RemakeSoF.Runtime
{
    [Serializable]
    public class AuthenticationResponse
    {
        public string message;
        public string playerId;
        public string username;
        public string token;

        public string selectedSkinName;
    }
}
