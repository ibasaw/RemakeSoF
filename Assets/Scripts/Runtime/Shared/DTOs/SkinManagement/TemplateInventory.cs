

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class TemplateInventory
    {
        [JsonProperty("Item")]
        [JsonConverter(typeof(SingleOrArrayConverter<InventoryItem>))]
        public List<InventoryItem> Items;
    }
}