using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TCharacterTemplate / SCharacterTemplate
    // Template describing a character (model, sounds, inventory, skins)
    public class CharacterTemplate
    {
        // const char *mName;
        public string mName;

        // const char *mModel;
        public string mModel;

        // const char *mParentName;
        public string mParentName;

        // const char *mFormalName;
        public string mFormalName;

        // int mSoundCount;
        public int mSoundCount;

        // qboolean mDeathmatch;
        public bool mDeathmatch;

        // TInventoryTemplate *mInventory;
        public InventoryTemplate mInventory;

        // TSkinTemplate *mSkins;
        public SkinTemplate mSkins;

        // TModelSounds *mSounds;
        public ModelSounds mSounds;

        // struct SCharacterTemplate *mParent;
        public CharacterTemplate mParent;

        // struct SCharacterTemplate *mNext;
        public CharacterTemplate mNext;
    }
}