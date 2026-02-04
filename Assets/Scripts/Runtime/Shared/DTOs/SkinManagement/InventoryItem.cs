

using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class InventoryItem
    {
        [JsonProperty("Name")]
        public string Name;
        
        [JsonProperty("Bolt")]
        public string Bolt;
    }
}