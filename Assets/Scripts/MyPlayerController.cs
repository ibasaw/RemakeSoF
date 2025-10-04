using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MyPlayerController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 5f;
    public float jumpHeight = 0.5f;
    public float rotationSpeed = 10f;
    public float rbMass = 60f;
    public float rbDrag = 0.5f;
    public float acceleration = 70f;

    [Header("Ground Check (Rigidbody)")]
    [SerializeField] private float groundCheckRadius = 0.13f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Camera/Rotation")]
    [SerializeField] private readonly bool shouldFaceMoveDirection = true;
    [SerializeField] private Transform yawTarget;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] public bool isAiming = false;
    [SerializeField] public bool isNPC = false;

    // internal variables for movement
    private bool isGrounded;
    private bool isAttacking;
    // Input System
    private AvatarActions inputActions;

    // --- AIR STRAFE / BHOP SETTINGS ---
    [Header("BHOP Settings")]
    [SerializeField] private readonly float airAccel = 12f;
    [SerializeField] private readonly float maxAirSpeed = 300f;

    [Tooltip("Maus-Einfluss (raw mouse delta -> seitlicher Push).")]
    public float mouseStrafeMultiplier = 0.002f;

    [Tooltip("Skalierung des Maus-Pushes (Feintuning).")]
    public float mouseAccelFactor = 10f;

    [Tooltip("Wenn true: AirStrafe nur aktiv wenn isAiming == true")]
    public bool onlyWhileAiming = false;

    [Tooltip("Maus-Beschleunigung Cap pro Frame")]
    public float maxMouseAccelPerFrame = 1f;

    [Header("Landing Bonus Settings")]
    [Tooltip("Landing Bonus Multiplikator (wie viel Speed beim Landen erhalten wird)")]
    public float landingBonusMultiplier = 0.05f;

    [Tooltip("Maximaler Landing Bonus")]
    public float maxLandingBonus = 10f;

    // intern: gewünschte horizontale Geschwindigkeit
    private Vector3 desiredHorizontalVelocity = Vector3.zero;

    // Debug-Variablen für Strafing
    private Vector3 lastWishDir = Vector3.zero;
    private float lastDynamicAccel = 0f;
    private float lastAddSpeed = 0f;
    private float lastMouseAccel = 0f;
    private float strafeEfficiency = 0f;

    // Bunnyhop-Distanz Tracking
    private Vector3 jumpStartPosition = Vector3.zero;
    private float currentJumpDistance = 0f;
    private float maxJumpDistance = 0f;
    private bool isTrackingJump = false;

    // internal variables for bhop
    private Vector3 lastPosition;
    private Vector2 moveInput;
    private Vector3 lastHorizontalVelocity = Vector3.zero;
    private float speedGainedThisTick = 0f;
    // Landing-Bonus für Bhop
    private float landingSpeedBonus = 0f;
    private bool lastGrounded = true;
    private float timeInAir = 0f;

    // Visuelle Debug-Hilfe
    private readonly bool showStrafeVisualization = true;

    private void OnGUI()
    {
        if (rb == null) return;

        float scaleFactor = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleFactor, scaleFactor, 1f));

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 25,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };

        GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            normal = { textColor = Color.white }
        };

        float y = 10f;
        float x = 10f;
        float lineHeight = 28f;

        // === LINKE SEITE: GRUNDLEGENDE DEBUG INFO ===
        GUI.Label(new Rect(x, y, 800, lineHeight), "=== DEBUG INFO ===", headerStyle); y += lineHeight * 1.2f;

        // Player States
        GUI.Label(new Rect(x, y, 800, lineHeight), $"IsGrounded: {isGrounded}   isAiming: {isAiming}   isAttacking: {isAttacking}", valueStyle); y += lineHeight;

        // Rigidbody
        Vector3 horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        GUI.Label(new Rect(x, y, 800, lineHeight), $"Horiz Speed: {horiz.magnitude:F2}", valueStyle); y += lineHeight;
        GUI.Label(new Rect(x, y, 800, lineHeight), $"Vertical Speed: {rb.linearVelocity.y:F2}", valueStyle); y += lineHeight;
        GUI.Label(new Rect(x, y, 800, lineHeight), $"Acceleration: {acceleration}   Mass: {rb.mass}   Drag: {rb.linearDamping}", valueStyle); y += lineHeight;

        // Input
        GUI.Label(new Rect(x, y, 800, lineHeight), $"MoveInput: {moveInput}   Sprint: {inputActions.Player.Walk.ReadValue<float>()}", valueStyle); y += lineHeight;

        // Bunnyhop-Distanz (NEU!)
        GUI.color = Color.yellow;
        GUI.Label(new Rect(x, y, 800, lineHeight), $"=== BUNNYHOP DISTANZ ===", headerStyle); y += lineHeight * 1.2f;
        GUI.Label(new Rect(x, y, 800, lineHeight), $"Aktuelle Sprungdistanz: {currentJumpDistance:F2}m", valueStyle); y += lineHeight;
        GUI.Label(new Rect(x, y, 800, lineHeight), $"Maximale Sprungdistanz: {maxJumpDistance:F2}m", valueStyle); y += lineHeight;
        GUI.Label(new Rect(x, y, 800, lineHeight), $"Tracking: {(isTrackingJump ? "AKTIV" : "INAKTIV")}", valueStyle); y += lineHeight;
        GUI.color = Color.white;

        // Jump Info
        float g = Mathf.Abs(UnityEngine.Physics.gravity.y);
        float jumpVelocity = Mathf.Sqrt(2f * g * jumpHeight);
        GUI.Label(new Rect(x, y, 800, lineHeight), $"JumpHeight: {jumpHeight}   JumpVelocity: {jumpVelocity:F2}", valueStyle); y += lineHeight;
        GUI.Label(new Rect(x, y, 600, lineHeight), $"Speed Gained: {speedGainedThisTick:F2}", valueStyle); y += lineHeight;

        // === RECHTE SEITE: BUNNYHOP DEBUG INFO ===
        float rightX = Screen.width / scaleFactor - 400f; // Rechte Seite
        float rightY = 10f;

        if (!isGrounded)
        {
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), "=== BUNNYHOP DEBUG ===", headerStyle); rightY += lineHeight * 1.2f;

            Vector2 currentLookDelta = GetLookDelta();
            Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"A/D Input: {moveInput.x:F2}", valueStyle); rightY += lineHeight;
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Mouse Delta X: {currentLookDelta.x:F2}", valueStyle); rightY += lineHeight;
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Strafe Efficiency: {strafeEfficiency:F2}", valueStyle); rightY += lineHeight;
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Dynamic Accel: {lastDynamicAccel:F2}", valueStyle); rightY += lineHeight;
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Add Speed: {lastAddSpeed:F2}", valueStyle); rightY += lineHeight;
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Mouse Accel: {lastMouseAccel:F2}", valueStyle); rightY += lineHeight;

            // Style Synchronisation Check
            float mouseStrafeDirection = Mathf.Sign(currentLookDelta.x);
            float strafeDirection = Mathf.Sign(moveInput.x);
            bool isStrafeAligned = Mathf.Abs(moveInput.x) < 0.0001f || Mathf.Approximately(mouseStrafeDirection, strafeDirection);

            GUI.color = isStrafeAligned ? Color.green : Color.red;
            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Maus-A/D Sync: {(isStrafeAligned ? "PERFEKT" : "SCHLECHT")}", valueStyle); rightY += lineHeight;
            GUI.color = Color.white;

            // Empfehlung
            string recommendation = "";
            if (!isStrafeAligned)
                recommendation = "Drücke A/D und bewege Maus in GLEICHE Richtung!";
            else if (strafeEfficiency < 0.3f)
                recommendation = "Du bewegst dich nicht hauptsächlich vorwärts!";
            else if (strafeEfficiency < 0.6f)
                recommendation = "OK, aber Winkel ist nicht optimal";
            else if (strafeEfficiency < 0.8f)
                recommendation = "GUT, feintune die Mausbewegung";
            else
                recommendation = "PERFEKT! Optimales BHOP!";

            GUI.Label(new Rect(rightX, rightY, 400, lineHeight), $"Recommendation: {recommendation}", valueStyle); rightY += lineHeight;

            // Visuelle Richtungsanzeige
            if (showStrafeVisualization)
            {
                rightY += 10;
                GUI.Label(new Rect(rightX, rightY, 400, lineHeight), "=== RICHTUNGEN ===", headerStyle); rightY += lineHeight * 1.2f;

                // Velocity Richtung
                if (currentVel.magnitude > 0.1f)
                {
                    GUI.color = Color.green;
                    GUI.Label(new Rect(rightX, rightY, 200, lineHeight), "Velocity: →", valueStyle);
                    GUI.color = Color.white;
                }

                // Vorwärts-Richtung
                if (currentVel.y > 0.1f)
                {
                    GUI.color = Color.green;
                    string strafeDir = moveInput.y > 0 ? "Forward: ↑" : "Forward: ↓";
                    GUI.Label(new Rect(rightX, rightY, 200, lineHeight), strafeDir, valueStyle);
                }
                GUI.color = Color.white;

                // A/D Richtung
                if (Mathf.Abs(moveInput.x) > 0.1f)
                {
                    GUI.color = Color.red;
                    string strafeDir = moveInput.x > 0 ? "Strafing: →" : "Strafing: ←";
                    GUI.Label(new Rect(rightX + 200, rightY, 200, lineHeight), strafeDir, valueStyle);
                    GUI.color = Color.white;
                }
                rightY += lineHeight;

                // Effizienz-Balken
                GUI.color = Color.Lerp(Color.red, Color.green, strafeEfficiency);
                GUI.Box(new Rect(rightX, rightY, strafeEfficiency * 300, 20), "");
                GUI.color = Color.white;
                GUI.Label(new Rect(rightX, rightY, 300, 20), $"Efficiency: {strafeEfficiency:F2}", valueStyle);
                rightY += 25;
            }
        }
    }

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        inputActions = new AvatarActions();

        if (rb != null)
        {
            rb.mass = rbMass;
            rb.linearDamping = rbDrag;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        lastPosition = transform.position;
    }

    void OnEnable()
    {
        if (isNPC)
            return;

        inputActions.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += ctx => Jump();
        inputActions.Player.Attack.performed += ctx => Attack();
    }

    void OnDisable()
    {
        if (isNPC)
            return;

        // safe disable events
        inputActions.Player.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled -= ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed -= ctx => Jump();
        inputActions.Player.Attack.performed -= ctx => Attack();

        inputActions.Disable();
    }

    private void Attack()
    {
        if (isAttacking) return;
        animator?.SetTrigger("Attack");
        isAttacking = true;
    }

    public void OnAttackEnd() => isAttacking = false;

    public void DealDamage() => Debug.Log("DealDamage triggered!");

    private void HandleAutoJump()
    {
        // Lese Jump-Input
        bool jumpPressed = inputActions.Player.Jump.ReadValue<float>() > 0f;

        if (jumpPressed && isGrounded)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        Vector3 currentHorizontal = rb.linearVelocity;

        // Kamera-relative Bewegungsrichtung
        Vector3 forward = cameraTransform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = cameraTransform.right; right.y = 0f; right.Normalize();
        Vector3 wishdir = (forward * moveInput.y + right * moveInput.x).normalized;

        // Landing Bonus
        HandleLandingBonus();

        // Air-Time tracken
        timeInAir = isGrounded ? 0f : timeInAir + Time.fixedDeltaTime;

        // Bunnyhop-Distanz Tracking
        if (isTrackingJump)
        {
            currentJumpDistance = Vector3.Distance(jumpStartPosition, transform.position);
            if (currentJumpDistance > maxJumpDistance)
            {
                maxJumpDistance = currentJumpDistance;
            }

            // Tracking beenden wenn gelandet
            if (isGrounded)
            {
                isTrackingJump = false;
            }
        }

        lastGrounded = isGrounded;
        lastHorizontalVelocity = currentHorizontal;

        // Bewegung
        HandleMovement();
        HandleAutoJump(); // <-- Automatischer Jump
    }

    private void HandleLandingBonus()
    {
        // Wenn du gerade gelandet bist
        if (isGrounded && !lastGrounded)
        {
            landingSpeedBonus = lastHorizontalVelocity.magnitude * 0.01f; // 1% des letzten Bodenspeed
        }
    }

    private void HandleMovement()
    {
        // --- Ground Check ---
        Vector3 checkPos = transform.position + Vector3.down * 0.1f;
        isGrounded = UnityEngine.Physics.CheckSphere(checkPos, groundCheckRadius, groundMask);
        animator?.SetBool("Grounded", isGrounded);

        // leichte Bodenhaftung
        if (isGrounded && rb.linearVelocity.y < 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -2f, rb.linearVelocity.z);

        // Input & gewünschte Geschwindigkeit
        float speed = (inputActions.Player.Walk.ReadValue<float>() > 0) ? runSpeed : walkSpeed;

        Vector3 forward = (isAiming || isNPC) ? transform.forward : cameraTransform.forward;
        Vector3 right = (isAiming || isNPC) ? transform.right : cameraTransform.right;
        forward.y = 0f; right.y = 0f; forward.Normalize(); right.Normalize();

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x; // wishDirection

        Vector3 desired = Vector3.zero;
        if (moveDir.sqrMagnitude > 0.001f)
            desired = moveDir.normalized * Mathf.Clamp(moveDir.magnitude * speed, 0f, maxAirSpeed);

        desiredHorizontalVelocity = new Vector3(desired.x, 0f, desired.z);

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector2 currentLookDelta = GetLookDelta();
        // --- LANDING-BONUS für Bhop ---
        if (isGrounded && !lastGrounded)
        {
            landingSpeedBonus = Mathf.Min(lastHorizontalVelocity.magnitude * landingBonusMultiplier, maxLandingBonus);

            Vector3 horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).normalized;

            Vector3 horizDir = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).normalized;
            Vector3 targetHorizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) + horizDir * landingSpeedBonus;
            rb.linearVelocity = Vector3.MoveTowards(new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), targetHorizontal, acceleration * Time.fixedDeltaTime);

            Debug.Log(string.Format(
                "[Landing Bonus] {0:F2} " +
                "[Current Speed] {1:F2} " +
                "[Desired Speed] {2:F2} " +
                "[Time In Air] {3:F2}s " +
                "[Speed Gain] {4:F2} " +
                "[Efficiency] {5:F2} " +
                "[Move Direction A/D] {6:F2} " +
                "[Mouse delta] {7:F2} ",
                landingSpeedBonus,
                currentHorizontal.magnitude,
                desiredHorizontalVelocity.magnitude,
                timeInAir,
                speedGainedThisTick,
                strafeEfficiency,
                moveInput.x,
                currentLookDelta.x
            ));
        }

        lastGrounded = isGrounded;

        // --- Bodengeschwindigkeit mit Momentum ---
        if (isGrounded)
        {
            Vector3 moveDirNormalized = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : currentHorizontal.normalized;
            Vector3 targetHorizontal = desiredHorizontalVelocity + moveDirNormalized * landingSpeedBonus;

            // Apply once: nachdem der Bonus in targetHorizontal eingebracht wurde,
            landingSpeedBonus = 0f;

            animator.SetFloat("Move", targetHorizontal.magnitude);
            /*
            // Sanfte Beschleunigung
            Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontal, acceleration * Time.fixedDeltaTime);

            // Max Speed Cap
            if (newHorizontal.magnitude > maxAirSpeed)
                newHorizontal = newHorizontal.normalized * maxAirSpeed;

            Vector3 oldHV = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);

            // SpeedGain
            speedGainedThisTick = (newHorizontal - oldHV).magnitude;
            */
        }
        else
        {
            // --- AirStrafe ---
            if (!onlyWhileAiming || isAiming)
            {
                ApplyAirStrafe(moveInput.x);
            }
        }

        // --- Rotation / Animation ---
        Quaternion targetRot = Quaternion.identity;
        if (isAiming)
        {
            Vector3 lookDir = yawTarget != null ? yawTarget.forward : transform.forward;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
                targetRot = Quaternion.LookRotation(lookDir);
        }
        else if (shouldFaceMoveDirection && desiredHorizontalVelocity.sqrMagnitude > 0.001f)
            targetRot = Quaternion.LookRotation(desiredHorizontalVelocity, Vector3.up);
        else
        {
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.01f)
                targetRot = Quaternion.LookRotation(camForward, Vector3.up);
        }

        if (targetRot != Quaternion.identity)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));

        animator?.SetBool("IsMoving", desiredHorizontalVelocity.sqrMagnitude > 0.001f);

        lastHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    void OnAnimatorMove()
    {
        // Root Motion direkt auf Controller anwenden
        if (animator)
        {
            Vector3 deltaPos = animator.deltaPosition;
            Quaternion deltaRot = animator.deltaRotation;

            Debug.Log(string.Format(
                "[Animator Move] sqrMag={0:F2} " +
                "[Delta Pos Magnitude] {1:F2} " +
                "[Delta Rot sqrMag] {2:F2} " +
                "[Delta Rot Magnitude] {3:F2}",
                deltaPos.sqrMagnitude,
                deltaPos.magnitude,
                deltaRot.eulerAngles.sqrMagnitude,
                deltaRot.eulerAngles.magnitude
            ));


            // Bewegung vom Animator → CharacterController
            rb.MovePosition(rb.position + deltaPos);
            rb.MoveRotation(rb.rotation * deltaRot);
        }
    }

    private void ApplyAirStrafe(float strafeInput)
    {
        Vector3 hv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector2 lookDelta = GetLookDelta();

        Vector3 rightVec = (yawTarget != null && (isAiming || isNPC)) ? yawTarget.right : cameraTransform.right;
        Vector3 forwardVec = (yawTarget != null && (isAiming || isNPC)) ? yawTarget.forward : cameraTransform.forward;
        rightVec.y = 0; forwardVec.y = 0; rightVec.Normalize(); forwardVec.Normalize();

        Vector3 strafeDir = Mathf.Abs(strafeInput) > 0.001f ? rightVec * Mathf.Sign(strafeInput) : Vector3.zero;
        float mouseDir = Mathf.Sign(lookDelta.x);
        float strafeDirSign = Mathf.Sign(strafeInput);
        bool aligned = Mathf.Approximately(mouseDir, strafeDirSign);

        if (aligned && Mathf.Abs(lookDelta.x) > 0.001f)
        {
            Vector3 wishDir = forwardVec + strafeDir * 0.1f;
            wishDir.Normalize();

            // --- Dynamische AirAccel Berechnung ---
            float dynamicAirAccel = airAccel;

            // Mehr Speed bei besserer Effizienz
            dynamicAirAccel *= 1f + strafeEfficiency; // 1x bis 2x
                                                      // Optional: Bonus abhängig von Zeit in Air
            dynamicAirAccel *= 1f + Mathf.Clamp(timeInAir * 0.5f, 0f, 1f); // bis +100% bei langem AirTime

            float airAccelFactor = Mathf.Clamp(airAccel * (1f + strafeEfficiency) * Time.fixedDeltaTime, 0f, maxAirSpeed * 0.02f);
            hv = Vector3.MoveTowards(hv, hv + wishDir * airAccelFactor, airAccelFactor);

            // Maus-Push
            float mouseAccel = lookDelta.x * mouseStrafeMultiplier * mouseAccelFactor * Time.fixedDeltaTime;
            mouseAccel = Mathf.Clamp(mouseAccel, -maxMouseAccelPerFrame, maxMouseAccelPerFrame);
            hv += wishDir * mouseAccel;

            // Optional: Forward Boost basierend auf Side Speed
            float strafeSpeed = Vector3.Dot(hv, strafeDir);
            if (Mathf.Abs(strafeSpeed) > 0.1f)
            {
                float forwardBoost = Mathf.Abs(strafeSpeed) * 0.8f;
                hv += forwardVec * forwardBoost * Time.fixedDeltaTime;
                hv -= strafeDir * strafeSpeed * 0.8f;
            }

            lastWishDir = wishDir;
            lastDynamicAccel = dynamicAirAccel;
            lastAddSpeed = dynamicAirAccel * Time.fixedDeltaTime;
            lastMouseAccel = mouseAccel;
        }

        strafeEfficiency = Mathf.Clamp01(1f - Vector3.Angle(hv, forwardVec) / 90f);
        if (aligned && Mathf.Abs(strafeInput) > 0.001f)
            strafeEfficiency = Mathf.Min(1f, strafeEfficiency + 0.2f);

        Vector3 oldHV = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.linearVelocity = new Vector3(hv.x, rb.linearVelocity.y, hv.z);
        speedGainedThisTick = (hv - oldHV).magnitude;
    }

    private Vector2 GetLookDelta()
    {
        // Mouse priority
        if (Mouse.current != null && Mouse.current.delta != null)
        {
            return Mouse.current.delta.ReadValue();
        }
        // Gamepad fallback (right stick)
        if (Gamepad.current != null)
        {
            return Gamepad.current.rightStick.ReadValue();
        }
        return Vector2.zero;
    }

    bool CanJump() => isGrounded && !isAttacking && Mathf.Abs(rb.linearVelocity.y) < 0.1f;

    private void Jump()
    {
        if (!isGrounded) return;

        float g = Mathf.Abs(UnityEngine.Physics.gravity.y);
        float jumpVel = Mathf.Sqrt(2f * g * jumpHeight);

        Vector3 vel = rb.linearVelocity;
        vel.y = jumpVel; // nur Y neu setzen
        rb.linearVelocity = vel;

        timeInAir = 0f; // Air-Time zurücksetzen
        isGrounded = false;
        // Start Distanz-Tracking
        isTrackingJump = true;
        jumpStartPosition = transform.position;
    }
}
