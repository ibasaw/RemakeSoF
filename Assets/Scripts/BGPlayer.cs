using UnityEngine;

// BGPlayer: a C# port of BG_PlayerAngles from SoF2 (simplified, adapted for Unity)
// Provides angle calculation for legs / lower torso / upper torso / head and an Apply helper
public static class BGPlayer
{
    public struct AnimInfo
    {
        public float yawAngle;
        public bool yawing;
        public float pitchAngle;
        public bool pitching;
    }

    public struct AnglesResult
    {
        public Vector3 legsAngles; // (pitch, yaw, roll)
        public Vector3 lowerTorsoAngles;
        public Vector3 upperTorsoAngles;
        public Vector3 headAngles;
    }

    // Helper: normalize angle to 0..360
    private static float AngleMod(float a)
    {
        a = a % 360f;
        if (a < 0) a += 360f;
        return a;
    }

    // Shortest signed delta (-180..180)
    private static float AngleDelta(float a, float b)
    {
        float d = a - b;
        while (d > 180f) d -= 360f;
        while (d < -180f) d += 360f;
        return d;
    }

    // SwingAngles approximate port: smooths current towards target
    private static void BG_SwingAngles(float target, float clampedMovement, float clampTolerance, float speed, ref float current, ref bool swinging, int frameTime)
    {
        // target,current in degrees
        float delta = AngleDelta(target, current);
        // If movement large enough, allow swinging
        if (Mathf.Abs(delta) > clampedMovement)
        {
            swinging = true;
        }

        if (swinging)
        {
            // step proportionate to delta, frameTime in ms
            float step = speed * (frameTime / 1000f) * delta;
            current = AngleMod(current + step);
            // stop swinging when close
            if (Mathf.Abs(AngleDelta(target, current)) < 0.5f)
            {
                swinging = false;
                current = AngleMod(target);
            }
        }
        else
        {
            current = AngleMod(target);
        }
    }

    // Add a small pain twitch (simple approximation)
    private static void BG_AddPainTwitch(int painTime, int painDir, int currentTime, ref Vector3 lowerTorsoAngles)
    {
        if (painTime <= 0) return;
        int delta = currentTime - painTime;
        if (delta < 0) return;
        // simple transient twitch based on direction
        float strength = Mathf.Max(0f, 5f - delta * 0.01f);
        lowerTorsoAngles.z += painDir * strength; // roll twitch
    }

    // Port of BG_PlayerAngles simplified. startAngles: (pitch, yaw, roll) in degrees.
    public static AnglesResult PlayerAngles(
        Vector3 startAngles,
        AnimInfo torsoInfo,
        AnimInfo legsInfo,
        int leanOffset,
        int painTime,
        int painDirection,
        int currentTime,
        float movementDir,
        Vector3 realvelocity,
        bool dead,
        int frameTime
    )
    {
        AnglesResult res = new AnglesResult();

        // movement offsets from SoF2
        int[] movementOffsets = new int[8] { 0, 22, 45, -22, 0, 22, -45, -22 };

        Vector3 headAngles = startAngles; // pitch,yaw,roll
        Vector3 legsAngles = Vector3.zero;
        Vector3 lowerTorsoAngles = Vector3.zero;
        Vector3 upperTorsoAngles = Vector3.zero;

        // ensure yaw is normalized
        headAngles.y = AngleMod(headAngles.y);

        Vector3 velocity = realvelocity;
        float speed = velocity.magnitude;

        // allow yaw to drift a bit (we won't toggle anim bits here but we accept torsoInfo/legsInfo)
        // determine dir
        int dir = 0;
        if (dead)
        {
            dir = 0;
        }
        else
        {
            dir = Mathf.Clamp((int)movementDir, 0, 7);
            if (leanOffset != 0 && speed == 0f)
            {
                dir = 0;
            }
        }

        // legs yaw = headYaw + 2*movementOffsets[dir]
        legsAngles.y = headAngles.y + 2f * movementOffsets[dir];
        lowerTorsoAngles.y = headAngles.y + 2f * movementOffsets[dir];

        // Swing smoothing similar to original
        float tmpUpper = torsoInfo.yawAngle;
        float tmpLegs = legsInfo.yawAngle;
        bool upperYawing = torsoInfo.yawing;
        bool legsYawing = legsInfo.yawing;

        BG_SwingAngles(lowerTorsoAngles.y, 25f, 90f, 0.3f, ref tmpUpper, ref upperYawing, frameTime);
        BG_SwingAngles(legsAngles.y, 40f, 90f, 0.3f, ref tmpLegs, ref legsYawing, frameTime);

        if (leanOffset != 0)
        {
            legsAngles.y = headAngles.y;
        }
        else
        {
            legsAngles.y = tmpLegs;
        }

        float lowerDelta = AngleDelta(headAngles.y, legsAngles.y);
        lowerDelta = Mathf.Clamp(lowerDelta, -90f, 90f);
        lowerTorsoAngles.y = legsAngles.y + lowerDelta / 2f;
        upperTorsoAngles.y = lowerDelta / 2f;
        headAngles.y = AngleMod(headAngles.y - upperTorsoAngles.y);

        // pitch: torso gets 75% of head pitch
        float destPitch = headAngles.x > 180f ? (-360f + headAngles.x) * 0.75f : headAngles.x * 0.75f;
        lowerTorsoAngles.x = destPitch;

        // roll: lean or velocity side
        if (leanOffset != 0)
        {
            lowerTorsoAngles.z -= (float)leanOffset * 1.25f;
            lowerTorsoAngles.y -= 1.25f * ((float)leanOffset / 30f) * destPitch; // LEAN_OFFSET ~30
            headAngles.y -= ((float)leanOffset / 30f) * destPitch;
            headAngles.z -= (float)leanOffset * 1.25f;
        }
        else if (speed > 0f)
        {
            // side roll based on lateral velocity
            // Quaternion does not have GetColumn - get the local right axis by rotating Vector3.right
            Vector3 axis0 = Quaternion.Euler(legsAngles) * Vector3.right; // approximate axis[1] (right)
            float side = speed * Vector3.Dot(velocity, axis0) * 0.025f;
            legsAngles.z -= side;
        }

        // pain twitch
        BG_AddPainTwitch(painTime, painDirection, currentTime, ref lowerTorsoAngles);

        // hierarchical subtract (head relative to lowerTorso, lowerTorso relative to legs)
        // AnglesSubtract(a,b,out a) means a = a - b (component-wise with angle wrap)
        headAngles = SubtractAngles(headAngles, lowerTorsoAngles);
        lowerTorsoAngles = SubtractAngles(lowerTorsoAngles, legsAngles);

        // fill result
        res.legsAngles = legsAngles;
        res.lowerTorsoAngles = lowerTorsoAngles;
        res.upperTorsoAngles = upperTorsoAngles;
        res.headAngles = headAngles;

        return res;
    }

    // subtract angles with wrapping: result = a - b
    private static Vector3 SubtractAngles(Vector3 a, Vector3 b)
    {
        return new Vector3(
            AngleDelta(a.x, b.x),
            AngleDelta(a.y, b.y),
            AngleDelta(a.z, b.z)
        );
    }

    // Apply computed angles to Unity transforms.
    // modelRoot: root transform that receives legsAngles as world rotation
    // lowerLumbar, upperLumbar, cranium: transforms for those bones
    // Note: you may need to adjust axis conversions depending on your model
    // multipliers allow per-axis sign/scale adjustments for bones (useful to invert axes per-model)
    public static void ApplyAnglesToBones(AnglesResult angles, Transform modelRoot, Transform lowerLumbar, Transform upperLumbar, Transform cranium,
        Vector3 legsMultiplier, Vector3 lowerMultiplier, Vector3 upperMultiplier, Vector3 headMultiplier)
    {
        if (modelRoot != null)
        {
            Vector3 legsEuler = new Vector3(angles.legsAngles.x * legsMultiplier.x, angles.legsAngles.y * legsMultiplier.y, angles.legsAngles.z * legsMultiplier.z);
            Quaternion legsWorld = Quaternion.Euler(legsEuler.x, legsEuler.y, legsEuler.z);
            modelRoot.rotation = legsWorld;
        }

        // lower torso world rotation
    Vector3 lowerEuler = new Vector3(angles.lowerTorsoAngles.x * lowerMultiplier.x, angles.lowerTorsoAngles.y * lowerMultiplier.y, angles.lowerTorsoAngles.z * lowerMultiplier.z);
    Quaternion lowerWorld = Quaternion.Euler(lowerEuler.x, lowerEuler.y, lowerEuler.z);
        if (lowerLumbar != null)
        {
            // set local rotation relative to modelRoot
            if (modelRoot != null)
            {
                lowerLumbar.localRotation = Quaternion.Inverse(modelRoot.rotation) * lowerWorld;
            }
            else
            {
                lowerLumbar.rotation = lowerWorld;
            }
        }

    Vector3 upperEuler = new Vector3(angles.upperTorsoAngles.x * upperMultiplier.x, angles.upperTorsoAngles.y * upperMultiplier.y, angles.upperTorsoAngles.z * upperMultiplier.z);
    Quaternion upperWorld = Quaternion.Euler(upperEuler.x, upperEuler.y, upperEuler.z);
        if (upperLumbar != null)
        {
            if (lowerLumbar != null)
            {
                upperLumbar.localRotation = Quaternion.Inverse(lowerLumbar.rotation) * upperWorld;
            }
            else if (modelRoot != null)
            {
                upperLumbar.localRotation = Quaternion.Inverse(modelRoot.rotation) * upperWorld;
            }
            else
            {
                upperLumbar.rotation = upperWorld;
            }
        }

    Vector3 headEuler = new Vector3(angles.headAngles.x * headMultiplier.x, angles.headAngles.y * headMultiplier.y, angles.headAngles.z * headMultiplier.z);
    Quaternion headWorld = Quaternion.Euler(headEuler.x, headEuler.y, headEuler.z);
        if (cranium != null)
        {
            if (lowerLumbar != null)
            {
                cranium.localRotation = Quaternion.Inverse(lowerLumbar.rotation) * headWorld;
            }
            else if (modelRoot != null)
            {
                cranium.localRotation = Quaternion.Inverse(modelRoot.rotation) * headWorld;
            }
            else
            {
                cranium.rotation = headWorld;
            }
        }
    }
}
