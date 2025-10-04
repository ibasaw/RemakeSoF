using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera aimCam;
    [SerializeField] private CinemachineCamera firstPersonCam;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MyPlayerControllerCustom player;

    [SerializeField] private bool startFirstPerson = true;

    private bool isFirstPerson = false;
    private AvatarActions inputActions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
	{
        inputActions = new AvatarActions();
        
        // Set initial camera based on startFirstPerson setting
        isFirstPerson = startFirstPerson;
        SetCameraMode(isFirstPerson);
    }

    void OnEnable(){
        inputActions.Enable();
        inputActions.Player.SwitchCamera.performed += OnCameraSwitched;
    }

    void OnDisable(){
        inputActions.Player.SwitchCamera.performed -= OnCameraSwitched;
        inputActions.Disable();
    }

    void OnCameraSwitched(InputAction.CallbackContext ctx)
    {
        // Toggle between first person and third person
        isFirstPerson = !isFirstPerson;
        SetCameraMode(isFirstPerson);
        
        Debug.Log($"Camera Switched to: {(isFirstPerson ? "First Person" : "Third Person")}");
    }
    
    private void SetCameraMode(bool firstPerson)
    {
        if (firstPerson)
        {
            // Switch to first person camera
            firstPersonCam.Priority = 20;
            aimCam.Priority = 10;
        }
        else
        {
            // Switch to third person camera
            aimCam.Priority = 20;
            firstPersonCam.Priority = 10;
        }
    }
}
