using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TItemTemplate / SItemTemplate
    // Template for an item that can be attached to a character model
    public class ItemTemplate
    {
        // const char *mName;
        public string mName;

        // const char *mModel;
        public string mModel;

        // TSurfaceList *mOnList;
        public SurfaceList mOnList;

        // TSurfaceList *mOffList;
        public SurfaceList mOffList;

        // struct SItemTemplate *mNext;
        public ItemTemplate mNext;
    }
}