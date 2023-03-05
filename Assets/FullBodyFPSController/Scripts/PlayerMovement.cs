using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;
using Input = UnityEngine.Input;

public class PlayerMovement : MonoBehaviour
{

    private CharacterController characterController;
    private InputManager inputManager;
    private bool hasAnimator;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float speedChangeRate; // in seconds
    [SerializeField] private float crouchHeight;
    [SerializeField] private float normalHeight;
    [SerializeField] private bool useRootMotion;
    [Space(10)]

    [Header("Ground Check Parameters")]
    [SerializeField] private float GroundCheckOffset = 0.1f;
    [SerializeField] private LayerMask GroundLayers;
    [SerializeField] private float GroundCheckRadius = 0.25f;
    [SerializeField] private Transform rightFeet;
    [SerializeField] private Transform leftFeet;
    [SerializeField] private bool Grounded;

    [Space(10)]

    [Header("Physics Parameters")]
    [SerializeField] private float gravity = -15.0f;
    private float terminalVelocity = 53f; // in air

    [Space(10)]

    [Header("Jump Parameters")]
    [SerializeField] private float jumpHeight;
    [SerializeField] private float JumpTimeout = 0.50f;
    [SerializeField] private float FallTimeout = 0.15f;
    private float jumpTimeoutDelta;
    private float fallTimeoutDelta;

    [Space(10)]

    [Header("Camera Parameters")]
    [SerializeField] private float RotationSpeed = 20;
    [SerializeField] private float BottomClamp = -90;
    [SerializeField] private float TopClamp = 90;
    [SerializeField] private GameObject FPSCameraRoot;
    [SerializeField] private GameObject ThirdPersonCameraRoot;
    [SerializeField] private CinemachineVirtualCamera PlayerCamera;
    [SerializeField] private Transform HeadPosition;
    private float CinemachineTargetPitch;
    private float RotationVelocity;

    [Space(10)]

    [Header("Animation Parameters")]
    [SerializeField] private GameObject model;
    [SerializeField] private float blendSpeed;
    private Animator animator;
    private float animationBlendSpeed;
    // Hashes
    private int X_VelocityHash;
    private int Y_VelocityHash;
    private int CrouchHash;
    private int JumpHash;
    private int GroundedHash;
    private int FallHash;

    private float instantSpeed;
    private float targetSpeed;
    private float currentHorizontalSpeed;
    private float verticalVelocity;
    private Vector2 currentVelocity;
    private Vector3 inputDirection;



    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        characterController = GetComponent<CharacterController>();
        inputManager = GetComponent<InputManager>();
        hasAnimator = model.TryGetComponent(out animator);

        X_VelocityHash = Animator.StringToHash("X_Velocity");
        Y_VelocityHash = Animator.StringToHash("Y_Velocity");
        CrouchHash = Animator.StringToHash("Crouch");
        JumpHash = Animator.StringToHash("Jump");
        GroundedHash = Animator.StringToHash("Grounded");
        FallHash = Animator.StringToHash("Fall");

        jumpTimeoutDelta = JumpTimeout;
        fallTimeoutDelta = FallTimeout;

    }

    void Update()
    {
        ApplyGravity();
        GroundCheck();
        if (!useRootMotion) { Move(); }
        Jump();
        ChangeCamera();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void CameraRotation()
    {
        //if (hasAnimator) return;

        var Mouse_X = inputManager.look.x;
        var Mouse_Y = inputManager.look.y;


        float deltaTimeMultiplier = Time.smoothDeltaTime;
            
        CinemachineTargetPitch -= Mouse_Y * RotationSpeed * deltaTimeMultiplier;
        RotationVelocity = Mouse_X * RotationSpeed * deltaTimeMultiplier;

        CinemachineTargetPitch = ClampAngle(CinemachineTargetPitch, BottomClamp, TopClamp);
        FPSCameraRoot.transform.localRotation = Quaternion.Euler(CinemachineTargetPitch, 0, 0);
        
        FPSCameraRoot.transform.position = HeadPosition.position;


    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }


    private void Move()
    {
        float deltaTimeMultiplier = Time.deltaTime;

        Vector2 MoveInput = inputManager.move.normalized;
        float MoveX = MoveInput.x; // -1 or 1
        float MoveZ = MoveInput.y; // -1 or 1

        targetSpeed = inputManager.run ? runSpeed : walkSpeed;
        
        if (inputManager.crouch) { targetSpeed = crouchSpeed; }

        inputDirection = (transform.right * MoveX + transform.forward * MoveZ).normalized;

        if (MoveInput == Vector2.zero) {
            DOTween.To(() => targetSpeed, x => targetSpeed = x, 0, blendSpeed);
            inputDirection = Vector3.zero;
        }
        
        if (!Grounded || targetSpeed < 0.001f) { targetSpeed = 0; }

        currentHorizontalSpeed = new Vector3(characterController.velocity.x, 0.0f, characterController.velocity.z).magnitude;

        float speedOffset = 0.1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {

            instantSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * MoveInput.magnitude, deltaTimeMultiplier * speedChangeRate);
            //DOTween.To(() => instantSpeed, x => instantSpeed = x, targetSpeed * MoveInput.magnitude, blendSpeed);

            instantSpeed = Mathf.Round(instantSpeed * 1000f) / 1000f;
        }
        else
        {
            DOTween.To(() => instantSpeed, x => instantSpeed = x, targetSpeed, blendSpeed);
        }

        characterController.Move(inputDirection * (instantSpeed * deltaTimeMultiplier) + new Vector3(0.0f, verticalVelocity, 0.0f) * deltaTimeMultiplier);
        transform.rotation = transform.rotation * Quaternion.Euler(0.0f, RotationVelocity, 0.0f);

        currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetSpeed * MoveX, deltaTimeMultiplier * speedChangeRate);
        currentVelocity.y = Mathf.Lerp(currentVelocity.y, targetSpeed * MoveZ, deltaTimeMultiplier * speedChangeRate);

        //DOTween.To(() => currentVelocity.x, x => currentVelocity.x = x, targetSpeed * MoveX, blendSpeed);
        //DOTween.To(() => currentVelocity.y, x => currentVelocity.y = x, targetSpeed * MoveZ, blendSpeed);

        if (Mathf.Abs(currentVelocity.x) < 0.001) { currentVelocity.x = 0; }
        if (Mathf.Abs(currentVelocity.y) < 0.001) { currentVelocity.y = 0; }

        animator.SetFloat(X_VelocityHash, currentVelocity.x);
        animator.SetFloat(Y_VelocityHash, currentVelocity.y);


    }

   

    private void Jump()
    {

        if (Grounded)
        {
            // reset the fall timeout timer
            fallTimeoutDelta = FallTimeout;

            

            // stop our velocity dropping infinitely when grounded
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            // Jump
            if (inputManager.jump && jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
            }

            // jump timeout
            if (jumpTimeoutDelta >= 0.0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }

        }
        else
        {
            // reset the jump timeout timer
            jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                
            }

            // if we are not grounded, do not jump
            inputManager.jump = false;
        }

    }

    private void ApplyGravity()
    {
        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void GroundCheck()
    {
        bool r = Physics.CheckBox(rightFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1, 1, 2) * GroundCheckRadius, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);
        bool l = Physics.CheckBox(leftFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1, 1, 2) * GroundCheckRadius, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);
        Grounded = r || l;

    }

    private void ChangeCamera()
    {

        if (Input.GetKeyDown(KeyCode.Tab))
        {

            if (PlayerCamera.Follow == FPSCameraRoot.transform)
            {

                PlayerCamera.Follow = ThirdPersonCameraRoot.transform;
                PlayerCamera.LookAt = ThirdPersonCameraRoot.transform;
            }
            else
            {

                PlayerCamera.Follow = FPSCameraRoot.transform;
                PlayerCamera.LookAt = FPSCameraRoot.transform;

            }

        }

    }

    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawCube(leftFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1, 1, 2) * GroundCheckRadius);
        Gizmos.DrawCube(rightFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1, 1, 2) * GroundCheckRadius);


    }

}
