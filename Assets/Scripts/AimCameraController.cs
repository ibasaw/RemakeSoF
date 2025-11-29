using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimCameraController : MonoBehaviour
{
    [SerializeField] private Transform yawTarget;
    [SerializeField] private Transform pitchTarget;

    [SerializeField] private InputActionReference lookInput;
    [SerializeField] private InputActionReference switchShouldInput;

    [SerializeField] private float mouseSensitivity = 0.05f;
    [SerializeField] private float gamepadSensitivity = 0.5f;
    [SerializeField] private float sensitivity = 1.5f;

    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    [SerializeField] private CinemachineThirdPersonFollow aimCam;

    [SerializeField] private float shoulderSwitchSpeed = 5f;

    private float yaw;
    private float pitch;
    private float targetCameraSide;
    
    // Accumulated look input between FixedUpdate calls
    private Vector2 accumulatedLookInput = Vector2.zero;

    private void Awake()
    {
        aimCam = GetComponent<CinemachineThirdPersonFollow>();
        targetCameraSide = aimCam.CameraSide;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3 angles = yawTarget.rotation.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        lookInput.asset.Enable();
    }

    private void OnEnable()
    {
        switchShouldInput.action.Enable();
        switchShouldInput.action.performed += OnSwitchShoulder;
    }

    private void OnDisable()
    {
        switchShouldInput.action.Disable();
        switchShouldInput.action.performed -= OnSwitchShoulder;
    }

    private void OnSwitchShoulder(InputAction.CallbackContext context)
    {
        targetCameraSide = aimCam.CameraSide < 0.5f ? 1f : 0f;
    }

    // Collect input in Update (runs every frame, captures all input)
    void Update()
    {
        Vector2 look = lookInput.action.ReadValue<Vector2>();

        if (Mouse.current != null && Mouse.current.delta.IsActuated())
        {
            // Mouse.delta is already a delta value (movement per frame)
            // Accumulate it directly
            accumulatedLookInput += look * mouseSensitivity;
        }
        else if (Gamepad.current != null && Gamepad.current.rightStick.IsActuated())
        {
            // Gamepad input is typically a rate (per second), so scale by deltaTime
            accumulatedLookInput += look * gamepadSensitivity * Time.deltaTime;
        }
        
        // Update shoulder switch in Update (visual, not physics-critical)
        aimCam.CameraSide = Mathf.Lerp(aimCam.CameraSide, targetCameraSide, Time.deltaTime * shoulderSwitchSpeed);
    }
    
    // Apply rotation in FixedUpdate to sync with physics movement
    // This ensures camera rotation happens at the same rate as movement calculations
    // and prevents stuttering when rotating while moving
    void FixedUpdate()
    {
        // Apply accumulated input
        if (accumulatedLookInput.sqrMagnitude > 0.0001f)
        {
            // Apply rotation with sensitivity
            yaw += accumulatedLookInput.x * sensitivity;
            pitch -= accumulatedLookInput.y * sensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            
            // Reset accumulated input
            accumulatedLookInput = Vector2.zero;
        }
        
        // Always update yawTarget and pitchTarget rotation, even if no input
        // This ensures the rotation is always current for movement calculations
        yawTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void SetYawPitchFromCameraForward(Transform cameraTransform)
    {
        Vector3 flatForward = cameraTransform.forward;
        flatForward.y = 0;

        if (flatForward.sqrMagnitude < 0.001f)
            return;

        yaw = Quaternion.LookRotation(flatForward).eulerAngles.y;

        yawTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchTarget.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }
}
