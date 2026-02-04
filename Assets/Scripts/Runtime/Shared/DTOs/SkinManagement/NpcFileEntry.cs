

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Helper class for parsing the NPC file structure
    /// </summary>
    [Serializable]
    public class NpcFileEntry
    {
        [JsonProperty("GroupInfo")]
        public GroupInfo GroupInfo { get; set; }

        [JsonProperty("CharacterTemplate")]
        [JsonConverter(typeof(SingleOrArrayConverter<CharacterTemplate>))]
        public List<CharacterTemplate> CharacterTemplate { get; set; }
    }
}
