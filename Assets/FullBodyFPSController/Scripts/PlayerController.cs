using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;
using UnityEditor;
using UnityEngine.InputSystem;
using Input = UnityEngine.Input;
using Cinemachine;

public class PlayerController : MonoBehaviour
{

    private InputManager InputManager;
    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider PlayerCollider;
    private bool hasAnimator;
    
    // Hashes
    private int X_VelocityHash;
    private int Y_VelocityHash;
    private int CrouchHash;
    private int JumpHash;
    private int GroundedHash;
    private int FallHash;


    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float crouchSpeed = 1;
    [SerializeField] private float force_factor;
    [SerializeField] private float blendSpeed = 10;
    [SerializeField] private float crouchHeight;
    [SerializeField] private float normalHeight;
    [SerializeField] private float jumpHeight; //in centimeters
    private float targetSpeed;
    private Vector2 currentVelocity;
    private float jumpForce;

    [Space(10)]

    [Header("World Physics Parameters")]
    [SerializeField] private float gravityScale;
    private float gravityValue;

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
    private const float threshold = 0.01f;
    private float RotationVelocity;

    [Space(10)]

    [Header("Ground Check Parameters")]
    [SerializeField] private float GroundCheckOffset = 0.1f;
    [SerializeField] private LayerMask GroundLayers;
    [SerializeField] private float GroundCheckRadius = 0.25f;
    [SerializeField] private Transform rightFeet;
    [SerializeField] private Transform leftFeet;
    private bool Grounded;


    private void Start()
    {
        DOTween.SetTweensCapacity(500, 50);

        hasAnimator = TryGetComponent<Animator>(out animator);
        rb = GetComponent<Rigidbody>();
        InputManager = GetComponent<InputManager>();

        PlayerCollider = GetComponent<CapsuleCollider>();

        X_VelocityHash = Animator.StringToHash("X_Velocity");
        Y_VelocityHash = Animator.StringToHash("Y_Velocity");
        CrouchHash = Animator.StringToHash("Crouch");
        JumpHash = Animator.StringToHash("Jump");
        GroundedHash = Animator.StringToHash("Grounded");
        FallHash = Animator.StringToHash("Fall");


        gravityValue = Physics.gravity.magnitude;
        Debug.Log("gravity is " + gravityValue);
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        Move();
        Crouch();
        HandleJumpAnimation();


    }

    private void LateUpdate()
    {
        CameraRotation();
        
    }

    private void Update()
    {
        GroundCheck();

        //animator.SetBool(GroundedHash, Grounded);
        //animator.SetBool(FallHash, !Grounded);

        ChangeCamera();
        //HandleInventoryAction();

        if (!Grounded)
        {

            gravityValue = Physics.gravity.magnitude * gravityScale;

        }
        else
        {

            gravityValue = Physics.gravity.magnitude;

        }

        Debug.Log(Grounded);
        
        
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

    /*private void HandleInventoryAction()
    {

        if (InputManager.InventoryAction.WasPressedThisFrame())
        {

            bool isActive = InventoryManager.Instance.InventoryUI.activeInHierarchy;

            InventoryManager.Instance.InventoryUI.SetActive(!isActive);

            if (!isActive)
            {
                InventoryManager.Instance.LoadSaves();

            }
            else
            {

                InventoryManager.Instance.UpdateSaves();

            }

        }

    }*/


    private void Move()
    {
        float deltaTimeMultiplier = Time.fixedDeltaTime;

        if (!hasAnimator) { return; }

        Vector2 MoveInput = InputManager.move.normalized;
        float MoveX = MoveInput.x;
        float MoveZ = MoveInput.y;

        targetSpeed = InputManager.run ? runSpeed : walkSpeed;
        if (InputManager.crouch) { targetSpeed = crouchSpeed; }
        Vector3 inputDirection = (transform.right * MoveX + transform.forward * MoveZ).normalized;

        if (MoveInput == Vector2.zero)
        {
            targetSpeed = 0;
            inputDirection = Vector3.zero;
        }

        Debug.Log(targetSpeed);
        if (!Grounded) { targetSpeed = 0; }


        //currentVelocity.x = Mathf.SmoothStep(currentVelocity.x, targetSpeed * MoveX, deltaTimeMultiplier * blendSpeed);
        //currentVelocity.y = Mathf.SmoothStep(currentVelocity.y, targetSpeed * MoveZ, deltaTimeMultiplier * blendSpeed);

        DOTween.To(() => currentVelocity.x, x => currentVelocity.x = x, targetSpeed * MoveX, blendSpeed);
        DOTween.To(() => currentVelocity.y, x => currentVelocity.y = x, targetSpeed * MoveZ, blendSpeed);


        Debug.Log(MoveInput);

        Vector3 force = new Vector3(currentVelocity.x, 0, currentVelocity.y);

        //Debug.Log(currentVelocity);

        if (Mathf.Abs(currentVelocity.x) < 0.001) { currentVelocity.x = 0; }
        if (Mathf.Abs(currentVelocity.y) < 0.001) { currentVelocity.y = 0; }

        animator.SetFloat(X_VelocityHash, currentVelocity.x);
        animator.SetFloat(Y_VelocityHash, currentVelocity.y);

        rb.AddForce(inputDirection * force.magnitude * force_factor,ForceMode.Force);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, RotationVelocity, 0));

    }

    private void Crouch()
    {

        if (!hasAnimator) { return; }

        if (InputManager.crouch)
        {

            animator.SetBool(CrouchHash,true);
            PlayerCollider.height = crouchHeight;
            PlayerCollider.center = new Vector3(0,crouchHeight/2,0);

        }
        else
        {

            animator.SetBool(CrouchHash, false);
            PlayerCollider.height = normalHeight;
            PlayerCollider.center = new Vector3(0, normalHeight / 2, 0);


        }

    }

    
    private void Jump()
    {
        if (!hasAnimator) return;
        if (!InputManager.jump) return;
        if (!Grounded) return;

        jumpForce = Mathf.Sqrt(jumpHeight * 100 * -2 * (Physics.gravity.y * gravityScale));

        rb.AddForce(-rb.velocity.y * Vector3.up, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

    }

    private void HandleJumpAnimation()
    {
        if (!hasAnimator) return;
        if (!InputManager.jump) return;
        animator.SetTrigger(JumpHash);
        

    }

    public void ApplyJumpForce()
    {

        jumpForce = Mathf.Sqrt(jumpHeight * 100 * -2 * (Physics.gravity.y * gravityScale));

        rb.AddForce(-rb.velocity.y * Vector3.up, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        animator.ResetTrigger(JumpHash);
    }

    private void GroundCheck()
    {
        bool r = Physics.CheckBox(rightFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1,1,2) * GroundCheckRadius, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);
        bool l = Physics.CheckBox(leftFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1,1,2) * GroundCheckRadius, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);

        Grounded = r || l;

    }

    private void ApplyGravity()
    {
        rb.AddForce(gravityValue * Vector3.down * rb.mass,ForceMode.Force);
    }

    private void CameraRotation()
    {
        if (!hasAnimator) return;

        var Mouse_X = InputManager.look.x;
        var Mouse_Y = InputManager.look.y;

        float deltaTimeMultiplier = Time.smoothDeltaTime;

        CinemachineTargetPitch -= Mouse_Y * RotationSpeed * deltaTimeMultiplier;
        CinemachineTargetPitch = ClampAngle(CinemachineTargetPitch, BottomClamp, TopClamp);
        FPSCameraRoot.transform.localRotation = Quaternion.Euler(CinemachineTargetPitch, 0, 0);

        RotationVelocity = Mouse_X * RotationSpeed * deltaTimeMultiplier;

        FPSCameraRoot.transform.position = HeadPosition.position;


    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawCube(leftFeet.position - new Vector3(0,0,GroundCheckOffset), new Vector3(1, 1, 2) * GroundCheckRadius);
        Gizmos.DrawCube(rightFeet.position - new Vector3(0, 0, GroundCheckOffset), new Vector3(1, 1, 2) * GroundCheckRadius);

    }

}
