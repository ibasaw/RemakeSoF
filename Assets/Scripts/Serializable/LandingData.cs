using System;
using Newtonsoft.Json;

namespace SoF2Remake.Data
{
    [Serializable]
    public class LandingData
    {
        [JsonProperty("sound")]
        public string sound { get; set; }
    }
}