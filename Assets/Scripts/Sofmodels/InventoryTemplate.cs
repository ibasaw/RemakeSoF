using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TInventoryTemplate / SInventoryTemplate
    // Represents an inventory slot/template for characters
    public class InventoryTemplate
    {
        // const char *mName;
        public string mName;

        // const char *mBolt;
        public string mBolt;

        // TItemTemplate *mItem;
        public ItemTemplate mItem;

        // struct SInventoryTemplate *mNext;
        public InventoryTemplate mNext;

        // qboolean mOnBack;
        public bool mOnBack;

        // int mBoltIndex;
        public int mBoltIndex;

        // int mModelIndex;
        public int mModelIndex;
    }
}