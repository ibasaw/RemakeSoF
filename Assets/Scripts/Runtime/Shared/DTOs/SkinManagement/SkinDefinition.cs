
using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class SkinDefinition
    {
        public ModelPreferences prefs;
        public List<MaterialDefinition> materials;

        public string GetModelName()
        {
            if (prefs != null && prefs.models != null && prefs.models.TryGetValue("1", out string modelName))
            {
                return modelName;
            }
            return null;
        }
    }
}