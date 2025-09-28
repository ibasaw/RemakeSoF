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
        public float movementDir; // 0-7 movement direction
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

        // legs yaw = headYaw + 2*movementOffsets[dir] (exact SoF2 implementation)
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

        // Exact SoF2 calculation
        lowerTorsoAngles.y = Mathf.Clamp(AngleDelta(headAngles.y, legsAngles.y), -90f, 90f);
        upperTorsoAngles.y = lowerTorsoAngles.y / 2f;
        lowerTorsoAngles.y = legsAngles.y + lowerTorsoAngles.y / 2f;
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
        res.movementDir = dir;

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
    // lowerLumbar, upperLumbar, cranium: transforms for those bones
    // Note: SoF2 uses PITCH=0, YAW=1, ROLL=2, Unity uses X=Pitch, Y=Yaw, Z=Roll
    // multipliers allow per-axis sign/scale adjustments for bones (useful to invert axes per-model)
    public static void ApplyAnglesToBones(AnglesResult angles, Transform lowerLumbar, Transform upperLumbar, Transform cranium,
        Vector3 legsMultiplier, Vector3 lowerMultiplier, Vector3 upperMultiplier, Vector3 headMultiplier)
    {

        // Apply lower torso rotation (relative to legs/parent)
        if (lowerLumbar != null)
        {
            // SoF2: lowerTorsoAngles = (pitch, yaw, roll) -> Unity: (x, y, z)
            // Model has skeleton_root rotation (-90, 0, 0), so we need to adjust axes
            // With -90° X rotation: Y becomes Z, Z becomes -Y
            Vector3 lowerEuler = new Vector3(
                angles.lowerTorsoAngles.x * lowerMultiplier.x,  // PITCH[0] -> x (hoch/runter)
                angles.lowerTorsoAngles.y * lowerMultiplier.y,  // YAW[1] -> y (links/rechts)
                angles.lowerTorsoAngles.z * lowerMultiplier.z  // ROLL[2] -> z (kippen, inverted)
            );
            lowerLumbar.localRotation = Quaternion.Euler(lowerEuler);
        }

        // Apply upper torso rotation (relative to lower torso)
        if (upperLumbar != null)
        {
            Vector3 upperEuler = new Vector3(
                angles.upperTorsoAngles.x * upperMultiplier.x,  // PITCH[0] -> x (hoch/runter)
                angles.upperTorsoAngles.y * upperMultiplier.y,  // YAW[1] -> y (links/rechts)
                angles.upperTorsoAngles.z * upperMultiplier.z  // ROLL[2] -> z (kippen, inverted)
            );
            upperLumbar.localRotation = Quaternion.Euler(upperEuler);
        }

        // Apply head rotation (relative to upper torso)
        if (cranium != null)
        {
            Vector3 headEuler = new Vector3(
                angles.headAngles.x * headMultiplier.x,  // PITCH[0] -> x (hoch/runter)
                angles.headAngles.y * headMultiplier.y,  // YAW[1] -> y (links/rechts)
                angles.headAngles.z * headMultiplier.z  // ROLL[2] -> z (kippen, inverted)
            );
            cranium.localRotation = Quaternion.Euler(headEuler);
        }
    }
}
