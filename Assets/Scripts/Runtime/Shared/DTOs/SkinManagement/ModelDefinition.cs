
using System;
using System.Collections.Generic;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class MaterialDefinition
    {
        public string name;
        public List<MaterialGroup> groups;
    }
}