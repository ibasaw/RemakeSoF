
using System;
using System.Collections.Generic;

namespace Unity.DedicatedGameServerSample.Runtime.PlayerSkinManagement
{
    // Definition.json Struktur
    public class SkinSurfaceDefinition
    {
        public string description;
        public Dictionary<string, SkinSurfacePartDefinition> parts;
    }
}