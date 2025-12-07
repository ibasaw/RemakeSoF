
using System;
using System.Collections.Generic;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class ModelPreferences
    {
        public Dictionary<string, string> models;
        public Dictionary<string, string> surfaces_on;
        public Dictionary<string, string> surfaces_off;
    }
}