using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.AI.Personality
{
    /// <summary>
    /// JSON-Wurzelobjekt fuer SoF2_BotPersonalities.json.
    /// </summary>
    [Serializable]
    public class BotPersonalityCollection
    {
        /// <summary>Liste aller verfuegbaren Profile.</summary>
        public List<BotPersonality> profiles = new();
    }
}
