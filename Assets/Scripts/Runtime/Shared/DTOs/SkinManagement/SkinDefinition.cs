
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

        /// <summary>
        /// Returns the texture1 (or shadow1 if present in future) for the given material definition name (e.g. "body").
        /// </summary>
        public string GetTextureOrShadowForMaterialDefinitionName(string materialName)
        {
            if (materials == null)
                return null;
            foreach (var mat in materials)
            {
                if (mat != null && string.Equals(mat.name, materialName, StringComparison.OrdinalIgnoreCase) && mat.groups != null)
                {
                    foreach (var group in mat.groups)
                    {
                        if (group != null)
                        {
                            if (!string.IsNullOrEmpty(group.texture1))
                                return group.texture1;
                            if (!string.IsNullOrEmpty(group.shader1))
                                return group.shader1;
                        }
                    }
                }
            }
            return null;
        }
    }
}