
using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    // NPC_definition.json Struktur
    public class SkinSurfaceDefinition
    {
        public string description;
        public Dictionary<string, SkinSurfacePartDefinition> parts;
    }
}