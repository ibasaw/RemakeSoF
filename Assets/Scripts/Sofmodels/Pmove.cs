using System;
using UnityEngine;

namespace Assets.Scripts.Sofmodels
{
    [Serializable]
    // pmove_t (converted)
    // Simplified port of pmove_t from bg_public.h. Many engine-specific types are left as object placeholders.
    public class Pmove
    {
        // playerState_t *ps; // state (in / out)
        public object ps;

        // usercmd_t cmd; // command (in)
        public object cmd;

        // int tracemask; // collide against these types of surfaces
        public int tracemask;

        // int debugLevel; // if set, diagnostic output will be printed
        public int debugLevel;

        // qboolean noFootsteps; // if the game is setup for no footsteps by the server
        public bool noFootsteps;

        // qboolean gauntletHit; // true if a gauntlet attack would actually hit something
        public bool gauntletHit;

        // int framecount;
        public int framecount;

        // int numtouch;
        public int numtouch;

        // int touchents[MAXTOUCH];
        public int[] touchents = new int[32];

        // int useEvent;
        public int useEvent;

        // vec3_t mins, maxs; // bounding box size
        public Vector3 mins;
        public Vector3 maxs;

        // int watertype;
        public int watertype;

        // int waterlevel;
        public int waterlevel;

        // animation_t *animations;
        public Animation[] animations;

        // float xyspeed;
        public float xyspeed;

        // int pmove_fixed;
        public int pmove_fixed;

        // int pmove_msec;
        public int pmove_msec;

        // callbacks - simplified to delegates
        public Action<object, object, object, object, object, int, int> trace;
        public Func<Vector3, int, int> pointcontents;

        // int weaponAnimIdx;
        public int weaponAnimIdx;

        // char weaponAnim[MAX_QPATH];
        public string weaponAnim;

        // char weaponEndAnim[MAX_QPATH];
        public string weaponEndAnim;

        public Pmove() { weaponAnim = string.Empty; weaponEndAnim = string.Empty; }
    }
}