using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8.5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.8f;

    [Header("Look Settings")]
    public float mouseSensitivity = 1.8f;
    public float maxLookAngle = 80f;

    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
        sprintAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
    }

    private void Update()
    {
        HandleMovementAndJump(); // combined movement
        HandleLook();
        ApplyGravity();
        HandleJump();
    }

    private void HandleMovementAndJump()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        float speed = sprintAction.action.IsPressed() ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        // --- ADD jump Y velocity into movement ---
        Vector3 motion = move * speed;
        motion.y = velocity.y;

        characterController.Move(motion * Time.deltaTime);
    }

    private void HandleLook()
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>();

        // Yaw (horizontal rotation)
        transform.Rotate(Vector3.up * look.x * mouseSensitivity);

        // Pitch (vertical rotation)
        cameraPitch -= look.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        // Apply pitch to camera
        Camera.main.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleJump()
    {
        if (characterController.isGrounded && jumpAction.action.WasPressedThisFrame())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }


    private void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
    }
}
