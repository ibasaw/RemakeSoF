
using System;
using System.Collections.Generic;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class SkinDefinition
    {
        public ModelPreferences prefs;
        public List<MaterialDefinition> materials;
    }
}