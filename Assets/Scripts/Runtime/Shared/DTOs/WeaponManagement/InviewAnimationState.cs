using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Animations-State fuer SoF2-Style First-Person Composite-Modell.
    /// Pro State (idle, fire, reload, ready, done) koennen Waffe, lhand und rhand
    /// jeweils einen eigenen Animation-Clip abspielen.
    /// Mappt auf SoF2 SOF2.inview "anim"-Eintraege (nur MP-relevante States).
    /// </summary>
    [Serializable]
    public class InviewAnimationState
    {
        /// <summary>
        /// Waffen-Animation-Track (Slot 0: viewG2Model).
        /// Null wenn kein waffenspezifischer Clip fuer diesen State existiert.
        /// </summary>
        [JsonProperty("weapon")]
        public InviewAnimationTrack Weapon;

        /// <summary>
        /// Linke-Hand-Animation-Track (Slot 3: lhand.glm).
        /// Null wenn linke Hand in diesem State nicht animiert wird.
        /// </summary>
        [JsonProperty("leftHand")]
        public InviewAnimationTrack LeftHand;

        /// <summary>
        /// Rechte-Hand-Animation-Track (Slot 2: rhand.glm).
        /// Null wenn rechte Hand in diesem State nicht animiert wird.
        /// </summary>
        [JsonProperty("rightHand")]
        public InviewAnimationTrack RightHand;
    }
}
