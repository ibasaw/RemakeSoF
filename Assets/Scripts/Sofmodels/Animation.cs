using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // animation_t (from animation_s)
    // Represents animation metadata (frames, lerp timings and flags)
    public class Animation
    {
        // int firstFrame;
        public int firstFrame;

        // int numFrames;
        public int numFrames;

        // int loopFrames; // 0 to numFrames
        public int loopFrames;

        // int frameLerp; // msec between frames
        public int frameLerp;

        // int initialLerp; // msec to get to first frame
        public int initialLerp;

        // int reversed; // true if animation is reversed
        public bool reversed;

        // int flipflop; // true if animation should flipflop back to base
        public bool flipflop;

        public Animation() { }
    }
}