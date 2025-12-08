
using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class SkinSurfacePartDefinition
    {
        public string naming;
        public string description;
        public bool required;
        public List<string> material; // Surface names
    }
}