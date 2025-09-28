using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // goutfitting_t
    // Player outfitting template (name and list of item indices per group)
    public class GOutfitting
    {
        // char name[MAX_OUTFITTING_NAME];
        public string name = string.Empty;

        // int items[OUTFITTING_GROUP_MAX];
        // OUTFITTING_GROUP_MAX is engine-specific; allocate dynamically
        public int[] items;

        public GOutfitting()
        {
            items = new int[0];
        }
    }
}