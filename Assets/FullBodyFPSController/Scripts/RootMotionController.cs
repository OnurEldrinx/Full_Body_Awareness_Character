using DG.Tweening;
using FischlWorks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionController : MonoBehaviour
{

    private InputManager inputManager;
    private Animator animator;
    private CharacterController characterController;
    private csHomebrewIK footIKScript;

    private int X_VelocityHash;
    private int Y_VelocityHash;
    private int JumpHash;
    private int GroundedHash;
    private int FallHash;


    private Vector3 rootMotion;


    [Header("Movement Parameters")]
    [SerializeField] private bool useRootMotion;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float speedChangeRate;
    [SerializeField] private float stopSmooth;
    [SerializeField] private float moveSmooth;
    [SerializeField] private bool isStopped;
    private float smoothTime;
    private float targetSpeed;
    private Vector2 currentVelocity;

    [Space(10)]

    [Header("Jump Parameters")]
    [SerializeField] private float jumpHeight;
    [SerializeField] private float JumpTimeout = 0.50f;
    [SerializeField] private float FallTimeout = 0.15f;
    private float jumpTimeoutDelta;
    private float fallTimeoutDelta;

    [Space(10)]

    [Header("Ground Check Parameters")]
    [SerializeField] private float GroundCheckOffsetX = 0.1f;
    [SerializeField] private float GroundCheckOffsetY = 0.1f;
    [SerializeField] private float GroundCheckOffsetZ = 0.1f;
    [SerializeField] private LayerMask GroundLayers;
    [SerializeField] private float GroundCheckRadius = 0.25f;
    [SerializeField] private Transform rightFeet;
    [SerializeField] private Transform leftFeet;
    [SerializeField] private Transform centerRef;
    [SerializeField] private bool Grounded;

    [Space(10)]

    [Header("Physics Parameters")]
    [SerializeField] private float gravity = -15.0f;
    private float terminalVelocity = 53f; // in air

    private float verticalVelocity;
    private bool hasAnimator;

    // Start is called before the first frame update
    void Start()
    {


        inputManager = GetComponent<InputManager>();
        hasAnimator = TryGetComponent(out animator);
        characterController = GetComponent<CharacterController>();
        footIKScript = GetComponent<csHomebrewIK>();

        X_VelocityHash = Animator.StringToHash("X_Velocity");
        Y_VelocityHash = Animator.StringToHash("Y_Velocity");
        JumpHash = Animator.StringToHash("Jump");
        GroundedHash = Animator.StringToHash("Grounded");
        FallHash = Animator.StringToHash("Fall");

        jumpTimeoutDelta = JumpTimeout;
        fallTimeoutDelta = FallTimeout;

    }

    // Update is called once per frame
    void Update()
    {
        ApplyGravity();
        GroundCheck();
        //animator.SetBool(GroundedHash, Grounded);
        if (useRootMotion) MoveWithRootMotion();
        JumpTimeouts();
        HandleJumpAnimation();
       // Debug.Log(targetSpeed);
    }

    private void MoveWithRootMotion()
    {
        float deltaTimeMultiplier = Time.deltaTime;

        Vector2 MoveInput = inputManager.move.normalized;

        //targetSpeed = inputManager.run ? runSpeed : walkSpeed;

        if (MoveInput == Vector2.zero)
        {
            //targetSpeed = 0;
            DOTween.To(() => targetSpeed, x => targetSpeed = x, 0, smoothTime);

            isStopped = (targetSpeed > 0);

        }
        else
        {
            isStopped = false;

            if (inputManager.run)
            {

                DOTween.To(() => targetSpeed, x => targetSpeed = x, runSpeed, smoothTime);
                DOTween.To(() => footIKScript.GlobalWeight, x => footIKScript.GlobalWeight = x, 0, 0.2f);
                
            }
            else
            {
                DOTween.To(() => targetSpeed, x => targetSpeed = x, walkSpeed, smoothTime);
                DOTween.To(() => footIKScript.GlobalWeight, x => footIKScript.GlobalWeight = x, 1, 0.2f);


            }

        }

        if(targetSpeed <= 0.5f) { targetSpeed = 0; }

        smoothTime = isStopped ? stopSmooth : moveSmooth;

        DOTween.To(() => currentVelocity.x, x => currentVelocity.x = x, targetSpeed * MoveInput.x, smoothTime);
        DOTween.To(() => currentVelocity.y, x => currentVelocity.y = x, targetSpeed * MoveInput.y, smoothTime);

        //currentVelocity.x = Mathf.SmoothStep(currentVelocity.x, targetSpeed * MoveInput.x, deltaTimeMultiplier * speedChangeRate);
        //currentVelocity.y = Mathf.SmoothStep(currentVelocity.y, targetSpeed * MoveInput.y, deltaTimeMultiplier * speedChangeRate);

        if (Mathf.Abs(currentVelocity.x) < 0.01f) { currentVelocity.x = 0; }
        if (Mathf.Abs(currentVelocity.y) < 0.01f) { currentVelocity.y = 0; }

        animator.SetFloat(X_VelocityHash, currentVelocity.x);
        animator.SetFloat(Y_VelocityHash, currentVelocity.y);


        characterController.Move(rootMotion);
        rootMotion = Vector3.zero;


    }

    private void OnAnimatorMove()
    {
        rootMotion += animator.deltaPosition;
        rootMotion.y = verticalVelocity * Time.deltaTime;
        //Debug.Log(rootMotion);
    }

    private void JumpTimeouts()
    {

        if (Grounded)
        {
            

            animator.SetBool(GroundedHash, true);
            animator.SetBool(FallHash, false);
            // reset the fall timeout timer
            fallTimeoutDelta = FallTimeout;

            // jump timeout
            if (jumpTimeoutDelta >= 0.0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }

        }
        else
        {
            

            animator.SetBool(GroundedHash, false);

            // reset the jump timeout timer
            jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (hasAnimator)
                {
                    animator.SetBool(FallHash, true);
                }
            }

        }

    }

    private void HandleJumpAnimation()
    {
        if (!hasAnimator) return;
        if (!inputManager.jump) return;

        if (jumpTimeoutDelta <= 0.0f)
        {
            animator.SetTrigger(JumpHash);
            DisableFootIK();
        }

    }

    //Anim Event Function
    public void ApplyJumpVelocity()
    {
        // the square root of H * -2 * G = how much velocity needed to reach desired height
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator.ResetTrigger(JumpHash);
        
    }

    public void EnableFootIK()
    {
        footIKScript.EnableIKPositioning = true;
        footIKScript.EnableIKRotating = true;
        DOTween.To(() => footIKScript.GlobalWeight, x => footIKScript.GlobalWeight = x, 1, 0.5f);

    }
    public void DisableFootIK()
    {
        footIKScript.EnableIKPositioning = false;
        footIKScript.EnableIKRotating = false;
        DOTween.To(() => footIKScript.GlobalWeight, x => footIKScript.GlobalWeight = x, 0, 0.5f);
        //footIKScript.GlobalWeight = 0;
    }


    private void ApplyGravity()
    {
        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // stop our velocity dropping infinitely when grounded
        if (verticalVelocity < 0.0f && Grounded)
        {
            verticalVelocity = -2f;
        }


    }

    private void GroundCheck()
    {
        bool r = Physics.CheckBox(rightFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), new Vector3(1, 1, 2) * GroundCheckRadius, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);
        bool l = Physics.CheckBox(leftFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), new Vector3(1, 1, 2) * GroundCheckRadius, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);
        bool c = Physics.CheckSphere(centerRef.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), GroundCheckRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        Grounded = r || l || c;

        /*bool r = Physics.CheckSphere(rightFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), GroundCheckRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        bool l = Physics.CheckSphere(leftFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), GroundCheckRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        Grounded = r || l;*/

    }

    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawCube(leftFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), new Vector3(1, 1, 2) * GroundCheckRadius);
        Gizmos.DrawCube(rightFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), new Vector3(1, 1, 2) * GroundCheckRadius);
        Gizmos.DrawSphere(centerRef.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), GroundCheckRadius/2);

        /*Gizmos.DrawSphere(rightFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ),GroundCheckRadius);
        Gizmos.DrawSphere(leftFeet.position - new Vector3(GroundCheckOffsetX, GroundCheckOffsetY, GroundCheckOffsetZ), GroundCheckRadius);*/


    }
}
