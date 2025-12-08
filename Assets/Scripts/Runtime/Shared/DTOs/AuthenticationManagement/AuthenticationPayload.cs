using System;

namespace Tolik.RemakeSoF.Runtime
{
    [Serializable]
    public class AuthenticationPayload
    {
        public string username;
        public string password;
        public string email;
    }
}
