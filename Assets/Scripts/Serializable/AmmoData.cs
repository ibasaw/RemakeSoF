using System;
using Newtonsoft.Json;

namespace SoF2Remake.Data
{
    [Serializable]
    public class AmmoData
    {
        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("shellsound")]
        public string shellsound { get; set; }

        [JsonProperty("effect")]
        public string effect { get; set; }

        [JsonProperty("debris")]
        public string debris { get; set; }
    }
}