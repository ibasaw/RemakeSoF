using System;
using Newtonsoft.Json;

namespace SoF2Remake.Data
{
    [Serializable]
    public class MaterialInfo
    {
        [JsonProperty("loudness")]
        public double? loudness { get; set; }

        [JsonProperty("density")]
        public double? density { get; set; }

        [JsonProperty("projectileBounce")]
        public double? projectileBounce { get; set; }

        [JsonProperty("friction")]
        public double? friction { get; set; }

        [JsonProperty("damage")]
        public double? damage { get; set; }
    }
}
