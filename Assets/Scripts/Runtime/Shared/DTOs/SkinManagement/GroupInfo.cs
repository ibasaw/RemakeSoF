

using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Helper class for GroupInfo section
    /// </summary>
    [Serializable]
    public class GroupInfo
    {
        [JsonProperty("Skeleton")]
        public string Skeleton { get; set; }

        [JsonProperty("ParentTemplate")]
        public string ParentTemplate { get; set; }
    }
}
