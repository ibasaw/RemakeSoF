
using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class ModelPreferences
    {
        public Dictionary<string, string> models;
        public Dictionary<string, string> surfaces_on;
        public Dictionary<string, string> surfaces_off;
    }
}