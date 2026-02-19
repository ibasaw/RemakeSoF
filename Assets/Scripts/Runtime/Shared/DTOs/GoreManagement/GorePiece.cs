

using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    [Serializable]
    public class GorePiece
    {
        [JsonProperty("Name")]
        public string name;
        [JsonProperty("Model")]
        public string modelName;
        [JsonProperty("Bolt")]
        public string boltName;
    }
}