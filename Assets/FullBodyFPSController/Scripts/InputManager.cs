using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

    private PlayerInput playerInput;
    private InputActionMap currentMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction runAction;
    private InputAction crouchAction;
    private InputAction jumpAction;
    private InputAction proneAction;
    private InputAction inventoryAction;
    
    public Vector2 move { get; private set;}
    public Vector2 look { get; private set;}
    public bool run { get; private set; }
    public bool crouch { get; private set; }
    public bool jump { get; set; }
    public bool prone { get; private set; }
    public InputAction InventoryAction { get => inventoryAction; private set => inventoryAction = value; }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        currentMap = playerInput.currentActionMap;
        moveAction = currentMap.FindAction("Move");
        lookAction = currentMap.FindAction("Look");
        runAction = currentMap.FindAction("Run");
        crouchAction = currentMap.FindAction("Crouch");
        jumpAction = currentMap.FindAction("Jump");
        proneAction = currentMap.FindAction("Prone");
        InventoryAction = currentMap.FindAction("Inventory");

        moveAction.performed += OnMove;
        lookAction.performed += OnLook;
        runAction.performed += OnRun;
        crouchAction.performed += OnCrouch;
        jumpAction.performed += OnJump;
        proneAction.performed += OnProne;

        moveAction.canceled += OnMove;
        lookAction.canceled += OnLook;
        runAction.canceled += OnRun;
        crouchAction.canceled += OnCrouch;
        jumpAction.canceled += OnJump;
        proneAction.canceled += OnProne;

    }



    private void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        look = context.ReadValue<Vector2>();
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        run = context.ReadValueAsButton();
        Debug.Log("Run " + context.phase);
    }

    private void OnCrouch(InputAction.CallbackContext context)
    {
        crouch = context.ReadValueAsButton();
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        jump = context.ReadValueAsButton();
    }
    private void OnProne(InputAction.CallbackContext context)
    {
        prone = context.ReadValueAsButton();
    }

    private void OnEnable()
    {
        currentMap.Enable();
    }

    private void OnDisable()
    {
        currentMap.Disable();
    }

}
