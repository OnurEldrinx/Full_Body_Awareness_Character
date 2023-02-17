using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Input = UnityEngine.Input;

public class CameraController : MonoBehaviour
{
    private InputManager inputManager;
    private Animator animator;

    [Header("Camera Parameters")]
    [SerializeField] private float RotationSpeed = 20;
    [SerializeField] private float BottomClamp = -90;
    [SerializeField] private float TopClamp = 90;
    [SerializeField] private GameObject FPSCameraRoot;
    [SerializeField] private GameObject ThirdPersonCameraRoot;
    [SerializeField] private CinemachineVirtualCamera FirstPersonCamera;
    [SerializeField] private CinemachineVirtualCamera ThirdPersonCamera;
    [SerializeField] private Transform HeadPosition;
    [SerializeField] private float CameraAngleOverride;
    [SerializeField] private CameraMod cameraMod;
    private float CinemachineTargetPitch;
    private float CinemachineTargetYaw;
    private const float threshold = 0.01f;
    private float RotationVelocity;
    private bool hasAnimator;

    private enum CameraMod {FirstPerson,ThirdPerson}

    void Start()
    {
        inputManager = GetComponent<InputManager>();
        hasAnimator = TryGetComponent(out animator);
    }

    void Update()
    {
        ChangeCamera();
    }

    private void LateUpdate()
    {
        if(cameraMod is CameraMod.FirstPerson){

            FirstPersonCameraControl();

        } else if( cameraMod is CameraMod.ThirdPerson){

            //ThirdPersonCameraControl();
            FirstPersonCameraControl();

        }
    }

    private void FirstPersonCameraControl()
    {
        if (!hasAnimator) return;

        var Mouse_X = inputManager.look.x;
        var Mouse_Y = inputManager.look.y;

        float deltaTimeMultiplier = Time.smoothDeltaTime;

        CinemachineTargetPitch -= Mouse_Y * RotationSpeed * deltaTimeMultiplier;
        CinemachineTargetPitch = ClampAngle(CinemachineTargetPitch, BottomClamp, TopClamp);

        if(inputManager.move == Vector2.zero)
        {
            CinemachineTargetYaw += Mouse_X * RotationSpeed * deltaTimeMultiplier;
            CinemachineTargetYaw = ClampAngle(CinemachineTargetYaw, -110, 110);
        }
        else
        {
            if(Mathf.Abs(CinemachineTargetYaw) >= 0.01f)
            {
                CinemachineTargetYaw = Mathf.LerpAngle(CinemachineTargetYaw, 0f, deltaTimeMultiplier * 4);
            }
            else
            {

                CinemachineTargetYaw = 0f;
            }

            RotationVelocity = Mouse_X * RotationSpeed * deltaTimeMultiplier;
            transform.rotation = transform.rotation * Quaternion.Euler(0.0f, RotationVelocity, 0.0f);
        }
        

        FPSCameraRoot.transform.localRotation = Quaternion.Euler(CinemachineTargetPitch, CinemachineTargetYaw, 0);
        FPSCameraRoot.transform.position = HeadPosition.position;


        


    }

    private void ThirdPersonCameraControl()
    {
        if (!hasAnimator) return;

        var MouseInput = inputManager.look;
        var Mouse_X = MouseInput.x;
        var Mouse_Y = MouseInput.y;

        float deltaTimeMultiplier = Time.deltaTime;

        if (MouseInput.sqrMagnitude >= threshold)
        {

            CinemachineTargetYaw += Mouse_X * deltaTimeMultiplier;
            CinemachineTargetPitch -= Mouse_Y * deltaTimeMultiplier;
        }

        CinemachineTargetYaw = ClampAngle(CinemachineTargetYaw, float.MinValue, float.MaxValue);
        CinemachineTargetPitch = ClampAngle(CinemachineTargetPitch, BottomClamp, TopClamp);

        ThirdPersonCameraRoot.transform.rotation = Quaternion.Euler(CinemachineTargetPitch + CameraAngleOverride, CinemachineTargetYaw, 0.0f);
        RotationVelocity = Mouse_X * RotationSpeed * deltaTimeMultiplier;

        transform.rotation = transform.rotation * Quaternion.Euler(0.0f, RotationVelocity, 0.0f);
    }

    private void ChangeCamera()
    {

        if (Input.GetKeyDown(KeyCode.Tab))
        {

            /*if (FirstPersonCamera.Follow == FPSCameraRoot.transform)
            {

                FirstPersonCamera.Follow = ThirdPersonCameraRoot.transform;
                FirstPersonCamera.LookAt = ThirdPersonCameraRoot.transform;
            }
            else
            {

                FirstPersonCamera.Follow = FPSCameraRoot.transform;
                FirstPersonCamera.LookAt = FPSCameraRoot.transform;

            }*/

            if (FirstPersonCamera.Priority > ThirdPersonCamera.Priority) {

                //FirstPersonCamera.gameObject.SetActive(false);
                //ThirdPersonCamera.gameObject.SetActive(true);
                FirstPersonCamera.Priority = 1;
                ThirdPersonCamera.Priority = 2;
                cameraMod = CameraMod.ThirdPerson;
            }
            else{

                //FirstPersonCamera.gameObject.SetActive(true);
                //ThirdPersonCamera.gameObject.SetActive(false);
                FirstPersonCamera.Priority = 2;
                ThirdPersonCamera.Priority = 1;
                cameraMod = CameraMod.FirstPerson;

            }


        }

    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

}
