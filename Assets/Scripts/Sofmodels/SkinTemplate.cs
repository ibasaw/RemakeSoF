using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TSkinTemplate / SSkinTemplate
    // A skin and its associated inventory templates
    public class SkinTemplate
    {
        // const char *mSkin;
        public string mSkin;

        // TInventoryTemplate *mInventory;
        public InventoryTemplate mInventory;

        // struct SSkinTemplate *mNext;
        public SkinTemplate mNext;
    }
}