using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;

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

        [JsonProperty("footstepData")]
        public Dictionary<string, FootstepData> footstepData { get; set; } = new();

        [JsonProperty("landingData")]
        public Dictionary<string, LandingData> landingData { get; set; } = new();


        [JsonProperty("ammoData")]
        public Dictionary<string, AmmoData> ammoData { get; set; } = new();
    }
}
