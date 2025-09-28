using System;
using UnityEngine;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TIdentity / SIdentity
    // Represents a named identity (team, character, skin, icon)
    public class Identity
    {
        // const char *mName;
        public string mName;

        // const char *mTeam;
        public string mTeam;

        // TCharacterTemplate *mCharacter;
        public CharacterTemplate mCharacter;

        // TSkinTemplate *mSkin;
        public SkinTemplate mSkin;

        // qhandle_t mIcon;
        public int mIcon; // qhandle_t -> int
    }
}