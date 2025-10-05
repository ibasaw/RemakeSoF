using System;
using Newtonsoft.Json;

namespace SoF2Remake.Data
{
    [Serializable]
    public class FootstepData
    {
        /// <summary>
        /// Path to the footstep sound to play.
        /// </summary>
        [JsonProperty("sound")]
        public string sound { get; set; }

        /// <summary>
        /// Path to the decal texture to use for the footstep.
        /// </summary>
        [JsonProperty("decal")]
        public string decal { get; set; }

        /// <summary>
        /// Duration of the footstep sound in milliseconds.
        /// </summary>
        [JsonProperty("duration")]
        public int? duration { get; set; }

        /// <summary>
        /// Fade time in milliseconds.
        /// </summary>
        [JsonProperty("fadetime")]
        public int? fadetime { get; set; }

        /// <summary>
        /// Latency in milliseconds.
        /// </summary>
        [JsonProperty("latency")]
        public int? latency { get; set; }

        public override string ToString()
        {
            return $"FootstepData(sound={sound}, decal={decal}, duration={duration}, fadetime={fadetime}, latency={latency})";
        }
    }
}
