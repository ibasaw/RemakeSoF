using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MeasureRootMotionSpeed : MonoBehaviour
{
    public Animator animator;
    public float sampleInterval = 0.5f;

    private float timer = 0f;
    private Vector3 accDelta = Vector3.zero;
    private int samples = 0;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = true;
        }
    }

    void OnAnimatorMove()
    {
        // deltaPosition liefert die Bewegung, die Animation in diesem Frame vorgibt
        Vector3 d = animator.deltaPosition;
        accDelta += d;
        samples++;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= sampleInterval)
        {
            Vector3 avgDelta = (samples > 0) ? accDelta / timer : Vector3.zero;
            float speed = avgDelta.magnitude; // units per second (wenn deltaPositions in world-units/frame -> durchschnitt pro sek)
            Debug.Log($"Measured speed over {timer:F2}s: {speed:F3} units/s  (avgDelta={avgDelta})");

            // reset
            timer = 0f;
            accDelta = Vector3.zero;
            samples = 0;
        }
    }
}
