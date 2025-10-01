using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayerControllerCustom : MonoBehaviour
{
	private Animator animator;
	// CharacterController removed - use capsule-based manual movement
	[Header("Capsule Settings (replaces CharacterController)")]
	[SerializeField] private float capsuleRadius = 0.5f;
	[SerializeField] private float capsuleHeight = 2.0f;
	[SerializeField] private Vector3 capsuleCenter = Vector3.zero;

	// Input System
	private AvatarActions inputActions;
	private Vector2 moveInput;

	[Header("Speed Scales")]
	[SerializeField] private float pm_duckScale = 0.25f;        // Speed scale when ducking
	[SerializeField] private float pm_swimScale = 0.50f;        // Speed scale when swimming
	[SerializeField] private float pm_wadeScale = 0.70f;        // Speed scale when wading
	[SerializeField] private float pm_ladderScale = 0.5f;

	[Header("SoF2 Movement Settings")]
	[SerializeField] public LayerMask groundMask = ~0;    // All layers or just "Ground" layer
	[SerializeField] private float groundCheckDistance = 0.3f;  // Distance to check for ground

	[Header("Movement Limits")]
	[SerializeField] private float pm_maxsteepness = 0.7f;      // maximum floor steepness
	[SerializeField] private float pm_maxstep = 18.0f;          // Maximum step height
	[SerializeField] private float pm_stepsize = 18.0f;         // Step size
	[SerializeField] private float pm_maxbarrier = 32.0f;       // maximum barrier height
	[SerializeField] private float pm_maxwaterjump = 19.0f;        // max water jump height

	[Header("Physics Constants")]
	[SerializeField] private float pm_accelerate = 6.0f;        // Ground acceleration
	[SerializeField] private float pm_airaccelerate = 1.0f;     // Air acceleration  
	[SerializeField] private float pm_flyaccelerate = 8.0f;     // Flying acceleration
	[SerializeField] private float pm_friction = 6.0f;          // Ground friction

	[SerializeField] private float pm_waterfriction = 3.0f;     // Water friction
	[SerializeField] private float pm_ladderfriction = 6.0f;    // Ladder friction
	[SerializeField] private float pm_headfriction = 0.0f;      // Friction when on someone's head
	[SerializeField] private float pm_spectatorfriction = 5.0f;  // Spectator friction

	[SerializeField] private float pm_watergravity = 400.0f; // Water acceleration

	[SerializeField] private float pm_wateraccelerate = 4.0f;  // Water acceleration
	[SerializeField] private float pm_maxswimvelocity = 150.0f;   // Water max swim velocity
	[SerializeField] private float pm_stopspeed = 100.0f;       // Stop speed threshold
	[SerializeField] private float pm_maxspeed = 320.0f;        // Maximum speed / max velocity
	[SerializeField] private float pm_maxwalkvelocity = 320.0f; // Maximum walk speed / max velocity
	[SerializeField] private float pm_maxcrouchvelocity = 100.0f;  // Maximum run speed / max velocity
	[SerializeField] private float pm_gravity = 800.0f;         // Gravity value
	[SerializeField] private float jumpVelocity = 270.0f;       // Jump velocity (from phys_jumpvel)
	[SerializeField] private float rotationSpeed = 10f;         // Rotation speed for character

	// SoF2 Physics Constants
	private const float OVERCLIP = 1.001f;                     // Overclip constant for sliding

	[Header("Camera/Rotation")]
	[SerializeField] private Transform yawTarget;
	[SerializeField] private Transform pitchTarget;
	[SerializeField] private Transform cameraTransform;
	[SerializeField] public bool isAiming = false;
	[SerializeField] public bool isNPC = false;

	[Header("Torso Settings")]
	[SerializeField] private float torsoMaxYaw = 90f; // degrees for normalization

	[Header("BGPlayer Bones")]
	[SerializeField] private Transform lowerLumbar;
	[SerializeField] private Transform upperLumbar;
	[SerializeField] private Transform cranium;
	[SerializeField] private Transform modelRoot;

	[Header("Bone Axis Multipliers")]
	[SerializeField] private Vector3 legsMultiplier = new Vector3(1f, 1f, 1f);
	[SerializeField] private Vector3 lowerMultiplier = new Vector3(1f, 1f, 1f);
	[SerializeField] private Vector3 upperMultiplier = new Vector3(1f, 1f, 1f);
	[SerializeField] private Vector3 headMultiplier = new Vector3(1f, 1f, 1f);

	// SoF2 Movement State
	private Vector3 velocity = Vector3.zero;           // Current velocity (x, y, z)
	private bool isGrounded = false;                   // Grounded state
	private bool isWalking = false;                    // Ground walking state
	private bool isJumping = false;                    // Jumping state
	private bool isSwimming = false;                   // Swimming state
	private bool isCrouching = false;                  // Crouching state
	private float jumpDebounce = 0f;                   // Jump debounce timer

	// Debug: store last BGPlayer results for OnGUI
	private BGPlayer.AnglesResult lastAngles;
	private float lastTorsoYawDeg = 0f;
	private float lastTorsoYawNormalized = 0f;
	private float lastHeadPitch = 0f;
	private float lastHeadYaw = 0f;

	// class fields
	private Vector3 lastMoveDirection = Vector3.forward; // merkt sich die letzte NonZero-Richtung
	public float legsRotationSmooth = 8f;   // smoothing wenn im Stand die Beine nachziehen
	public float standCameraInfluence = 0.0f; // 0 = in Stand niemals zur Kamera drehen, 0.05-0.2 = langsam nachziehen
	public Quaternion skeletonOffset = Quaternion.Euler(0f, 90f, 0f); // dein Y-Offset
	[SerializeField] private float legsYawOffsetDegrees = 75f; // legs offset so left foot leads slightly

	[Header("Idle Facing Offsets (by movement dir 0..7)")]
	// 0:fwd,1:fwd-right,2:right,3:back-right,4:back,5:back-left,6:left,7:fwd-left
	[SerializeField] private float[] idleYawByDir = new float[8] { 112f, 45f, 68f, 68f, 112f, 180f, 180f, 90f };
	[SerializeField] private float strafeYawDegrees = 12f; // strafe yaw twist magnitude

	[Header("Animator Smoothing")]
	[SerializeField] private float animParamSmooth = 10f; // higher = faster response
	private float animHorizontal = 0f;
	private float animVertical = 0f;

	[Header("Torso-Legs Follow")]
	[SerializeField] private float torsoFollowYawInfluence = 4f; // legs yaw catch-up when torso twists

	[Header("Head Settings")]
	[SerializeField] private float headForwardBlend = 0.2f; // blend toward body forward so head looks straighter
	[SerializeField] private bool headSwapPitchRoll = false;   // swap lean pitch/roll for head bone axes
	[SerializeField] private float headPitchMultiplier = 0.3f;  // scale head pitch from lean
	[SerializeField] private float headRollMultiplier = 0.3f;   // scale head roll from lean
	[SerializeField] private int headPitchSign = 1;             // 1 or -1 to flip pitch
	[SerializeField] private int headRollSign = -1;             // 1 or -1 to flip roll (default -1 fixes common Z flip)

	// BGPlayer animation state
	// Smoothed legs forward to avoid snapping/jitter
	private Vector3 smoothedLegsForward = Vector3.forward;
	private int lastMoveDirIndex = 0;

	// Lean state
	private bool isLeaningLeft = false;
	private bool isLeaningRight = false;
	private int leanOffset = 0; // -30 for left, +30 for right, 0 for none

	[Header("Lean Settings")]
	[SerializeField] private float rollLeanDegrees = 15f;  // left/right roll lean magnitude
	[SerializeField] private float pitchLeanDegrees = 12f; // forward/backward pitch lean magnitude
	[SerializeField] private float leanSmooth = 8f;        // smoothing speed for lean interpolation

	// Smoothed lean state
	private Vector2 currentLeanAngles = Vector2.zero; // x = roll, y = pitch

	private void Awake()
	{
		// no CharacterController - using capsule-based physics
		animator = GetComponentInChildren<Animator>();
		inputActions = new AvatarActions();

		// Load skeleton configuration
		SkeletonConfigLoader.LoadSkeletonConfig("Data/skeletons/average_sleeves.skl");

		// Initialize smoothed legs forward with current facing
		Vector3 initialForward = transform.forward;
		initialForward.y = 0f;
		if (initialForward.sqrMagnitude < 0.0001f)
			initialForward = Vector3.forward;
		smoothedLegsForward = initialForward.normalized;
	}

	private void OnEnable()
	{
		if (isNPC) return;
		inputActions.Enable();
		inputActions.Player.Move.performed += OnMovePerformed;
		inputActions.Player.Move.canceled += OnMoveCanceled;
		inputActions.Player.Jump.performed += OnJumpPerformed;
		inputActions.Player.Crouch.performed += OnCrouchPerformed;
		inputActions.Player.Crouch.canceled += OnCrouchCanceled;
		inputActions.Player.LeanLeft.performed += OnLeanLeftPerformed;
		inputActions.Player.LeanLeft.canceled += OnLeanLeftCanceled;
		inputActions.Player.LeanRight.performed += OnLeanRightPerformed;
		inputActions.Player.LeanRight.canceled += OnLeanRightCanceled;
	}

	private void OnCrouchPerformed(InputAction.CallbackContext ctx)
	{
		Debug.Log("C Pressed");
		isCrouching = true;
		animator?.SetBool("IsCrouching", isCrouching);
		//TODO
	}
	private void OnCrouchCanceled(InputAction.CallbackContext ctx)
	{
		Debug.Log("C Released");
		isCrouching = false;
		animator?.SetBool("IsCrouching", isCrouching);
		//TODO
	}
	private void OnLeanLeftPerformed(InputAction.CallbackContext ctx)
	{
		Debug.Log("Q Pressed - Lean Left");
		isLeaningLeft = true;
		UpdateLeanOffset();
	}
	private void OnLeanLeftCanceled(InputAction.CallbackContext ctx)
	{
		Debug.Log("Q Released - Stop Lean Left");
		isLeaningLeft = false;
		UpdateLeanOffset();
	}
	private void OnLeanRightPerformed(InputAction.CallbackContext ctx)
	{
		Debug.Log("E Pressed - Lean Right");
		isLeaningRight = true;
		UpdateLeanOffset();
	}
	private void OnLeanRightCanceled(InputAction.CallbackContext ctx)
	{
		Debug.Log("E Released - Stop Lean Right");
		isLeaningRight = false;
		UpdateLeanOffset();
	}

	private void UpdateLeanOffset()
	{
		if (isLeaningLeft && !isLeaningRight)
		{
			leanOffset = -30; // Left lean
		}
		else if (isLeaningRight && !isLeaningLeft)
		{
			leanOffset = 30; // Right lean
		}
		else
		{
			leanOffset = 0; // No lean
		}
	}

	private void OnDisable()
	{
		if (isNPC) return;
		inputActions.Player.Move.performed -= OnMovePerformed;
		inputActions.Player.Move.canceled -= OnMoveCanceled;
		inputActions.Player.Jump.performed -= OnJumpPerformed;
		inputActions.Disable();
	}

	private void OnMovePerformed(InputAction.CallbackContext ctx)
	{
		moveInput = ctx.ReadValue<Vector2>();
		Debug.Log($"Move Input: {moveInput}");

		// Calculate the intended movement direction based on camera and input
		Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
		Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
		forward.y = 0;
		right.y = 0;
		forward.Normalize();
		right.Normalize();

		Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
		if (moveDirection.sqrMagnitude > 0.01f)
		{
			lastMoveDirection = moveDirection.normalized;
		}
	}

	private void OnMoveCanceled(InputAction.CallbackContext ctx)
	{
		moveInput = Vector2.zero;
	}

	private void OnJumpPerformed(InputAction.CallbackContext ctx)
	{
		TryJump();
	}

	// Try to perform a jump immediately (removes queued-jump logic)
	private void TryJump()
	{
		// Can't jump if debounce active
		if (jumpDebounce > 0f)
			return;

		// Must be grounded to initiate a jump
		if (!isGrounded)
			return;

		// If already jumping, ignore
		if (isJumping)
			return;

		// Perform jump now
		isJumping = true;
		jumpDebounce = 0.25f; // 250ms debounce like SoF2
		velocity.y = jumpVelocity;
	}

	private bool CheckGrounded()
	{
		// Capsule bottom and top in world space
		float halfHeight = Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
		Vector3 center = transform.TransformPoint(capsuleCenter);
		Vector3 top = center + Vector3.up * halfHeight;
		Vector3 bottom = center - Vector3.up * halfHeight;

		// Check by casting slightly down from current position
		float castDistance = groundCheckDistance + 0.01f;
		if (Physics.CapsuleCast(top, bottom, capsuleRadius * 0.9f, Vector3.down, out RaycastHit hit, castDistance, groundMask))
		{
			// Consider grounded if the normal is reasonably upwards
			if (Vector3.Dot(hit.normal, Vector3.up) > pm_maxsteepness)
				return true;
		}

		return false;
	}

	private void Update()
	{
		// Ground state with raycast check
		isGrounded = CheckGrounded();
		animator?.SetBool("IsGrounded", isGrounded);

		// jump debounce
		if (jumpDebounce > 0f) jumpDebounce -= Time.deltaTime;

		// restliche Logik -> ersetze Time.deltaTime mit dt
		isGrounded = CheckGrounded();
		if (isGrounded) PM_WalkMove();
		else PM_AirMove();

		// Check for swimming (simple water detection)
		isSwimming = transform.position.y < 0f; // Assuming water level is at y=0

		// Apply gravity
		ApplyGravity();

		// Move character
		MoveCharacter();

		// Rotate toward aim/camera/move
		HandleRotation();

		// Animator locomotion values
		Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
		bool isMoving = horizontalVel.sqrMagnitude > 0.001f;
		animator?.SetBool("IsMoving", isMoving);
		animator?.SetFloat("Speed", horizontalVel.magnitude);
		// Smooth animator parameters for better transition blending
		float animT = Mathf.Clamp01(animParamSmooth * Time.deltaTime);
		animHorizontal = Mathf.Lerp(animHorizontal, moveInput.x, animT);
		animVertical = Mathf.Lerp(animVertical, moveInput.y, animT);
		animator?.SetFloat("Horizontal", animHorizontal);
		animator?.SetFloat("Vertical", animVertical);

		// Set walking state based on input (like SoF2)
		isWalking = isGrounded && (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f);
	}

	private void LateUpdate()
	{
		// The skeleton has an offset rotation.
		// We apply a counter-rotation to correct the orientation.
		// You mentioned it was on the Y axis.
		Quaternion offset = Quaternion.Euler(0, 90, 0);
		Quaternion legsOffset = Quaternion.Euler(0, legsYawOffsetDegrees, 0);

		// Rotate modelRoot (legs) to face movement input direction (W/A/S/D)
		if (modelRoot != null)
		{
			Vector3 fwd = cameraTransform != null ? cameraTransform.forward : transform.forward;
			Vector3 rgt = cameraTransform != null ? cameraTransform.right : transform.right;
			fwd.y = 0f; rgt.y = 0f; fwd.Normalize(); rgt.Normalize();
			bool hasInput = moveInput.sqrMagnitude > 0.0001f;
			if (hasInput)
			{
				// Legs orientation: keep forward when going straight back; mirror strafe when moving back-diagonal
				float forwardComp = Mathf.Abs(moveInput.y);
				float effectiveX = (moveInput.y < 0f) ? -moveInput.x : moveInput.x;
				Vector3 inputDir = fwd * forwardComp + rgt * effectiveX;
				if (inputDir.sqrMagnitude > 0.0001f)
				{
					Vector3 desiredFlat = inputDir; desiredFlat.y = 0f;
					float t = Mathf.Clamp01(legsRotationSmooth * Time.deltaTime);
					smoothedLegsForward = Vector3.Slerp(smoothedLegsForward, desiredFlat.normalized, t);
				}
				// Update last movement dir index based on raw input
				lastMoveDirIndex = (int)ComputeMovementDir(moveInput);
			}
			/*else
			{
				// When idle, slowly follow camera yaw based on standCameraInfluence
				float idleFollowT = Mathf.Clamp01(standCameraInfluence * Time.deltaTime);
				if (idleFollowT > 0f)
				{
					smoothedLegsForward = Vector3.Slerp(smoothedLegsForward, fwd, idleFollowT);
				}
			}*/

			// Additionally, when torso (upper/lower) twists to follow camera, let legs catch up proportionally
			// Apply this subtly while idle; movement uses input-driven facing, idle uses idleYawByDir offset
			if (!hasInput)
			{
				// Compute yaw delta between current legs forward and camera forward
				float yawDelta = Vector3.SignedAngle(smoothedLegsForward, fwd, Vector3.up);
				float torsoDrivenT = Mathf.Clamp01(torsoFollowYawInfluence * Time.deltaTime * (Mathf.Abs(yawDelta) / 90f));
				if (torsoDrivenT > 0f)
				{
					smoothedLegsForward = Vector3.Slerp(smoothedLegsForward, fwd, torsoDrivenT);
				}
			}
			// Use smoothed forward direction for legsLook
			Quaternion legsLook = Quaternion.LookRotation(smoothedLegsForward, Vector3.up);
			// When idle, apply SoF2 idle correction (2->1, 6->7) before using idleYawByDir
			if (!hasInput)
			{
				int idleDir = Mathf.Clamp(lastMoveDirIndex, 0, 7);
				//if (idleDir == 2) idleDir = 1; // right -> forward-right
				//else if (idleDir == 6) idleDir = 7; // left -> forward-left
				Quaternion offsetToUseIdle = Quaternion.Euler(0f, idleYawByDir[idleDir], 0f);
				modelRoot.rotation = legsLook * offsetToUseIdle;
			}
			else
			{
				Quaternion offsetToUseRun = legsOffset;
				modelRoot.rotation = legsLook * offsetToUseRun;
			}
		}

		if (yawTarget != null)
		{
			Vector3 lookAtPoint;
			// komplette LookPoint von pitchTarget (Position + forward * Distanz)
			if (pitchTarget != null)
			{
				lookAtPoint = pitchTarget.position + pitchTarget.forward * 100f;
			}
			else
			{
				lookAtPoint = yawTarget.position + yawTarget.forward * 100f;
			}

			// Calculate movement-based lean angle (10 degrees max)
			Vector2 targetLeanAngles = Vector2.zero;
			if (moveInput.sqrMagnitude > 0.0001f)
			{
				// Target lean based on movement (left/right and forward/backward)
				targetLeanAngles.x = -moveInput.x * rollLeanDegrees; // Roll lean (left/right)
				targetLeanAngles.y = moveInput.y * pitchLeanDegrees;  // Pitch lean (forward/backward)
			}
			// Smooth toward target lean
			float leanT = Mathf.Clamp01(leanSmooth * Time.deltaTime);
			currentLeanAngles = Vector2.Lerp(currentLeanAngles, targetLeanAngles, leanT);

			if (lowerLumbar != null)
			{
				Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - lowerLumbar.position, transform.up);
				// Add lean rotation (roll + pitch) and strafe yaw twist
				float strafeYaw = moveInput.x * strafeYawDegrees;
				Quaternion leanRotation = Quaternion.Euler(currentLeanAngles.y, strafeYaw, currentLeanAngles.x);
				lowerLumbar.rotation = lookRotation * leanRotation * offset;
			}
			if (upperLumbar != null)
			{
				Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - upperLumbar.position, transform.up);
				// Add lean rotation (roll + pitch) and reduced strafe yaw for upper torso
				float strafeYawUpper = moveInput.x * (strafeYawDegrees * 0.6f);
				Quaternion leanRotation = Quaternion.Euler(currentLeanAngles.y * 0.7f, strafeYawUpper, currentLeanAngles.x * 0.7f);
				upperLumbar.rotation = lookRotation * leanRotation * offset;
			}

			/*if (cranium != null)
			{
				// Make head look slightly more straight ahead by blending toward body forward
				Vector3 toLook = (lookAtPoint - cranium.position).normalized;
				Vector3 bodyForward = (modelRoot != null ? modelRoot.forward : transform.forward).normalized;
				Vector3 blendedForward = Vector3.Slerp(toLook, bodyForward, Mathf.Clamp01(headForwardBlend));
				Quaternion lookRotation = Quaternion.LookRotation(blendedForward, transform.up);
				// Subtle lean for head with configurable axis mapping
				float srcRoll = currentLeanAngles.x;  // x = roll
				float srcPitch = currentLeanAngles.y; // y = pitch
				float headPitch = headSwapPitchRoll ? (srcRoll * headPitchMultiplier * headPitchSign) : (srcPitch * headPitchMultiplier * headPitchSign);
				float headRoll  = headSwapPitchRoll ? (srcPitch * headRollMultiplier  * headRollSign)  : (srcRoll  * headRollMultiplier  * headRollSign);
				Quaternion leanRotation = Quaternion.Euler(headPitch, 0f, headRoll);
				cranium.rotation = lookRotation * leanRotation * offset;
			}*/

		}
	}


	/// <summary>
	/// SoF2 PM_Friction equivalent - handles ground and air friction (exact from bg_pmove.c)
	/// </summary>
	private void PM_Friction()
	{
		Vector3 vec = velocity;
		float speed = vec.magnitude;
		float drop = 0f;

		// If walking, ignore slope movement
		if (isGrounded)
		{
			vec.y = 0f; // ignore slope movement
		}

		speed = vec.magnitude;
		if (speed < 1f)
		{
			velocity.x = 0f;
			velocity.z = 0f;        // allow sinking underwater
			return;
		}

		// apply ground friction
		if (!isSwimming)
		{
			if (isGrounded)
			{
				// if getting knocked back, no friction
				// (We could add a knockback flag if needed)
				float control = speed < pm_stopspeed ? pm_stopspeed : speed;
				drop += control * pm_friction * Time.deltaTime;
			}
		}

		// apply water friction even if just wading
		if (isSwimming)
		{
			drop += speed * 3.0f * Time.deltaTime;  // pm_waterfriction = 3.0f
		}

		// Apply the friction
		float newspeed = speed - drop;
		if (newspeed < 0)
			newspeed = 0;

		if (newspeed != speed)
		{
			newspeed /= speed;
			velocity.x *= newspeed;
			velocity.z *= newspeed;
			// Don't apply friction to y-velocity in air
			if (isGrounded)
				velocity.y *= newspeed;
		}
	}

	[SerializeField] private bool testAlternativeAccelaration = false;  // Flag to test alternative acceleration method

	/// <summary>
	/// SoF2 PM_Accelerate equivalent - handles movement acceleration (exact from bg_pmove.c)
	/// </summary>
	private void PM_Accelerate(Vector3 wishdir, float wishspeed, float accel)
	{
		if (!testAlternativeAccelaration)
		{
			// Q2 style (original)
			float currentspeed = Vector3.Dot(velocity, wishdir);
			float addspeed = wishspeed - currentspeed;

			if (addspeed <= 0)
				return;

			float accelspeed = accel * Time.deltaTime * wishspeed;
			if (accelspeed > addspeed)
				accelspeed = addspeed;

			// Apply acceleration to all 3 components (like bg_pmove.c)
			velocity.x += accelspeed * wishdir.x;
			velocity.y += accelspeed * wishdir.y;
			velocity.z += accelspeed * wishdir.z;
		}
		else
		{
			// Alternative way (avoids strafe jump maxspeed bug), but feels bad
			Vector3 wishVelocity = wishdir * wishspeed;
			Vector3 pushDir = wishVelocity - velocity;
			float pushLen = pushDir.magnitude;
			if (pushLen > 0.0001f)  // Avoid division by zero
				pushDir /= pushLen;  // Normalize

			float canPush = accel * Time.deltaTime * wishspeed;
			if (canPush > pushLen)
				canPush = pushLen;

			velocity += pushDir * canPush;
		}
	}

	/// <summary>
	/// SoF2 PM_WalkMove equivalent - handles ground movement
	/// </summary>
	private void PM_WalkMove()
	{
		if (PM_CheckJump())
		{
			PM_AirMove();
			return;
		}

		PM_Friction();

		// Get movement input
		Vector3 forward = (isAiming || isNPC) ? transform.forward : (cameraTransform != null ? cameraTransform.forward : transform.forward);
		Vector3 right = (isAiming || isNPC) ? transform.right : (cameraTransform != null ? cameraTransform.right : transform.right);
		// derive a ground normal via a short down cast to project movement like SoF2
		Vector3 groundNormal = Vector3.up;
		{
			float halfHeight = Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
			Vector3 center = transform.TransformPoint(capsuleCenter);
			Vector3 top = center + Vector3.up * halfHeight;
			Vector3 bottom = center - Vector3.up * halfHeight;
			if (Physics.CapsuleCast(top, bottom, capsuleRadius * 0.9f, Vector3.down, out RaycastHit groundHit, groundCheckDistance + 0.05f, groundMask))
			{
				if (Vector3.Dot(groundHit.normal, Vector3.up) > pm_maxsteepness)
					groundNormal = groundHit.normal;
			}
		}
		// project forward/right onto ground plane (like PM_ClipVelocity on directions)
		forward = Vector3.ProjectOnPlane(forward, Vector3.up); // start flat
		right = Vector3.ProjectOnPlane(right, Vector3.up);
		if (groundNormal != Vector3.up)
		{
			forward = Vector3.ProjectOnPlane(forward, Vector3.Cross(groundNormal, Vector3.Cross(forward, groundNormal))).normalized;
			right = Vector3.ProjectOnPlane(right, Vector3.Cross(groundNormal, Vector3.Cross(right, groundNormal))).normalized;
		}
		forward.y = 0f; right.y = 0f; forward.Normalize(); right.Normalize();

		// Calculate movement direction
		Vector3 wishvel = forward * moveInput.y + right * moveInput.x;
		wishvel.y = 0f; // No vertical movement in walk move

		// Get movement scale
		float scale = PM_CmdScale();

		// Get normalized direction and base speed
		Vector3 wishdir = wishvel.normalized;
		float wishspeed = scale * pm_maxspeed;

		// Clamp to max speed
		if (wishspeed > pm_maxspeed)
		{
			wishspeed = pm_maxspeed;
		}

		// Accelerate
		PM_Accelerate(wishdir, wishspeed, pm_accelerate);
	}

	/// <summary>
	/// SoF2 PM_AirMove equivalent - handles air movement
	/// </summary>
	private void PM_AirMove()
	{
		// Apply friction
		PM_Friction();

		// Get movement input
		Vector3 forward = (isAiming || isNPC) ? transform.forward : (cameraTransform != null ? cameraTransform.forward : transform.forward);
		Vector3 right = (isAiming || isNPC) ? transform.right : (cameraTransform != null ? cameraTransform.right : transform.right);
		forward.y = 0f; right.y = 0f; forward.Normalize(); right.Normalize();

		Vector3 wishvel = forward * moveInput.y + right * moveInput.x;
		wishvel.y = 0f;

		float scale = PM_CmdScale();
		if (scale == 0)
		{
			wishvel = Vector3.zero;
		}
		else
		{
			wishvel *= scale;
		}

		Vector3 wishdir = wishvel.normalized;
		float wishspeed = wishvel.magnitude;

		// Clamp to max speed
		if (wishspeed > pm_maxspeed)
		{
			wishspeed = pm_maxspeed;
		}

		// Accelerate with air acceleration
		PM_Accelerate(wishdir, wishspeed, pm_airaccelerate);
	}

	/// <summary>
	/// SoF2 PM_CheckJump equivalent - handles jumping
	/// </summary>
	private bool PM_CheckJump()
	{
		// If debounce active, no jump
		if (jumpDebounce > 0f)
			return false;

		// If we've already performed a jump this frame, signal caller to switch to air movement
		if (isJumping)
			return true;

		// No queued-jump behaviour in this implementation; jump is handled immediately in input callback
		return false;
	}

	/// <summary>
	/// SoF2 PM_CmdScale equivalent - scales movement input
	/// </summary>
	private float PM_CmdScale()
	{
		// Emulate SoF2 PM_CmdScale with inputs in [-1..1] by mapping to [-127..127]
		float forwardmove = moveInput.y * 127f;
		float rightmove = moveInput.x * 127f;
		float upmove = 0f;
		float max = Mathf.Max(Mathf.Abs(forwardmove), Mathf.Abs(rightmove));
		max = Mathf.Max(max, Mathf.Abs(upmove));
		if (max <= 0.0f)
			return 0.0f;
		float total = Mathf.Sqrt(forwardmove * forwardmove + rightmove * rightmove + upmove * upmove);
		float scale = pm_maxspeed * max / (127.0f * total);
		return scale;
	}

	// Compute movementDir (0..7) like SoF2 PM_SetMovementDir based on move input
	private float ComputeMovementDir(Vector2 move)
	{
		// If no input, return 0
		if (move.sqrMagnitude < 0.0001f)
			return 0f;
		// Convert to SoF2-style forwardmove/rightmove values (-1 to 1)
		float forwardmove = move.y; // W/S keys
		float rightmove = move.x;   // A/D keys

		// Exact SoF2 PM_SetMovementDir logic
		if (rightmove == 0 && forwardmove > 0)
		{
			return 0; // forward 0
		}
		else if (rightmove < 0 && forwardmove > 0)
		{
			return 1; // forward-right 22
		}
		else if (rightmove < 0 && forwardmove == 0)
		{
			return 2; // right 45
		}
		else if (rightmove < 0 && forwardmove < 0)
		{
			return 3; // back-right -22
		}
		else if (rightmove == 0 && forwardmove < 0)
		{
			return 4; // back 0
		}
		else if (rightmove > 0 && forwardmove < 0)
		{
			return 5; // back-left -22
		}
		else if (rightmove > 0 && forwardmove == 0)
		{
			return 6; // left 45
		}
		else if (rightmove > 0 && forwardmove > 0)
		{
			return 7; // forward-left -45
		}

		return 0; // default 0	
	}

	/// <summary>
	/// Apply gravity to velocity
	/// </summary>
	private void ApplyGravity()
	{
		if (!isGrounded)
		{
			velocity.y -= pm_gravity * Time.deltaTime;
		}
		else
		{
			// emulate ground stick like SoF2: small negative to keep contact
			if (velocity.y < 0f) velocity.y = -2f;
			// Reset jumping state when grounded
			if (isJumping)
			{
				isJumping = false;
			}
		}
	}

	/// <summary>
	/// SoF2 PM_ClipVelocity equivalent - slides off impacting surfaces
	/// </summary>
	private void PM_ClipVelocity(Vector3 input, Vector3 normal, out Vector3 output, float overbounce)
	{
		float backoff = Vector3.Dot(input, normal);

		if (backoff < 0)
		{
			backoff *= overbounce;
		}
		else
		{
			backoff /= overbounce;
		}

		output = new Vector3();
		// Apply to all 3 components individually
		output.x = input.x - (normal.x * backoff);
		output.y = input.y - (normal.y * backoff);
		output.z = input.z - (normal.z * backoff);
	}

	/// <summary>
	/// SoF2 PM_StepSlideMove equivalent - handles collision sliding
	/// </summary>
	private void PM_StepSlideMove(bool gravity)
	{
		// Manual capsule move with simple sliding and ground detection
		Vector3 desired = velocity * Time.deltaTime;

		// We'll perform an iterative slide similar to PM_SlideMove but simplified
		int numbumps = 4;
		Vector3 primal_velocity = velocity;
		Vector3 currentPos = transform.position;
		float time_left = 1.0f; // fraction of movement remaining

		// compute capsule top/bottom for casts
		float halfHeight = Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
		Vector3 center = transform.TransformPoint(capsuleCenter);

		Vector3 top = center + Vector3.up * halfHeight;
		Vector3 bottom = center - Vector3.up * halfHeight;

		Vector3 vel = velocity;

		for (int bump = 0; bump < numbumps; bump++)
		{
			Vector3 end = currentPos + vel * Time.deltaTime * time_left;
			Vector3 castDir = end - currentPos;
			float castDist = castDir.magnitude;
			if (castDist < 1e-6f)
			{
				transform.position = currentPos;
				break;
			}

			if (Physics.CapsuleCast(top, bottom, capsuleRadius, castDir.normalized, out RaycastHit hit, castDist, ~0, QueryTriggerInteraction.Ignore))
			{
				// move up to hit
				float moveFraction = hit.distance / castDist;
				currentPos += castDir * moveFraction;
				// save touch entity? (not available here)

				// slide along plane
				Vector3 clipVel;
				PM_ClipVelocity(vel, hit.normal, out clipVel, OVERCLIP);
				vel = clipVel;

				// reduce time left
				time_left -= time_left * moveFraction;
				// if too many planes / stuck, stop
				if (time_left <= 0.001f)
					break;
			}
			else
			{
				// no hit, move entire distance
				currentPos = end;
				break;
			}
		}

		// Apply final position
		transform.position = currentPos;

		// ground check: cast a short distance down to determine if we're on the ground now
		float downDist = 0.2f;
		if (Physics.CapsuleCast(top, bottom, capsuleRadius * 0.9f, Vector3.down, out RaycastHit downHit, downDist, groundMask))
		{
			isGrounded = Vector3.Dot(downHit.normal, Vector3.up) > 0.5f;
			if (isGrounded && velocity.y < 0)
				velocity.y = -2f; // stick to ground
		}
		else
		{
			isGrounded = false;
		}
	}

	/// <summary>
	/// Move the character using the calculated velocity with SoF2 physics
	/// </summary>
	private void MoveCharacter()
	{
		// Apply SoF2 step slide move for proper collision handling
		//Debug.Log($"Before Move - Position: {transform.position}, Velocity: {velocity}");
		PM_StepSlideMove(!isGrounded);
		//Debug.Log($"After Move - Position: {transform.position}, Velocity: {velocity}");
	}


	private void HandleRotation()
	{
		Quaternion targetRot = Quaternion.identity;
		if (isAiming)
		{
			Vector3 lookDir = yawTarget != null ? yawTarget.forward : transform.forward;
			lookDir.y = 0f;
			if (lookDir.sqrMagnitude > 0.01f)
				targetRot = Quaternion.LookRotation(lookDir);
		}
		else
		{
			Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
			if (horizontalVel.sqrMagnitude > 0.001f)
			{
				targetRot = Quaternion.LookRotation(horizontalVel, Vector3.up);
			}
			else
			{
				Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
				camForward.y = 0f;
				if (camForward.sqrMagnitude > 0.01f)
					targetRot = Quaternion.LookRotation(camForward, Vector3.up);
			}
		}

		if (targetRot != Quaternion.identity)
		{
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		// Draw capsule top and bottom based on capsule settings
		float halfHeight = Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
		Vector3 center = transform.TransformPoint(capsuleCenter);
		Vector3 top = center + Vector3.up * halfHeight;
		Vector3 bottom = center - Vector3.up * halfHeight;
		float radius = capsuleRadius * 0.9f;

		Gizmos.color = isGrounded ? Color.green : Color.red;
		Gizmos.DrawWireSphere(top, radius);
		Gizmos.DrawWireSphere(bottom, radius);
		Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
		Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
		Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
		Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
	}
#endif

	private void OnGUI()
	{
		float scaleFactor = Screen.height / 1080f;
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleFactor, scaleFactor, 1f));

		GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 22,
			fontStyle = FontStyle.Bold,
			normal = { textColor = Color.cyan }
		};
		GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 20,
			normal = { textColor = Color.white }
		};

		float x = 10f, y = 10f, line = 24f;
		GUI.Label(new Rect(x, y, 600, line), "=== SoF2 Movement Debug ===", headerStyle); y += line * 1.2f;
		Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);

		// Movement States (vertical layout)
		GUI.Label(new Rect(x, y, 600, line), $"IsGrounded: {isGrounded}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"IsJumping: {isJumping}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"IsSwimming: {isSwimming}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"IsWalking: {isWalking}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"FPS: {(int)(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f))}", valueStyle); y += line;
		y += line * 0.5f; // Spacing

		// Velocity Info
		GUI.Label(new Rect(x, y, 600, line), $"Velocity: {velocity}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Horiz Speed: {horiz.magnitude:F2} / {pm_maxspeed}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Vertical Speed: {velocity.y:F2}", valueStyle); y += line;
		y += line * 0.5f; // Spacing

		// Input Info
		GUI.Label(new Rect(x, y, 600, line), $"MoveInput: {moveInput}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"MovementDir: {lastAngles.movementDir:F1}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Jump Debounce: {jumpDebounce:F2}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Lean: Left={isLeaningLeft} Right={isLeaningRight} Offset={leanOffset}", valueStyle); y += line;
		y += line * 0.5f; // Spacing

		// Physics Settings
		GUI.Label(new Rect(x, y, 600, line), $"Accel: {pm_accelerate}  AirAccel: {pm_airaccelerate}  Friction: {pm_friction}", valueStyle); y += line;

		// BGPlayer Debug Info
		GUI.Label(new Rect(x, y, 600, line), "--- BGPlayer Debug ---", headerStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Legs Euler: {lastAngles.legsAngles}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Lower Torso Euler: {lastAngles.lowerTorsoAngles}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Upper Torso Euler: {lastAngles.upperTorsoAngles}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Head Euler: {lastAngles.headAngles}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"TorsoYaw deg: {lastTorsoYawDeg:F1}  normalized: {lastTorsoYawNormalized:F2}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Head Pitch/Yaw: {lastHeadPitch:F1} / {lastHeadYaw:F1}", valueStyle); y += line;
	}
}
