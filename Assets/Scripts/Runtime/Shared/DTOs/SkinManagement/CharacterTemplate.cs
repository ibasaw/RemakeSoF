

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class CharacterTemplate
    {
        [JsonProperty("Name")]
        public string Name;
        
        [JsonProperty("Model")]
        public string Model;
        
        // ParentTemplate kommt aus GroupInfo (wird manuell gesetzt)
        public string ParentTemplate;
        
        [JsonProperty("Skin")]
        [JsonConverter(typeof(SingleOrArrayConverter<SkinTemplate>))]
        public List<SkinTemplate> SkinTemplates;
        
        [JsonProperty("Inventory")]
        public TemplateInventory Inventory;

        /// <summary>
        /// Finds a SkinTemplate by its skin name (case-insensitive).
        /// </summary>
        /// <param name="skinName">The name of the skin to find.</param>
        /// <returns>The matching SkinTemplate or null if not found.</returns>
        public SkinTemplate FindBySkinName(string skinName)
        {
            if (string.IsNullOrEmpty(skinName) || SkinTemplates == null)
                return null;

            return SkinTemplates.FirstOrDefault(skin => 
                string.Equals(skin.SkinName, skinName, StringComparison.OrdinalIgnoreCase));
        }

    }
}