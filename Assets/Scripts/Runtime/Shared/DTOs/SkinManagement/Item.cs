

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    [Serializable]
    public class Item
    {
        [JsonProperty("name")]
        public string name;

        [JsonProperty("onsurf")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public List<string> activeSurfaces;

        [JsonProperty("offsurf")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public List<string> inactiveSurfaces;

        [JsonProperty("model")]
        public string modelName;
    }
}