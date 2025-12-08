using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Data
{
    [Serializable]
    public class LandingData
    {
        [JsonProperty("sound")]
        public string sound { get; set; }
    }
}