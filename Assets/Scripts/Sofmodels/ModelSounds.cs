using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TModelSounds / SModelSounds
    // Holds a named group of model sounds
    public class ModelSounds
    {
        // const char *mName;
        public string mName;

        // const char *mSounds[MAX_MODEL_SOUNDS];
        public string[] mSounds = new string[8];

        // int mCount;
        public int mCount;

        // struct SModelSounds *mNext;
        public ModelSounds mNext;
    }
}