
using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class MaterialDefinition
    {
        public string name;
        public List<MaterialGroup> groups;
    }
}