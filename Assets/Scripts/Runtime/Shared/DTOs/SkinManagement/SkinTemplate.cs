

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class SkinTemplate
    {
        [JsonProperty("File")]
        public string SkinName;
        
        [JsonProperty("Inventory")]
        public TemplateInventory Inventory;
    }
}