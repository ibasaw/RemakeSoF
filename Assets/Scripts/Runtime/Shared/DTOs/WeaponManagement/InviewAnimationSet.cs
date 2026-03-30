using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Sammlung aller FP-Composite-Animationen fuer eine Waffe.
    /// Mappt SoF2 SOF2.inview Animation-States auf Unity Animation-Clips.
    /// MP-relevante States: idle, fire, reload, ready, done, altfire, altreload, dryfire.
    /// Die mp_speed-Werte aus SoF2 sind bereits in die Track-Speeds eingerechnet.
    /// </summary>
    [Serializable]
    public class InviewAnimationSet
    {
        /// <summary>
        /// Idle-Animation (Standardpose bei gehaltener Waffe).
        /// </summary>
        [JsonProperty("idle")]
        public InviewAnimationState Idle;

        /// <summary>
        /// Feuer-Animation (primaerer Angriff).
        /// </summary>
        [JsonProperty("fire")]
        public InviewAnimationState Fire;

        /// <summary>
        /// Nachladen-Animation.
        /// </summary>
        [JsonProperty("reload")]
        public InviewAnimationState Reload;

        /// <summary>
        /// Waffe-Ziehen-Animation (Weapon-Switch rein).
        /// </summary>
        [JsonProperty("ready")]
        public InviewAnimationState Ready;

        /// <summary>
        /// Waffe-Wegstecken-Animation (Weapon-Switch raus).
        /// </summary>
        [JsonProperty("done")]
        public InviewAnimationState Done;

        /// <summary>
        /// Alternativfeuer-Animation (optional, z.B. M203, Bayonet, Pistol-Whip).
        /// </summary>
        [JsonProperty("altfire")]
        public InviewAnimationState Altfire;

        /// <summary>
        /// Alternativ-Nachladen-Animation (optional, z.B. M4 M203 reload).
        /// </summary>
        [JsonProperty("altreload")]
        public InviewAnimationState Altreload;

        /// <summary>
        /// Leer-Feuer-Animation (Klick bei leerer Waffe).
        /// </summary>
        [JsonProperty("dryfire")]
        public InviewAnimationState Dryfire;
    }
}
