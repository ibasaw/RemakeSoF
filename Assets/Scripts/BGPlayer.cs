using UnityEngine;

// BGPlayer: a C# port of BG_PlayerAngles from SoF2 (exact implementation)
// Provides angle calculation for legs / lower torso / upper torso / head and an Apply helper
public static class BGPlayer
{
    // Constants from SoF2
    private const int LEAN_TIME = 250;
    private const int LEAN_OFFSET = 30;
    private const int PAIN_TWITCH_TIME = 200;
    private const int TORSO_IDLE_PISTOL = 281; // From bg_public.h animNumber_t

    public struct AnimInfo
    {
        public float yawAngle;
        public bool yawing;
        public float pitchAngle;
        public bool pitching;
        public int anim;        // Animation number (like SoF2)
        public int animTime;    // Animation time (like SoF2)
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

    // Exact SoF2 BG_SwingAngles implementation
    private static void BG_SwingAngles(float destination, float swingTolerance, float clampTolerance, float speed, ref float angle, ref bool swinging, int frameTime)
    {
        float swing;
        float move;
        float scale;

        if (!swinging)
        {
            // see if a swing should be started
            swing = AngleDelta(angle, destination);
            if (swing > swingTolerance || swing < -swingTolerance)
            {
                swinging = true;
            }
        }

        if (!swinging)
        {
            return;
        }

        // modify the speed depending on the delta
        // so it doesn't seem so linear
        swing = AngleDelta(destination, angle);
        scale = Mathf.Abs(swing);
        if (scale < swingTolerance * 0.5f)
        {
            scale = 0.5f;
        }
        else if (scale < swingTolerance)
        {
            scale = 1.0f;
        }
        else
        {
            scale = 2.0f;
        }

        // swing towards the destination angle
        if (swing >= 0)
        {
            move = frameTime * scale * speed;
            if (move >= swing)
            {
                move = swing;
                swinging = false;
            }
            angle = AngleMod(angle + move);
        }
        else if (swing < 0)
        {
            move = frameTime * scale * -speed;
            if (move <= swing)
            {
                move = swing;
                swinging = false;
            }
            angle = AngleMod(angle + move);
        }

        // clamp to no more than tolerance
        swing = AngleDelta(destination, angle);
        if (swing > clampTolerance)
        {
            angle = AngleMod(destination - (clampTolerance - 1));
        }
        else if (swing < -clampTolerance)
        {
            angle = AngleMod(destination + (clampTolerance - 1));
        }
    }

    // Exact SoF2 BG_AddPainTwitch implementation
    private static void BG_AddPainTwitch(int painTime, int painDirection, int currentTime, ref Vector3 torsoAngles)
    {
        int t = currentTime - painTime;
        if (t >= PAIN_TWITCH_TIME)
        {
            return;
        }

        float f = 1.0f - (float)t / PAIN_TWITCH_TIME;

        if (painDirection != 0)
        {
            torsoAngles.z += 20 * f; // roll twitch
        }
        else
        {
            torsoAngles.z -= 20 * f; // roll twitch
        }
    }

    // Exact SoF2 BG_PlayerAngles implementation
    public static AnglesResult PlayerAngles(
        Vector3 startAngles,
        AnimInfo torsoInfo,
        AnimInfo legsInfo,
        int leanOffset,
        int painTime,
        int painDirection,
        float currentTime,
        float movementDir,
        Vector3 realvelocity,
        bool dead,
        int frameTime
    )
    {
        AnglesResult res = new AnglesResult();

        // movement offsets from SoF2 (exact values)
        int[] movementOffsets = new int[8] { 0, 22, 45, -22, 0, 22, -45, -22 };

        Vector3 headAngles = startAngles; // pitch,yaw,roll
        Vector3 legsAngles = Vector3.zero;
        Vector3 lowerTorsoAngles = Vector3.zero;
        Vector3 upperTorsoAngles = Vector3.zero;

        // ensure yaw is normalized
        headAngles.y = AngleMod(headAngles.y);

        Vector3 velocity = realvelocity;
        float speed = velocity.magnitude;

        // --------- yaw -------------

        // allow yaw to drift a bit (exact SoF2 logic)
        if ((legsInfo.anim & ~0x1000) != TORSO_IDLE_PISTOL) // ANIM_TOGGLEBIT = 0x1000
        {
            // if not standing still, always point all in the same direction
            torsoInfo.yawing = true;
            torsoInfo.pitching = true;
            legsInfo.yawing = true;
        }

        // adjust legs for movement dir
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

        // torso swing angles (exact SoF2 parameters)
        BG_SwingAngles(lowerTorsoAngles.y, 25f, 90f, 0.3f, ref torsoInfo.yawAngle, ref torsoInfo.yawing, frameTime);
        BG_SwingAngles(legsAngles.y, 40f, 90f, 0.3f, ref legsInfo.yawAngle, ref legsInfo.yawing, frameTime);

        if (leanOffset != 0)
        {
            legsAngles.y = headAngles.y;
        }
        else
        {
            legsAngles.y = legsInfo.yawAngle;
        }

        // Exact SoF2 angle hierarchy calculation
        lowerTorsoAngles.y = Mathf.Clamp(AngleDelta(headAngles.y, legsAngles.y), -90f, 90f);
        upperTorsoAngles.y = lowerTorsoAngles.y / 2f;
        lowerTorsoAngles.y = legsAngles.y + lowerTorsoAngles.y / 2f;
        headAngles.y -= upperTorsoAngles.y;

        // --------- pitch -------------

        // only show a fraction of the pitch angle in the torso
        float dest;
        if (headAngles.x > 180f)
        {
            dest = (-360f + headAngles.x) * 0.75f;
        }
        else
        {
            dest = headAngles.x * 0.75f;
        }
        lowerTorsoAngles.x = dest;

        // --------- roll -------------

        // Add in leanoffset
        if (leanOffset != 0)
        {
            lowerTorsoAngles.z -= ((float)leanOffset * 1.25f);
            lowerTorsoAngles.y -= 1.25f * ((float)leanOffset / LEAN_OFFSET) * dest;
            headAngles.y -= ((float)leanOffset / LEAN_OFFSET) * dest;
            headAngles.z -= ((float)leanOffset * 1.25f);
        }
        else if (speed > 0f)
        {
            // Velocity-based roll (exact SoF2 implementation)
            Vector3[] axis = new Vector3[3];
            float side;

            speed *= 0.025f;

            AnglesToAxis(legsAngles, axis);
            side = speed * Vector3.Dot(velocity, axis[1]); // axis[1] is right vector
            legsAngles.z -= side;
        }

        // pain twitch
        BG_AddPainTwitch(painTime, painDirection, (int)currentTime, ref lowerTorsoAngles);

        // pull the angles back out of the hierarchial chain
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

    // Convert angles to axis matrix (exact SoF2 implementation)
    private static void AnglesToAxis(Vector3 angles, Vector3[] axis)
    {
        float angle;
        float sr, sp, sy, cr, cp, cy;

        angle = angles.y * (Mathf.PI * 2 / 360);
        sy = Mathf.Sin(angle);
        cy = Mathf.Cos(angle);
        angle = angles.x * (Mathf.PI * 2 / 360);
        sp = Mathf.Sin(angle);
        cp = Mathf.Cos(angle);
        angle = angles.z * (Mathf.PI * 2 / 360);
        sr = Mathf.Sin(angle);
        cr = Mathf.Cos(angle);

        // forward vector (axis[0])
        axis[0].x = cp * cy;
        axis[0].y = cp * sy;
        axis[0].z = -sp;

        // right vector (axis[1])
        axis[1].x = (-1 * sr * sp * cy + -1 * cr * -sy);
        axis[1].y = (-1 * sr * sp * sy + -1 * cr * cy);
        axis[1].z = -1 * sr * cp;

        // up vector (axis[2])
        axis[2].x = (cr * sp * cy + -sr * -sy);
        axis[2].y = (cr * sp * sy + -sr * cy);
        axis[2].z = cr * cp;
    }
}
