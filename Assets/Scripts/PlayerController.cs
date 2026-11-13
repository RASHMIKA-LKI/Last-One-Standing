using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -15.0f;

    [Header("Look / Camera Settings")]
    public Transform cameraHolder;
    public float mouseSensitivity = 0.15f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Status")]
    public bool isFrozen = false;

    private CharacterController controller;
    private Vector3 velocity;
    private float pitch = 0f;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Lock cursor for FPS/TPS gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // If no cameraHolder assigned, try to find child Camera
        if (cameraHolder == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraHolder = cam.transform;
            }
        }
    }

    void Update()
    {
        HandleCursorToggle();

        // If player is frozen, block movement and rotation
        if (isFrozen)
        {
            ApplyGravityOnly();
            return;
        }

        HandleLook();
        HandleMovement();
    }

    private void HandleCursorToggle()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Escape))
#endif
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 mouseDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        }
#else
        mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity * 10f;
#endif

        // Yaw (Horizontal look - rotate body)
        transform.Rotate(Vector3.up * mouseDelta.x);

        // Pitch (Vertical look - rotate camera holder)
        if (cameraHolder != null)
        {
            pitch -= mouseDelta.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keep grounded firmly
        }

        Vector2 inputMove = Vector2.zero;
        bool isSprinting = false;
        bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.wKey.isPressed) inputMove.y += 1f;
            if (kb.sKey.isPressed) inputMove.y -= 1f;
            if (kb.dKey.isPressed) inputMove.x += 1f;
            if (kb.aKey.isPressed) inputMove.x -= 1f;

            isSprinting = kb.leftShiftKey.isPressed;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }
#else
        inputMove.x = Input.GetAxisRaw("Horizontal");
        inputMove.y = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif

        inputMove = inputMove.normalized;

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 move = (transform.right * inputMove.x + transform.forward * inputMove.y) * currentSpeed;

        controller.Move(move * Time.deltaTime);

        // Jump
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void ApplyGravityOnly()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (frozen)
        {
            velocity = Vector3.zero;
        }
    }
}
