using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;


public class MyPlayerControllerCustom : MonoBehaviour
{
	private Animator animator;

	// Input System
	private AvatarActions inputActions;
	private Vector2 moveInput;

	[Header("SoF2 Movement Settings")]
	[SerializeField] private bool testAlternativeAccelaration = false;  // Flag to test alternative acceleration method
	// All layers or just "Ground" layer groundCheckDistance inside colliderSystem
	[SerializeField] public LayerMask groundMask = ~0; 

	[Header("Movement Limits")]
	[SerializeField] private bool drawPhysicsDebugGUI = false;
	[SerializeField] private float pm_maxsteepness = 0.7f;      // maximum floor steepness (lower = steeper slopes allowed)
	[SerializeField] private float pm_maxstep = 18.0f;          // Maximum step height
	[SerializeField] private float pm_stepsize = 18.0f;         // Step size
	[SerializeField] private float pm_maxbarrier = 32.0f;       // maximum barrier height

	[Header("Speed Scales")]
	[SerializeField] private float pm_duckScale = 0.25f;        // Speed scale when ducking

	[Header("Physics Constants")]
	[SerializeField] private float pm_accelerate = 6.0f;       // Ground acceleration (SoF2 default)
	[SerializeField] private float pm_airaccelerate = 1.0f;     // Air acceleration  

	[SerializeField] private float pm_friction = 6.0f;          // Ground friction
	[SerializeField] private float pm_stopspeed = 100.0f;       // Stop speed threshold
	[SerializeField] private float pm_maxspeed = 280.0f;        // Maximum speed / g_speed

	[SerializeField] private float phys_maxvelocity = 320;  // Maximum overall horizontal velocity (SoF2 default)
	[SerializeField] private float phys_maxwalkvelocity = 320;  // Maximum walk horizontal velocity (SoF2 default) 
	[SerializeField] private float phys_maxcrouchvelocity = 100.0f;  // Maximum crouch horizontal velocity
	
	[SerializeField] private float pm_gravity = 800.0f;         // Gravity value g_gravity

	[SerializeField] private float jumpVelocity = 270.0f;       // Jump velocity (from phys_jumpvel)
	[SerializeField] private float rotationSpeed = 14f;         // Rotation speed for character
	[SerializeField] private float totalAirTimeGlitchSuspicion = 0.05f; // reduced to from 0.1 to 0.05f

	// SoF2 Physics Constants
	private const float OVERCLIP = 1.001f;                     // Overclip constant for sliding

	[Header("Camera/Rotation")]
	[SerializeField] private Transform yawTarget;
	[SerializeField] private Transform pitchTarget;
	[SerializeField] private Transform cameraTransform;
	[SerializeField] private float standYawTargetY = 85f;
	[SerializeField] private float crouchYawTargetY = 45f;
	[SerializeField] public bool isAiming = false;
	[SerializeField] public bool isNPC = false;

	[Header("BGPlayer Bones")]
	[SerializeField] private Transform lowerLumbar;
	[SerializeField] private Transform upperLumbar;
	[SerializeField] private Transform cranium;
	[SerializeField] private Transform modelRoot;
	[SerializeField] private Transform pelvis;

	[SerializeField] private Transform rightHandBolt;
	[SerializeField] private Transform leftHandBolt;
	[SerializeField] private GameObject startWeaponPrefab;
	[SerializeField] private float startWeaponZOverride = -90f;
	[SerializeField] private float startWeaponScaleOverride = 0.01f;


	// SoF2 Movement State
	private Vector3 velocity = Vector3.zero;           // Current velocity (x, y, z)
	private bool isGrounded = false;                   // Grounded state
	private bool isWalking = false;                    // Ground walking state
	private bool isWalkingPressed = false;              // Ground walking pressed state (shift-button at default)
	private bool isJumping = false;                    // Jumping state
	private bool isAttacking = false;				   // Attacking state
	private bool landedThisGround = false;             // Ensures landing sound/log fire once per ground contact
	private bool wasGroundedPrev = false;              // Previous grounded state for transitions
	private float nonJumpAirTime = 0f;                 // Airtime when falling without an explicit jump
	private Vector3 nonJumpStartPosition = Vector3.zero; // Start position when leaving ground (no jump)
	private float nonJumpStartY = 0f;                  // Start height when leaving ground (no jump)
	private Vector3 lastGroundedPosition = Vector3.zero; // Last known grounded position
	private bool hasValidGroundedPosition = false; // Track if we have a valid grounded position
	private float lastStepUpTime = 0f; // Time when last step-up occurred
	private bool isSwimming = false;                   // Swimming state
	private bool isCrouching = false;                  // Crouching state
	private float jumpDebounce = 0f;                   // Jump debounce timer (starts after landing)
	private bool isDebounceActive = false;             // Whether debounce is currently active
	private float airTime = 0f;                        // Time spent in air
	private float lastJumpTime = 0f;                   // Time when last jump started
	private Vector3 jumpStartPosition = Vector3.zero;  // Position when jump started
	private float jumpDistance = 0f;                   // Horizontal distance traveled during jump
	private float jumpHeight = 0f;                     // Maximum height reached during jump
	private float jumpStartY = 0f;                     // Y position when jump started
	private float landingY = 0f;                       // Y position when landing
	[SerializeField] private float stepUpHeightThreshold = 5.0f; // Minimum height difference to consider as step-up
	[SerializeField] private float jumpDebounceAfterMs = 0.25f; // 250ms debounce like SoF2
	[SerializeField] private bool autoJump = false; // Auto jump when grounded and space is pressed

	// class fields
	private Vector3 lastMoveDirection = Vector3.forward; // merkt sich die letzte NonZero-Richtung
	[SerializeField] public float standCameraInfluence = 0.0f; // 0 = in Stand niemals zur Kamera drehen, 0.05-0.2 = langsam nachziehen
	[SerializeField] private float legsYawOffsetDegrees = 90f; // legs offset so left foot leads slightly

	[Header("legs Idle Facing Offsets (by movement dir 0..7)")]
	[SerializeField] private float[] idleYawByDir = new float[8] { 112f, 45f, 68f, 68f, 112f, 180f, 180f, 90f };

	[Header("Animator Smoothing")]
	[SerializeField] private float animParamSmooth = 10f; // higher = faster response
	private float animHorizontal = 0f;
	private float animVertical = 0f;

	[Header("Lumbar Yaw Offsets")]
	[SerializeField] private float upperLumbarYawOffset = -5f;    // Upper lumbar yaw offset in degrees
	[SerializeField] private float lowerLumbarYawOffset = 0f;    // Lower lumbar yaw offset in degrees
	[SerializeField] private float lumbarYawSmooth = 8f;          // Smoothing speed for lumbar yaw changes

	[Header("Lumbar Pitch Offsets")]
	[SerializeField] private float upperLumbarPitchOffset = 0f;    // Upper lumbar pitch offset in degrees (forward/backward)
	[SerializeField] private float lowerLumbarPitchOffset = 0f;   // Lower lumbar pitch offset in degrees (forward/backward)
	[SerializeField] private float lumbarPitchSmooth = 8f;        // Smoothing speed for lumbar pitch changes

	[Header("Movement Direction upperLumbar Idle Offsets")]
	[SerializeField] private int[] movementOffsets = new int[8] { 0, 22, 45, -22, 0, 22, -45, -22 }; //evtl. erst mal alle 0
	[SerializeField] private float movementOffsetSmooth = 6f;     // Smoothing speed for movement-based offsets

	[Header("Torso-Legs Follow")]
	[SerializeField] private float torsoFollowYawInfluence = 65f; // legs yaw catch-up when torso twists
	[SerializeField] private float strafeYawDegrees = 12f; // strafe yaw twist magnitude
	[SerializeField] private float baseLegsRotationSmooth = 8f;   // Base smoothing speed for legs rotation

	[Header("Dynamic Pelvis(legs) Follow")]
	[SerializeField] private float maxLegsRotationSmooth = 20f;   // Maximum smoothing speed when mouse moves fast
	[SerializeField] private float mouseSpeedMultiplier = 2f;     // How much mouse speed affects smoothing
	[SerializeField] private float mouseSpeedSmooth = 10f;        // Smoothing for mouse speed calculation

	// Smoothed legs forward to avoid snapping/jitter
	private Vector3 smoothedLegsForward = Vector3.forward;
	private int lastMoveDirIndex = 0;

	// Lean state
	private bool isLeaningLeft = false;
	private bool isLeaningRight = false;
	private int leanOffset = 0; // -30 for left, +30 for right, 0 for none
	private List<string> touchedObjects = new List<string>();

	[Header("Lean Settings")]
	[SerializeField] private bool debugInputInfo = false;
	[SerializeField] private float rollLeanDegrees = 25;  	// left/right roll lean magnitude
	[SerializeField] private float pitchLeanDegrees = 20; 	// forward/backward pitch lean magnitude
	[SerializeField] private float leanSmooth = 8f;        	// smoothing speed for lean interpolation

	// Smoothed lean state
	private Vector2 currentLeanAngles = Vector2.zero; // x = roll, y = pitch

	// Smoothed lumbar yaw offsets
	private float currentUpperLumbarYaw = 0f;
	private float currentLowerLumbarYaw = 0f;

	// Smoothed lumbar pitch offsets
	private float currentUpperLumbarPitch = 0f;
	private float currentLowerLumbarPitch = 0f;

	// Movement-based idle offset
	private float currentMovementIdleOffset = 0f;
	
	// Collider position tracking for accurate airtime/height calculations
	private Vector3 jumpStartColliderPosition;

	// Mouse speed detection for dynamic pelvisTarget follow (using Input System)
	private Vector2 lookInput = Vector2.zero;
	private float currentMouseSpeed = 0f;
	private float smoothedMouseSpeed = 0f;
	private float dynamicLegsRotationSmooth = 8f;

	// Sound system
	private PlayerSoundSystem soundSystem;
	private RaycastHit lastGroundHit; // Store last ground hit info for sound system

	private MyPlayerColliderSystem colliderSystem;

	private void Awake()
	{
		// no CharacterController - using custom capsule-based physics
		inputActions = new AvatarActions();
		animator = GetComponentInChildren<Animator>();
		soundSystem = GetComponent<PlayerSoundSystem>();
		if (soundSystem == null)
			soundSystem = gameObject.AddComponent<PlayerSoundSystem>();

		colliderSystem = GetComponent<MyPlayerColliderSystem>();
		if (colliderSystem == null)
			colliderSystem = gameObject.AddComponent<MyPlayerColliderSystem>();

		// Initialize smoothed legs forward with current facing
		Vector3 initialForward = transform.forward;
		initialForward.y = 0f;
		if (initialForward.sqrMagnitude < 0.0001f)
			initialForward = Vector3.forward;
		smoothedLegsForward = initialForward.normalized;

		// Initialize landing sound system
		soundSystem.InitializeSoundSystem();

		// Attach start weapon to right hand bolt when player spawns
		AttachStartWeapon();
		
		colliderSystem.InitializeVisualCollider();
		colliderSystem.InitializeVisualGroundCheck();
		colliderSystem.CalculateAutoCapsuleSize(cranium, pelvis, leftHandBolt, rightHandBolt);
		
		// Initialize grounded position
		lastGroundedPosition = GetColliderBottomPosition();
		hasValidGroundedPosition = true;
	}

	private void OnEnable()
	{
		if (isNPC) return;
		inputActions.Enable();
		inputActions.Player.Move.performed += OnMovePerformed;
		inputActions.Player.Move.canceled += OnMoveCanceled;
		inputActions.Player.Walk.performed += OnWalkPressed;
		inputActions.Player.Walk.canceled += OnWalkCanceled;
		inputActions.Player.Jump.performed += OnJumpPerformed;
		inputActions.Player.Crouch.performed += OnCrouchPerformed;
		inputActions.Player.Crouch.canceled += OnCrouchCanceled;
		inputActions.Player.LeanLeft.performed += OnLeanLeftPerformed;
		inputActions.Player.LeanLeft.canceled += OnLeanLeftCanceled;
		inputActions.Player.LeanRight.performed += OnLeanRightPerformed;
		inputActions.Player.LeanRight.canceled += OnLeanRightCanceled;
		inputActions.Player.Look.performed += OnLookPerformed;
		inputActions.Player.Look.canceled += OnLookCanceled;
		inputActions.Player.Attack.performed += OnAttack;
		inputActions.Player.Attack.canceled += OnCancelAttack;
	}

	private void OnDisable()
	{
		if (isNPC) return;
		inputActions.Player.Move.performed -= OnMovePerformed;
		inputActions.Player.Move.canceled -= OnMoveCanceled;
		inputActions.Player.Walk.performed -= OnWalkPressed;
		inputActions.Player.Walk.canceled -= OnWalkCanceled;
		inputActions.Player.Jump.performed -= OnJumpPerformed;
		inputActions.Player.Look.performed -= OnLookPerformed;
		inputActions.Player.Look.canceled -= OnLookCanceled;
		inputActions.Player.Crouch.performed -= OnCrouchPerformed;
		inputActions.Player.Crouch.canceled -= OnCrouchCanceled;
		inputActions.Player.LeanLeft.performed -= OnLeanLeftPerformed;	
		inputActions.Player.LeanLeft.canceled -= OnLeanLeftCanceled;
		inputActions.Player.LeanRight.performed -= OnLeanRightPerformed;
		inputActions.Player.LeanRight.canceled -= OnLeanRightCanceled;
		inputActions.Player.Attack.performed -= OnAttack;
		inputActions.Player.Attack.canceled -= OnCancelAttack;
		inputActions.Disable();
	}

	private void OnWalkPressed(InputAction.CallbackContext ctx)
	{
		Debug.Log("shift-Walk Pressed");
		isWalkingPressed = true;
		// Animator parameter will be set in Update() for consistent timing
	}

	private void OnWalkCanceled(InputAction.CallbackContext ctx)
	{
		Debug.Log("shift-Walk Released");
		isWalkingPressed = false;
		// Animator parameter will be set in Update() for consistent timing
	}

	private void OnAttack(InputAction.CallbackContext ctx)
	{
		Debug.Log("Attack Pressed");
		isAttacking = true;
		// Animator parameter will be set in Update() for consistent timing
	}

	private void OnCancelAttack(InputAction.CallbackContext ctx)
	{
		Debug.Log("Attack Released");
		isAttacking = false;
		// Animator parameter will be set in Update() for consistent timing
	}

	private void OnCrouchPerformed(InputAction.CallbackContext ctx)
	{
		Debug.Log("C Pressed");
		isCrouching = true;
		// Animator parameter will be set in Update() for consistent timing
		if (yawTarget != null)
		{
			Vector3 localPos = yawTarget.localPosition;
			localPos.y = crouchYawTargetY;
			yawTarget.localPosition = localPos;
		}
		
		// Update capsule size for crouching
		colliderSystem.UpdateCapsuleSizeForState(isCrouching);
	}
	private void OnCrouchCanceled(InputAction.CallbackContext ctx)
	{
		Debug.Log("C Released");
		isCrouching = false;
		// Animator parameter will be set in Update() for consistent timing
		if (yawTarget != null)
		{
			Vector3 localPos = yawTarget.localPosition;
			localPos.y = standYawTargetY;
			yawTarget.localPosition = localPos;
		}
		
		// Update capsule size for standing
		colliderSystem.UpdateCapsuleSizeForState(isCrouching);
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

	private void OnLookPerformed(InputAction.CallbackContext ctx)
	{
		lookInput = ctx.ReadValue<Vector2>();
	}

	private void OnLookCanceled(InputAction.CallbackContext ctx)
	{
		lookInput = Vector2.zero;
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

	private void OnMovePerformed(InputAction.CallbackContext ctx)
	{
		moveInput = ctx.ReadValue<Vector2>();

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

	/// <summary>
	/// Auto-jump for testing debounce system - jumps continuously while space is held
	/// </summary>
	private void HandleAutoJump()
	{
		// Check if jump input is currently being held down
		bool jumpInputHeld = inputActions.Player.Jump.ReadValue<float>() > 0f;

		if (jumpInputHeld)
		{
			// Try to jump (will be blocked by debounce/grounded checks)
			TryJump();
		}
	}

	// Try to perform a jump immediately (removes queued-jump logic)
	private void TryJump()
	{
		// Can't jump if debounce active (now only active after landing)
		if (isDebounceActive)
			return;

		// Must be grounded to initiate a jump
		if (!isGrounded)
			return;

		// If already jumping, ignore
		if (isJumping)
			return;

		// Perform jump now
		isJumping = true;
		isDebounceActive = false; // Reset debounce state (will be activated after landing)
		velocity.y = jumpVelocity;
		lastJumpTime = Time.time;
		airTime = 0f;
		jumpStartColliderPosition = GetColliderBottomPosition();
		jumpStartPosition = jumpStartColliderPosition;
		jumpStartY = jumpStartColliderPosition.y;
		jumpDistance = 0f;
		jumpHeight = 0f;

		// Trigger jump animation (Trigger resets automatically after one frame)
		animator?.SetTrigger("Jump");
		//Debug.Log("Jump performed - Starting airtime, distance and height tracking");
	}

	/// <summary>
	/// Unified ground check method used by both CheckGrounded and PM_StepSlideMove
	/// </summary>
	private bool CheckGroundedAtPosition(Vector3 position, out RaycastHit groundHit)
	{
		groundHit = new RaycastHit();
		
		// Jump grace period - don't detect ground for a short time after jumping
		if (isJumping && (Time.time - lastJumpTime) < 0.1f)
		{
			return false;
		}

		// Don't detect ground if still moving upward significantly
		if (isJumping && velocity.y > 50f)
		{
			return false;
		}

		// Calculate capsule points at the given position using unified world center calculation
		float halfHeight = Mathf.Max(0, (colliderSystem.GetCurrentCapsuleHeight() * 0.5f) - colliderSystem.GetCurrentCapsuleRadius());
		Vector3 center = GetWorldCenterAtPosition(position);
		Vector3 top = center + transform.up * halfHeight;
		Vector3 bottom = center - transform.up * halfHeight;

		// Check by casting slightly down from current position with dynamic distance
		float dynamicCastDistance = colliderSystem.GetCurrentGroundCheckDistance() + 0.01f + Mathf.Abs(velocity.y) * Time.fixedDeltaTime;
		if (Physics.CapsuleCast(top, bottom, colliderSystem.GetCurrentCapsuleRadius() * 0.9f, Vector3.down, out RaycastHit hit, dynamicCastDistance, groundMask, QueryTriggerInteraction.Ignore))
		{
			// Consider grounded if the normal is reasonably upwards (handle both cos and angle values)
			float slopeThreshold = pm_maxsteepness > 1f ? Mathf.Cos(pm_maxsteepness * Mathf.Deg2Rad) : pm_maxsteepness;
			if (Vector3.Dot(hit.normal, Vector3.up) > slopeThreshold)
			{
				// Debug unexpected ground detection during jumping
				if (isJumping && velocity.y > 10f) // Still going up
				{
					Debug.LogWarning($"Prevented ground detection! Y-vel: {velocity.y:F2}, Hit distance: {hit.distance:F3}, Time since jump: {(Time.time - lastJumpTime):F3}s");
					return false;
				}
				// Store ground hit for sound system
				groundHit = hit;
				lastGroundHit = hit;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Check if grounded at current transform position
	/// </summary>
	private bool CheckGrounded()
	{
		return CheckGroundedAtPosition(transform.position, out RaycastHit hit);
	}
	
	/// <summary>
	/// Get world center position at given position (handles scale correctly)
	/// </summary>
	private Vector3 GetWorldCenterAtPosition(Vector3 position)
	{
		// Use TransformPoint to handle scale correctly, then adjust for position delta
		Vector3 baseCenter = transform.TransformPoint(colliderSystem.GetCurrentCapsuleCenter());
		Vector3 positionDelta = position - transform.position;
		return baseCenter + positionDelta;
	}
	
	/// <summary>
	/// Get the current collider bottom position for accurate calculations
	/// </summary>
	private Vector3 GetColliderBottomPosition()
	{
		float halfHeight = Mathf.Max(0, (colliderSystem.GetCurrentCapsuleHeight() * 0.5f) - colliderSystem.GetCurrentCapsuleRadius());
		Vector3 center = GetWorldCenterAtPosition(transform.position);
		return center - transform.up * halfHeight;
	}

	private void Update()
	{
		// Calculate mouse speed for dynamic pelvisTarget follow using Input System
		currentMouseSpeed = lookInput.magnitude / Time.deltaTime; // degrees per second

		// Smooth mouse speed
		float mouseSpeedSmoothT = Mathf.Clamp01(mouseSpeedSmooth * Time.deltaTime);
		smoothedMouseSpeed = Mathf.Lerp(smoothedMouseSpeed, currentMouseSpeed, mouseSpeedSmoothT);

		// Calculate dynamic legs rotation smooth based on mouse speed
		float speedFactor = Mathf.Clamp01(smoothedMouseSpeed / 100f); // Normalize to 0-1 range (100 degrees/s = max)
		dynamicLegsRotationSmooth = Mathf.Lerp(baseLegsRotationSmooth, maxLegsRotationSmooth, speedFactor * mouseSpeedMultiplier);

		// jump debounce (only runs after landing)
		if (isDebounceActive && jumpDebounce > 0f)
		{
			jumpDebounce -= Time.deltaTime;
			if (jumpDebounce <= 0f)
			{
				isDebounceActive = false; // Debounce finished, can jump again
			}
		}

		// Auto-jump for testing debounce system
		if (autoJump && !isNPC)
		{
			HandleAutoJump();
		}

		// Play weapon swing sounds - play next sound when current one finishes
		soundSystem.HandleWeaponSoundEvents(isAttacking, "Knife", "swing");

		// Check for swimming (simple water detection)
		isSwimming = transform.position.y < 0f; // Assuming water level is at y=0

        // Rotate is handled in LateUpdate for consistency with final physics state
		
		// Update visual collider
		colliderSystem.UpdateVisualCollider(isGrounded);
		
		// Update visual ground check
		colliderSystem.UpdateVisualGroundCheck(isGrounded);

		// Update all animator parameters centrally for consistent timing
		UpdateAnimatorParameters();
	}
	
	/// <summary>
	/// FixedUpdate for physics operations - called at consistent intervals
	/// </summary>
	private void FixedUpdate()
	{
		// Store previous grounded state for edge detection
		bool wasGrounded = wasGroundedPrev;
		
		// Apply gravity (pure physics)
		ApplyGravity();
		
		// Move character (physics-based movement)
		MoveCharacter();
		
		// Check grounded state AFTER movement for accurate detection
		isGrounded = CheckGrounded();
		
		// Additional ground check for better detection on slopes
		if (!isGrounded)
		{
			// Try a more aggressive ground check for slopes
			float halfHeight = Mathf.Max(0, (colliderSystem.GetCurrentCapsuleHeight() * 0.5f) - colliderSystem.GetCurrentCapsuleRadius());
			Vector3 center = GetWorldCenterAtPosition(transform.position);
			Vector3 top = center + transform.up * halfHeight;
			Vector3 bottom = center - transform.up * halfHeight;
			
			// Check with larger distance for slopes
			float slopeCheckDistance = colliderSystem.GetCurrentGroundCheckDistance() * 2f;
			if (Physics.CapsuleCast(top, bottom, colliderSystem.GetCurrentCapsuleRadius() * 0.9f, Vector3.down, out RaycastHit slopeHit, slopeCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
			{
				float slopeThreshold = pm_maxsteepness > 1f ? Mathf.Cos(pm_maxsteepness * Mathf.Deg2Rad) : pm_maxsteepness;
				if (Vector3.Dot(slopeHit.normal, Vector3.up) > slopeThreshold)
				{
					isGrounded = true;
					lastGroundHit = slopeHit;
				}
			}
		}
		
		// Update grounded position AFTER ground check
		if (isGrounded)
		{
			lastGroundedPosition = GetColliderBottomPosition();
			hasValidGroundedPosition = true;
		}
		
		// NOW handle non-jump airtime with CORRECT grounded state
		if (!isGrounded)
		{
			if (wasGrounded && !isJumping)
			{
				// Add a small grace period to prevent false airtime detection after landing
				// This helps with the issue where ground detection briefly fails after landing on elevated surfaces
				if (nonJumpAirTime == 0f) // Only start airtime tracking if not already started
				{
					// Use the last known grounded position if available, otherwise use current position
					if (hasValidGroundedPosition)
					{
						nonJumpStartPosition = lastGroundedPosition;
						nonJumpStartY = nonJumpStartPosition.y;
					}
					else
					{
						// Fallback: use current position
						nonJumpStartPosition = GetColliderBottomPosition();
						nonJumpStartY = nonJumpStartPosition.y;
					}
					// Debug the start position
					Debug.Log($"Non-jump airtime started - Start Y: {nonJumpStartY:F2}, Position: {nonJumpStartPosition}, Valid: {hasValidGroundedPosition}");
					landedThisGround = false;
				}
			}
			nonJumpAirTime += Time.fixedDeltaTime;
		}
		
		// Edge detection for landings (more robust than velocity.y <= 0f)
		bool justLanded = !wasGrounded && isGrounded;
		
		// Update grounded state
		wasGroundedPrev = isGrounded;
		
		// Set animator grounded state AFTER final ground check
		animator?.SetBool("IsGrounded", isGrounded);
		
		// Choose movement mode based on grounded state
		if (isGrounded) PM_WalkMove();
		else PM_AirMove();
		
		// Handle landing events AFTER movement and ground check
		HandleLandingEvents(justLanded);
		
		// Handle footstep sound events AFTER movement (uses final ground state)
		soundSystem.HandleFootstepSoundEvents(isWalking, lastGroundHit);
	}

    private void LateUpdate()
	{
		// The skeleton has an offset rotation.
		// We apply a counter-rotation to correct the orientation.
		// You mentioned it was on the Y axis.
		Quaternion offset = Quaternion.Euler(0, 90, 0);
		Quaternion legsOffset = Quaternion.Euler(0, legsYawOffsetDegrees, 0);
	
		// Rotate pelvisTarget (legs) to face movement input direction (W/A/S/D)
        if (pelvis != null)
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
					float t = Mathf.Clamp01(dynamicLegsRotationSmooth * Time.deltaTime);
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
				// Apply dynamic smoothing to torso follow as well
				torsoDrivenT *= (dynamicLegsRotationSmooth / baseLegsRotationSmooth);
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
				Quaternion offsetToUseIdle = Quaternion.Euler(0f, idleYawByDir[idleDir], 0f);
				pelvis.rotation = legsLook * offsetToUseIdle;
			}
			else
			{
				Quaternion offsetToUseRun = legsOffset;
				pelvis.rotation = legsLook * offsetToUseRun;
			}
		}

        // Apply character root rotation after physics and before GUI
        HandleRotation();

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

			// Smooth lumbar yaw offsets
			float yawSmoothT = Mathf.Clamp01(lumbarYawSmooth * Time.deltaTime);
			currentUpperLumbarYaw = Mathf.Lerp(currentUpperLumbarYaw, upperLumbarYawOffset, yawSmoothT);
			currentLowerLumbarYaw = Mathf.Lerp(currentLowerLumbarYaw, lowerLumbarYawOffset, yawSmoothT);

			// Smooth lumbar pitch offsets
			float pitchSmoothT = Mathf.Clamp01(lumbarPitchSmooth * Time.deltaTime);
			currentUpperLumbarPitch = Mathf.Lerp(currentUpperLumbarPitch, upperLumbarPitchOffset, pitchSmoothT);
			currentLowerLumbarPitch = Mathf.Lerp(currentLowerLumbarPitch, lowerLumbarPitchOffset, pitchSmoothT);

			// Calculate movement-based idle offset
			bool hasInput = moveInput.sqrMagnitude > 0.0001f;
			float targetMovementOffset = 0f;
			if (!hasInput && lastMoveDirIndex >= 0 && lastMoveDirIndex < movementOffsets.Length)
			{
				targetMovementOffset = movementOffsets[lastMoveDirIndex];
			}

			// Smooth movement-based offset
			float movementSmoothT = Mathf.Clamp01(movementOffsetSmooth * Time.deltaTime);
			currentMovementIdleOffset = Mathf.Lerp(currentMovementIdleOffset, targetMovementOffset, movementSmoothT);

			if (lowerLumbar != null)
			{
				Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - lowerLumbar.position, transform.up);
				// Add lean rotation (roll + pitch), strafe yaw twist, lumbar yaw offset, and lumbar pitch offset
				float strafeYaw = moveInput.x * strafeYawDegrees;
				Quaternion leanRotation = Quaternion.Euler(currentLeanAngles.y + currentLowerLumbarPitch, strafeYaw + currentLowerLumbarYaw, currentLeanAngles.x);
				lowerLumbar.rotation = lookRotation * leanRotation * offset;
			}
			if (upperLumbar != null)
			{
				Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - upperLumbar.position, transform.up);
				// Add lean rotation (roll + pitch), reduced strafe yaw, lumbar yaw offset, lumbar pitch offset, and movement-based idle offset for upper torso
				float strafeYawUpper = moveInput.x * (strafeYawDegrees * 0.6f);
				float totalYawOffset = strafeYawUpper + currentUpperLumbarYaw + currentMovementIdleOffset;
				Quaternion leanRotation = Quaternion.Euler(currentLeanAngles.y * 0.7f + currentUpperLumbarPitch, totalYawOffset, currentLeanAngles.x * 0.7f);
				upperLumbar.rotation = lookRotation * leanRotation * offset;
			}

		}

		if(isCrouching){
			if (modelRoot != null)
			{
				//Debug.Log($"modelRoot position while crouching: {modelRoot.position}");
			}
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
				drop += control * pm_friction * Time.fixedDeltaTime;
				
				// Additional friction for slopes to prevent sliding
				if (lastGroundHit.collider != null)
				{
					float slopeDot = Vector3.Dot(lastGroundHit.normal, Vector3.up);
					if (slopeDot < 0.9f) // On any slope
					{
						drop += control * pm_friction * 1.0f * Time.fixedDeltaTime; // Extra friction on slopes
						
						// Even more friction on steep slopes
						if (slopeDot < 0.7f) // Steep slope
						{
							drop += control * pm_friction * 1.5f * Time.fixedDeltaTime; // Heavy friction on steep slopes
						}
					}
				}
			}
		}

		// apply water friction even if just wading
		if (isSwimming)
		{
			drop += speed * 3.0f * Time.fixedDeltaTime;  // pm_waterfriction = 3.0f
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

			float accelspeed = accel * Time.fixedDeltaTime * wishspeed;
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

			float canPush = accel * Time.fixedDeltaTime * wishspeed;
			if (canPush > pushLen)
				canPush = pushLen;

			velocity += pushDir * canPush;
		}
	}

	/// <summary>
	/// Apply SoF2-style velocity limits (phys_maxvelocity, phys_maxwalkvelocity)
	/// </summary>
	private void ApplyVelocityLimits()
	{
		// Get horizontal velocity
		Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
		float horizontalSpeed = horizontalVel.magnitude;
		
		// Determine max velocity based on state
		float maxVelocity;
		if (isGrounded)
		{
			// Use walk velocity limit when grounded
			maxVelocity = phys_maxwalkvelocity;
		}
		else
		{
			// Use overall velocity limit when in air
			maxVelocity = phys_maxvelocity;
		}
		
		// Apply horizontal velocity limit
		if (horizontalSpeed > maxVelocity)
		{
			float scale = maxVelocity / horizontalSpeed;
			velocity.x *= scale;
			velocity.z *= scale;
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
		
		// Get ground normal for slope movement
		Vector3 groundNormal = Vector3.up;
		if (lastGroundHit.collider != null && Vector3.Dot(lastGroundHit.normal, Vector3.up) > pm_maxsteepness)
		{
			groundNormal = lastGroundHit.normal;
			// Debug ground normal for diagonal movement (reduced spam)
			//if (moveInput.sqrMagnitude > 0.1f && Time.time % 1f < 0.1f) // Only log once per second
			//{
			//	Debug.Log($"Ground Normal: {groundNormal}, Dot: {Vector3.Dot(lastGroundHit.normal, Vector3.up):F3}, MoveInput: {moveInput}");
			//}
		}
		
		// Project movement directions onto ground plane (simplified and more reliable)
		forward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
		right = Vector3.ProjectOnPlane(right, groundNormal).normalized;

		// Calculate movement direction using SoF2-style input scaling (like PM_AirMove)
		float fmove = moveInput.y * 127f;
		float smove = moveInput.x * 127f;
		Vector3 wishvel = forward * fmove + right * smove;
		// Only project onto ground plane if we have significant input
		if (wishvel.sqrMagnitude > 0.01f)
		{
			wishvel = Vector3.ProjectOnPlane(wishvel, groundNormal);
		}

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

		// Debug movement for consistency check
		//if (moveInput.sqrMagnitude > 0.1f && Time.time % 1f < 0.1f) // Log once per second
		//{
		//	Debug.Log($"Movement Debug - Input: {moveInput}, Scale: {scale:F3}, WishSpeed: {wishspeed:F2}, WishDir: {wishdir}");
		//}
		
		// Accelerate with consistent acceleration
		PM_Accelerate(wishdir, wishspeed, pm_accelerate);
		
		// Apply SoF2-style velocity limits
		ApplyVelocityLimits();
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

		// Use raw input values like SoF2 (fmove, smove are -127 to +127)
		float fmove = moveInput.y * 127f;
		float smove = moveInput.x * 127f;

		Vector3 wishvel = forward * fmove + right * smove;
		wishvel.y = 0f;

		float scale = PM_CmdScale();

		// Copy wishvel to wishdir and normalize (like original SoF2)
		Vector3 wishdir = wishvel;
		float wishspeed = wishdir.magnitude;

		if (wishspeed > 0.0001f)
		{
			wishdir /= wishspeed; // Normalize
		}
		else
		{
			wishdir = Vector3.zero;
			wishspeed = 0f;
		}

		// Apply scale AFTER normalization (like original SoF2)
		wishspeed *= scale;

		// Clamp to max speed
		if (wishspeed > pm_maxspeed)
		{
			wishspeed = pm_maxspeed;
		}

		// Accelerate with air acceleration
		PM_Accelerate(wishdir, wishspeed, pm_airaccelerate);
		
		// Apply SoF2-style velocity limits
		ApplyVelocityLimits();
	}

	/// <summary>
	/// SoF2 PM_CheckJump equivalent - handles jumping
	/// </summary>
	private bool PM_CheckJump()
	{
		// If debounce active, no jump
		if (isDebounceActive)
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
		// Simplified scale calculation for consistent acceleration
		// Use input magnitude directly instead of complex SoF2 scaling
		float inputMagnitude = moveInput.magnitude;
		if (inputMagnitude <= 0.0f)
			return 0.0f;
		
		// Return normalized scale (0.0 to 1.0) for consistent acceleration
		return Mathf.Clamp01(inputMagnitude);
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
	/// Apply gravity to velocity (pure physics, no landing logic)
	/// </summary>
	private void ApplyGravity()
	{
		if (!isGrounded)
		{
			velocity.y -= pm_gravity * Time.fixedDeltaTime;
			// We are airborne again -> allow next landing trigger
			landedThisGround = false;
			// Track airtime, distance and height while in air
			if (isJumping)
			{
				airTime = Time.time - lastJumpTime;
				// Calculate horizontal distance from jump start position using collider position
				Vector3 currentColliderPos = GetColliderBottomPosition();
				Vector3 horizontalDiff = new Vector3(currentColliderPos.x - jumpStartPosition.x, 0f, currentColliderPos.z - jumpStartPosition.z);
				jumpDistance = horizontalDiff.magnitude;
				// Track maximum height reached using collider position
				float currentHeight = currentColliderPos.y - jumpStartY;
				if (currentHeight > jumpHeight)
				{
					jumpHeight = currentHeight;
				}
			}
		}
		else
		{
			// emulate ground stick like SoF2: small negative to keep contact
			if (velocity.y < 0f) velocity.y = -2f;
		}
	}
	
	/// <summary>
	/// Handle landing events and state transitions (called after movement)
	/// </summary>
	private void HandleLandingEvents(bool justLanded)
	{
		// Get current landing position
		Vector3 currentColliderPos = GetColliderBottomPosition();
		landingY = currentColliderPos.y;
		
		// PRIORITY 1: Handle jump landings first (explicit jumps take precedence)
		if (isJumping && isGrounded)
		{
			float totalAirTime = Time.time - lastJumpTime;
			
			// Calculate final jump distance using collider position
			Vector3 horizontalDiff = new Vector3(currentColliderPos.x - jumpStartPosition.x, 0f, currentColliderPos.z - jumpStartPosition.z);
			float finalJumpDistance = horizontalDiff.magnitude;
			
			// Calculate final jump height - use the maximum of tracked height or current height difference
			float currentHeightFromStart = currentColliderPos.y - jumpStartY;
			float finalJumpHeight = Mathf.Max(jumpHeight, currentHeightFromStart);
			
			Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
			float heightDifference = landingY - jumpStartY;
			bool landedHigher = heightDifference > stepUpHeightThreshold;
			
			// Only process if we haven't already processed this landing
			if (!landedThisGround)
			{
				// Play landing sound only if not higher
				if (!landedHigher)
				{
					soundSystem.PlayLandingSound(lastGroundHit);
				}

				if (landedHigher)
				{
					Debug.Log($"Landing detected (jump) [UP] - Airtime: {totalAirTime:F3}s, Distance: {finalJumpDistance:F2}u, Height: {finalJumpHeight:F2}u, Start Y: {jumpStartY:F2}, Landing Y: {landingY:F2}, Height Diff: {heightDifference:F3} (Threshold: {stepUpHeightThreshold:F1})");
					// Reset jump debounce immediately for step-up success
					jumpDebounce = 0f;
					isDebounceActive = false;
				}
				else
				{
					string heightChange = heightDifference > 0 ? "UP" : heightDifference < 0 ? "DOWN" : "SAME";
					Debug.Log($"Landing detected (jump) [{heightChange}] - Airtime: {totalAirTime:F3}s, Distance: {finalJumpDistance:F2}u, Height: {finalJumpHeight:F2}u, Horiz Speed: {horiz.magnitude:F2}u, Vertical Speed: {velocity.y:F2}u, Start Y: {jumpStartY:F2}, Landing Y: {landingY:F2}, Height Diff: {heightDifference:F3} (Threshold: {stepUpHeightThreshold:F1})");
					// Start debounce timer AFTER landing (only if not higher)
					jumpDebounce = jumpDebounceAfterMs;
					isDebounceActive = true;
				}
				landedThisGround = true;
			}

			// Only warn about suspicious landings for very short airtime (likely glitches)
			if (totalAirTime < totalAirTimeGlitchSuspicion)
			{
				Debug.LogWarning($"SUSPICIOUS LANDING - Very short airtime: {totalAirTime:F3}s, Distance: {finalJumpDistance:F2} units, Height: {finalJumpHeight:F2} units. Horiz Speed: {horiz.magnitude:F2}, Vertical Speed: {velocity.y:F2}u");
			}

			// Reset jumping state and update UI values
			isJumping = false;
			airTime = 0f;
			jumpDistance = finalJumpDistance;
			jumpHeight = finalJumpHeight; // Update with final calculated height
			
			// Reset non-jump tracking since we processed a jump landing
			nonJumpAirTime = 0f;
			nonJumpStartY = 0f;
			nonJumpStartPosition = Vector3.zero;
		}
		// PRIORITY 2: Handle non-jump landings (falls, drops, etc.) - only if not jumping
		else if (isGrounded && nonJumpAirTime > 0f && !landedThisGround && !isJumping)
		{
			// Non-jump landing: use edge detection for more robust landing detection
			// Only process if we have a valid start position (not 0,0,0) and no recent step-up
			bool recentStepUp = (Time.time - lastStepUpTime) < 0.5f; // Ignore landings within 0.5s of step-up
			// Ignore very short airtime periods that are likely caused by ground detection glitches
			bool significantAirtime = nonJumpAirTime > 0.1f; // Back to 0.1f for proper tracking
			
			if (nonJumpStartY != 0f && !recentStepUp && significantAirtime)
			{
				// Calculate height difference
				float heightDifference = landingY - nonJumpStartY;
				
				// Only play landing sound if we landed lower (fell down)
				if (heightDifference < 0f)
				{
					soundSystem.PlayLandingSound(lastGroundHit);
				}
				
				// Log with non-jump airtime/distance/height using collider position
				Vector3 horizontalDiff = new Vector3(currentColliderPos.x - nonJumpStartPosition.x, 0f, currentColliderPos.z - nonJumpStartPosition.z);
				float finalDistance = horizontalDiff.magnitude;
				float finalHeight = Mathf.Abs(heightDifference); // Absolute height for display
				Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
				string heightChange = heightDifference > 0 ? "UP" : heightDifference < 0 ? "DOWN" : "SAME";
				Debug.Log($"Landing detected (no jump) [{heightChange}] - Airtime: {nonJumpAirTime:F3}s, Distance: {finalDistance:F2}u, Height: {finalHeight:F2}u, Horiz Speed: {horiz.magnitude:F2}u, Vertical Speed: {velocity.y:F2}u, Start Y: {nonJumpStartY:F2}, Landing Y: {landingY:F2}, Height Diff: {heightDifference:F3} (Threshold: {stepUpHeightThreshold:F1})");
				
				// Reset non-jump tracking
				nonJumpAirTime = 0f;
				nonJumpStartY = 0f;
				nonJumpStartPosition = Vector3.zero;
				landedThisGround = true;
			}
			else
			{
				// Debug why we're not processing this landing
				string reason = nonJumpStartY == 0f ? "Start Y is 0" : 
								recentStepUp ? "Recent step-up" : 
								"Airtime too short";
				Debug.Log($"Skipping no-jump landing - {reason}, Valid: {hasValidGroundedPosition}, Position: {lastGroundedPosition}, Step-up time: {(Time.time - lastStepUpTime):F2}s, Airtime: {nonJumpAirTime:F3}s");
				
				// Reset tracking even when skipping to prepare for next fall
				nonJumpAirTime = 0f;
				nonJumpStartY = 0f;
				nonJumpStartPosition = Vector3.zero;
			}
		}
	}
	
	/// <summary>
	/// Update all animator parameters centrally for consistent timing
	/// </summary>
	private void UpdateAnimatorParameters()
	{
		if (animator == null) return;
		
		// Movement-based parameters
		Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
		bool isMoving = horizontalVel.sqrMagnitude > 0.001f;
		animator.SetBool("IsMoving", isMoving);
		animator.SetFloat("Speed", horizontalVel.magnitude);
		
		// Input-based parameters with smoothing
		float animT = Mathf.Clamp01(animParamSmooth * Time.deltaTime);
		animHorizontal = Mathf.Lerp(animHorizontal, moveInput.x, animT);
		animVertical = Mathf.Lerp(animVertical, moveInput.y, animT);
		animator.SetFloat("Horizontal", animHorizontal);
		animator.SetFloat("Vertical", animVertical);
		
		// State-based parameters
		animator.SetBool("IsWalking", isWalkingPressed);
		animator.SetBool("IsAttacking", isAttacking);
		animator.SetBool("IsCrouching", isCrouching);
		
		// Ground state (set in FixedUpdate after final ground check)
		// animator.SetBool("IsGrounded", isGrounded); // Already set in FixedUpdate
		
		// Set walking state based on input (like SoF2)
		isWalking = isGrounded && (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f);
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
	/// SoF2 PM_StepSlideMove equivalent - handles collision sliding with step-up detection
	/// </summary>
	private void PM_StepSlideMove(bool gravity)
	{
		touchedObjects.Clear();
		const float SKIN_WIDTH = 0.01f; // Abstand vor der Oberfläche
		Vector3 desired = velocity * Time.fixedDeltaTime;	

		int numbumps = 4;
		Vector3 primal_velocity = velocity;
		Vector3 currentPos = transform.position;
		float time_left = 1.0f;

		// Lokale Angaben beibehalten (colliderSystem.GetCurrentCapsuleCenter() ist in lokalen Koordinaten)
		float halfHeightLocal = Mathf.Max(0f, (colliderSystem.GetCurrentCapsuleHeight() * 0.5f) - colliderSystem.GetCurrentCapsuleRadius());

		Vector3 vel = velocity;

		for (int bump = 0; bump < numbumps; bump++)
		{
			// Berechne world-space center / top / bottom basierend auf currentPos (wichtig!)
			Vector3 worldCenter = GetWorldCenterAtPosition(currentPos);
			Vector3 top = worldCenter + transform.up * halfHeightLocal;
			Vector3 bottom = worldCenter - transform.up * halfHeightLocal;

			Vector3 end = currentPos + vel * Time.fixedDeltaTime * time_left;
			Vector3 castDir = end - currentPos;
			float castDist = castDir.magnitude;

			if (castDist < 1e-6f)
			{
				transform.position = currentPos;
				break;
			}

			Vector3 castDirNorm = castDir / castDist;

			// Bewegungscast - use all layers for collision detection to prevent falling through
			if (Physics.CapsuleCast(top, bottom, colliderSystem.GetCurrentCapsuleRadius(), castDirNorm, out RaycastHit hit, castDist + SKIN_WIDTH, ~0, QueryTriggerInteraction.Ignore))
			{
				if (!touchedObjects.Contains(hit.collider.name))
				{
					touchedObjects.Add(hit.collider.name);
				}
				
		// Check if this is a step-up opportunity (only for horizontal obstacles, not slopes)
		if (TryStepUp(currentPos, hit, out Vector3 stepUpPos))
		{
			// Successfully stepped up
			currentPos = stepUpPos;
			// Continue with original movement after step-up
			Vector3 remainingMovement = vel * Time.fixedDeltaTime * time_left;
			remainingMovement.y = 0f; // Don't apply vertical velocity after step-up
			currentPos += remainingMovement;
			break;
		}
				
				// Bewege nur bis kurz vor den Hit (skin width), damit wir nicht "in" die Geometrie landen
				float moveDist = Mathf.Max(hit.distance - SKIN_WIDTH, 0f);
				currentPos += castDirNorm * moveDist;

				// Debug: wer wird getroffen und wie weit waren wir von ihm entfernt
				//Debug.Log($"Movement Hit: {hit.collider.name}, hitDist={hit.distance:F4}, moveDist={moveDist:F4}, bump={bump}");

				// Slide entlang der Fläche
				Vector3 clipVel;
				PM_ClipVelocity(vel, hit.normal, out clipVel, OVERCLIP);
				vel = clipVel;

				// Zeit reduzieren (proportional zur Strecke)
				float fraction = (moveDist / castDist);
				time_left -= time_left * fraction;

				if (time_left <= 0.001f)
					break;
			}
			else
			{
				// kein Treffer -> komplette Strecke gehen
				currentPos = end;
				break;
			}
		}

		// Endposition setzen
		transform.position = currentPos;

		// --- Finaler Ground-Check: verwende einheitliche CheckGroundedAtPosition Methode ---
		// Store the previous grounded state to avoid overriding a correct ground detection
		bool wasGroundedBeforeMove = isGrounded;
		bool newGroundCheck = CheckGroundedAtPosition(currentPos, out RaycastHit downHit);
		
		// Enhanced ground detection for slopes and diagonal surfaces
		if (!newGroundCheck && wasGroundedBeforeMove)
		{
			// Try a more aggressive ground check for slopes
			float halfHeight = Mathf.Max(0, (colliderSystem.GetCurrentCapsuleHeight() * 0.5f) - colliderSystem.GetCurrentCapsuleRadius());
			Vector3 center = GetWorldCenterAtPosition(currentPos);
			Vector3 top = center + transform.up * halfHeight;
			Vector3 bottom = center - transform.up * halfHeight;
			
			// Check with larger distance for slopes
			float slopeCheckDistance = colliderSystem.GetCurrentGroundCheckDistance() * 3f; // Increased from 2f to 3f
			if (Physics.CapsuleCast(top, bottom, colliderSystem.GetCurrentCapsuleRadius() * 0.9f, Vector3.down, out RaycastHit slopeHit, slopeCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
			{
				float slopeThreshold = pm_maxsteepness > 1f ? Mathf.Cos(pm_maxsteepness * Mathf.Deg2Rad) : pm_maxsteepness;
				if (Vector3.Dot(slopeHit.normal, Vector3.up) > slopeThreshold)
				{
					newGroundCheck = true;
					downHit = slopeHit;
				}
			}
		}
		
		// Only update isGrounded if we weren't already grounded, or if the new check confirms we're grounded
		// This prevents overriding a correct ground detection with a false negative
		if (!wasGroundedBeforeMove || newGroundCheck)
		{
			isGrounded = newGroundCheck;
		}
		
		if (isGrounded && velocity.y < 0)
		{
			// Strong ground stick - prevent falling through and sliding
			velocity.y = -2f; // stick to ground
			
			// Enhanced ground stick for slopes - prevent sliding down
			if (downHit.collider != null)
			{
				float slopeDot = Vector3.Dot(downHit.normal, Vector3.up);
				if (slopeDot < 0.95f) // On any slope
				{
					// Project velocity onto ground plane to prevent sliding
					Vector3 groundProjectedVel = Vector3.ProjectOnPlane(velocity, downHit.normal);
					velocity = groundProjectedVel;
					velocity.y = -2f; // Keep ground stick
					
					// Additional velocity dampening on steep slopes
					if (slopeDot < 0.7f) // Steep slope
					{
						velocity *= 0.8f; // Reduce velocity by 20% on steep slopes
						velocity.y = -2f; // Keep ground stick
					}
				}
			}
		}
		// Note: Non-jump airtime tracking is handled in the main FixedUpdate loop
		// to avoid conflicts and ensure proper initialization
	}
	
	/// <summary>
	/// Try to step up over an obstacle (SoF2 step-up logic)
	/// </summary>
	private bool TryStepUp(Vector3 currentPos, RaycastHit hit, out Vector3 stepUpPos)
	{
		stepUpPos = currentPos;
		
		// Only try step-up if we're grounded and moving horizontally
		if (!isGrounded || Mathf.Abs(velocity.y) > 10f)
			return false;
			
		// Check if the hit normal is roughly horizontal (not a ceiling or slope)
		// For slopes, we want to slide along them, not step up
		if (Vector3.Dot(hit.normal, Vector3.up) < 0.3f)
			return false;
			
		// Check if this is actually a vertical obstacle, not a slope
		// If the normal is too close to vertical, it's likely a slope, not a step
		if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
			return false;
			
		// Check if the obstacle height is within step-up range
		float obstacleHeight = hit.point.y - currentPos.y;
		if (obstacleHeight > pm_maxstep || obstacleHeight < 0.1f)
			return false;
			
		// Calculate step-up position
		Vector3 stepUpTarget = currentPos + Vector3.up * (obstacleHeight + pm_stepsize);
		
		// Check if there's space above the step
		float halfHeight = Mathf.Max(0, (colliderSystem.GetCurrentCapsuleHeight() * 0.5f) - colliderSystem.GetCurrentCapsuleRadius());
		Vector3 worldCenter = GetWorldCenterAtPosition(stepUpTarget);
		Vector3 top = worldCenter + transform.up * halfHeight;
		Vector3 bottom = worldCenter - transform.up * halfHeight;
		
		// Check for ceiling collision at step-up position
		if (Physics.CapsuleCast(bottom, top, colliderSystem.GetCurrentCapsuleRadius(), Vector3.up, out RaycastHit ceilingHit, pm_stepsize, groundMask, QueryTriggerInteraction.Ignore))
		{
			// Not enough headroom
			return false;
		}
		
		// Check if the step-up position is clear
		Vector3 stepUpCenter = GetWorldCenterAtPosition(stepUpTarget);
		Vector3 stepUpTop = stepUpCenter + transform.up * halfHeight;
		Vector3 stepUpBottom = stepUpCenter - transform.up * halfHeight;
		
		// Check for horizontal obstacles at step-up position
		Vector3 horizontalCheck = stepUpTarget - currentPos;
		horizontalCheck.y = 0f;
		if (horizontalCheck.magnitude > 0.1f)
		{
			Vector3 horizontalDir = horizontalCheck.normalized;
			if (Physics.CapsuleCast(stepUpBottom, stepUpTop, colliderSystem.GetCurrentCapsuleRadius(), horizontalDir, out RaycastHit horizontalHit, horizontalCheck.magnitude, groundMask, QueryTriggerInteraction.Ignore))
			{
				// There's still an obstacle at the step-up position
				return false;
			}
		}
		
		// Step-up is possible
		stepUpPos = stepUpTarget;
		lastStepUpTime = Time.time;
		Debug.Log($"Step-up successful! Height: {obstacleHeight:F2}, Target: {stepUpTarget}");
		// Note: Step-up successful sound is handled in HandleLandingEvents based on height comparison
		return true;
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
            // Prefer input-driven desired direction to avoid physics timing jitter
            bool hasInput = moveInput.sqrMagnitude > 0.0001f;
            if (hasInput)
            {
                Vector3 desired = lastMoveDirection;
                desired.y = 0f;
                if (desired.sqrMagnitude > 0.0001f)
                    targetRot = Quaternion.LookRotation(desired.normalized, Vector3.up);
            }
            else
            {
                // When no input, align to camera forward
                Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                camForward.y = 0f;
                if (camForward.sqrMagnitude > 0.01f)
                    targetRot = Quaternion.LookRotation(camForward.normalized, Vector3.up);
            }
		}

        if (targetRot != Quaternion.identity)
        {
            // Rotate only the visual model to avoid dragging the camera (yaw/pitch) with player facing
            Transform rotTarget = modelRoot != null ? modelRoot : transform;
            rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
	}

	/// <summary>
	/// Attach the start weapon to the right hand bolt when player spawns
	/// </summary>
	private void AttachStartWeapon()
	{
		if (rightHandBolt == null)
		{
			Debug.LogWarning("Right hand bolt is not assigned in the inspector!");
			return;
		}

		// Instantiate from prefab (no existing Transform reference)
		if (startWeaponPrefab != null)
		{
			GameObject weaponInstance = Instantiate(startWeaponPrefab, rightHandBolt);
			Transform weaponTransform = weaponInstance.transform;
			weaponTransform.localPosition = Vector3.zero;
			// Apply Z rotation override to fix SoF2 weapon axis issues
			weaponTransform.localRotation = Quaternion.Euler(0f, 0f, startWeaponZOverride);
			// Apply scale override to fix SoF2 weapon size issues
			weaponTransform.localScale = Vector3.one * startWeaponScaleOverride;
			Debug.Log($"Instantiated and attached start weapon prefab '{startWeaponPrefab.name}' to right hand bolt '{rightHandBolt.name}' with Z-rotation: {startWeaponZOverride}° and scale: {startWeaponScaleOverride}");
		}
		else
		{
			Debug.LogWarning("No startWeaponPrefab set in the inspector!");
		}
	}

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
		GUI.Label(new Rect(x, y, 600, line), $"FPS: {(int)(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f))}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"IsGrounded: {isGrounded}, IsWalking: {isWalking}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"IsJumping: {isJumping}, IsSwimming: {isSwimming}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"IsAttacking: {isAttacking}, IsCrouching: {isCrouching}", valueStyle); y += line;
		// Airtime display with color coding
		GUIStyle airtimeStyle = new GUIStyle(valueStyle);
		if (isJumping)
		{
			airtimeStyle.normal.textColor = Color.yellow; // Yellow while jumping
		}
		else if (airTime > 0f)
		{
			airtimeStyle.normal.textColor = Color.green; // Green for last recorded airtime
		}
		GUI.Label(new Rect(x, y, 600, line), $"Airtime: {(isJumping ? airTime : 0f):F3}s", airtimeStyle); y += line;

		// Jump distance display with color coding
		GUIStyle distanceStyle = new GUIStyle(valueStyle);
		if (isJumping)
		{
			distanceStyle.normal.textColor = Color.yellow; // Yellow while jumping
		}
		else if (jumpDistance > 0f)
		{
			distanceStyle.normal.textColor = Color.cyan; // Cyan for last recorded distance
		}
		GUI.Label(new Rect(x, y, 600, line), $"Jump Distance: {jumpDistance:F2} units", distanceStyle); y += line;

		// Jump height display with color coding
		GUIStyle heightStyle = new GUIStyle(valueStyle);
		if (isJumping)
		{
			heightStyle.normal.textColor = Color.yellow; // Yellow while jumping
		}
		else if (jumpHeight > 0f)
		{
			heightStyle.normal.textColor = Color.magenta; // Magenta for last recorded height
		}
		GUI.Label(new Rect(x, y, 600, line), $"Jump Height: {jumpHeight:F2} units", heightStyle); y += line;
		
		// Landing height display
		GUIStyle landingStyle = new GUIStyle(valueStyle);
		float heightDiff = landingY - jumpStartY;
		if (heightDiff > stepUpHeightThreshold)
		{
			landingStyle.normal.textColor = Color.green; // Green when landed significantly higher
		}
		else if (heightDiff > 0f)
		{
			landingStyle.normal.textColor = Color.yellow; // Yellow when slightly higher (below threshold)
		}
		else
		{
			landingStyle.normal.textColor = Color.white; // White when normal landing
		}
		GUI.Label(new Rect(x, y, 600, line), $"Landing Y: {landingY:F2} (Start: {jumpStartY:F2}, Diff: {heightDiff:F3}, Threshold: {stepUpHeightThreshold:F1})", landingStyle); y += line;

		// Jump debounce display with color coding
		GUIStyle debounceStyle = new GUIStyle(valueStyle);
		if (isDebounceActive && jumpDebounce > 0f)
		{
			debounceStyle.normal.textColor = Color.red; // Red while debounce is active
		}
		else
		{
			debounceStyle.normal.textColor = Color.green; // Green when can jump
		}
		string debounceText = isDebounceActive ? $"Jump Debounce: {jumpDebounce:F2}s" : "Jump Ready";
		GUI.Label(new Rect(x, y, 600, line), debounceText, debounceStyle); y += line;

		// Auto-jump status display
		if (autoJump)
		{
			GUIStyle autoJumpStyle = new GUIStyle(valueStyle);
			bool jumpInputHeld = !isNPC && inputActions.Player.Jump.ReadValue<float>() > 0f;
			autoJumpStyle.normal.textColor = jumpInputHeld ? Color.yellow : Color.gray;
			string autoJumpText = jumpInputHeld ? "Auto-Jump: ACTIVE (Space Held)" : "Auto-Jump: Enabled (Press & Hold Space)";
			GUI.Label(new Rect(x, y, 600, line), autoJumpText, autoJumpStyle); y += line;
		}
		y += line * 0.5f; // Spacing

		// Velocity Info
		GUI.Label(new Rect(x, y, 600, line), $"Velocity: {velocity}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Horiz Speed: {horiz.magnitude:F2} / {pm_maxspeed}", valueStyle); y += line;
		GUI.Label(new Rect(x, y, 600, line), $"Vertical Speed: {velocity.y:F2}", valueStyle); y += line;
		y += line * 0.5f; // Spacing

		// Input Info
		if(debugInputInfo){
			GUI.Label(new Rect(x, y, 600, line), $"MoveInput: {moveInput}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Lean: Left={isLeaningLeft} Right={isLeaningRight} Offset={leanOffset}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Lumbar Yaw: Upper={currentUpperLumbarYaw:F1}° Lower={currentLowerLumbarYaw:F1}°", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Lumbar Pitch: Upper={currentUpperLumbarPitch:F1}° Lower={currentLowerLumbarPitch:F1}°", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Movement Dir: {lastMoveDirIndex} Idle Offset: {currentMovementIdleOffset:F1}°", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Look Speed: {smoothedMouseSpeed:F1}°/s Legs Smooth: {dynamicLegsRotationSmooth:F1}", valueStyle); y += line;
			y += line * 0.5f; // Spacing
		}

		// Touched Objects Debug
		GUI.Label(new Rect(x, y, 600, line), "Last Touched Objects:", headerStyle); y += line * 1.2f;
		if (touchedObjects.Count > 0)
		{
			foreach (string objName in touchedObjects)
			{
				GUI.Label(new Rect(x, y, 600, line), $"- {objName}", valueStyle); y += line;
			}
		}
		else
		{
			GUI.Label(new Rect(x, y, 600, line), "- None", valueStyle); y += line;
		}
		y += line * 0.5f; // Spacing

		if (lastGroundHit.collider != null && soundSystem != null){
			GUI.Label(new Rect(x, y, 600, line), $"Last Ground Material Hit: {soundSystem.GetGroundMaterialType(lastGroundHit)}", valueStyle); y += line;
			y += line * 0.5f; // Spacing
		}

		// Physics Settings
		if (drawPhysicsDebugGUI)
		{
			GUI.Label(new Rect(x, y, 600, line), $"Accel: {pm_accelerate}  AirAccel: {pm_airaccelerate}  Friction: {pm_friction}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Step: Max={pm_maxstep}  Size={pm_stepsize}  Barrier={pm_maxbarrier}", valueStyle); y += line;
		}
	}
}

