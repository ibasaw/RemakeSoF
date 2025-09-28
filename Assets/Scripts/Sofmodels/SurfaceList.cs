using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // TSurfaceList / SSurfaceList
    // Singly-linked list of surface names used on templates
    public class SurfaceList
    {
        // const char *mName;
        public string mName;

        // struct SSurfaceList *mNext;
        public SurfaceList mNext;
    }
}