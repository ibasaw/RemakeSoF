
using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class SkinDefinition
    {
        public ModelPreferences prefs;
        public List<MaterialDefinition> materials;
    }
}