using System.Collections;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    ///<summary>
    /// A collection of Coroutines Helpers
    ///</summary>
    internal static class CoroutinesHelper
    {
        /*
            usage: yield return CoroutinesHelper.OneSecond; 
            This is better than : yield return new waitforseconds(1);
            because: it doesn't generate garbage.
        */
        public static readonly WaitForSeconds PointZeroOneSeconds = new(0.01f);
        public static readonly WaitForSeconds PointZeroFiveSeconds = new(0.05f);
        public static readonly WaitForSeconds PointOneSeconds = new(0.1f);
        public static readonly WaitForSeconds PointTwoSeconds = new(0.2f);
        public static readonly WaitForSeconds PointThreeSeconds = new(0.3f);
        public static readonly WaitForSeconds PointFiveSeconds = new(0.5f);
        public static readonly WaitForSeconds PointSevenSeconds = new(0.7f);
        public static readonly WaitForSeconds PointSevenFiveSeconds = new(0.75f);
        public static readonly WaitForSeconds OneSecond = new(1);
        public static readonly WaitForSeconds OnePointFiveSeconds = new(1.5f);
        public static readonly WaitForSeconds TwoSeconds = new(2);
        public static readonly WaitForSeconds ThreeSeconds = new(3);
        public static readonly WaitForSeconds FourSeconds = new(4);
        public static readonly WaitForSeconds FiveSeconds = new(5);
        public static readonly WaitForSeconds EightSeconds = new(8);
        public static readonly WaitForSeconds TenSeconds = new(10);
        public static readonly WaitForSeconds TwelveSeconds = new(12);
        public static readonly WaitForSeconds FifteenSeconds = new(15);
        public static readonly WaitForSeconds TwentySeconds = new(20);
        public static readonly WaitForSeconds TwentyFiveSeconds = new(25);
        static readonly WaitForEndOfFrame EndOfFrame = new();

        /// <summary>
        /// EndOfFrame does not work in the batchmode editor, so we need 
        /// a workaround: https://forum.unity.com/threads/do-not-use-waitforendofframe.883648/
        /// </summary>
        /// <returns></returns>
        public static IEnumerator WaitAFrame()
        {
#if UNITY_EDITOR
            yield return Application.isBatchMode ? null : EndOfFrame;
#else
            yield return EndOfFrame;
#endif
        }

        public static void StopAndNullifyRoutine(ref Coroutine routine, MonoBehaviour behaviourWhichStartedIt)
        {
            if (routine == null) { return; }
            if (!behaviourWhichStartedIt) { return; }
            behaviourWhichStartedIt.StopCoroutine(routine);
            routine = null;
        }

        public static IEnumerator WaitAndDo(IEnumerator delay, System.Action action)
        {
            yield return delay;
            action?.Invoke();
        }

        public static IEnumerator WaitAndDo(YieldInstruction delay, System.Action action)
        {
            yield return delay;
            action?.Invoke();
        }
    }
}