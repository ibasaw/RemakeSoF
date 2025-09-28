using System;
using UnityEngine;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // ladder_t (from ladder_s)
    // Simple ladder descriptor with origin and forward vector
    public class Ladder
    {
        // vec3_t origin;
        public Vector3 origin;

        // vec3_t fwd;
        public Vector3 fwd;
    }
}