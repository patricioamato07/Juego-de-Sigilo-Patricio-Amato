using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ThirdPersonController : MonoBehaviour
{
    public Transform cam;

    public float moveSpeed = 6f;
    public float jumpForce = 7f;

    [Header("Gravity")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public int maxAirDashes = 1;
    public bool hasDashUnlocked = false;
    public AudioClip dashSound;

    public float mouseSensitivity = 0.12f;
    public float cameraDistance = 5f;
    public float cameraHeight = 1.6f;

    public Rigidbody rb;
    private float yaw;
    private float pitch;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;
    private int airDashesUsed;
    private bool wasGroundedLastFrame;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        Look();
        Jump();
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        bool grounded = IsGrounded();
        if (grounded && !wasGroundedLastFrame)
        {
            airDashesUsed = 0;
        }
        wasGroundedLastFrame = grounded;

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
        }

        if (isDashing)
        {
            PerformDashStep();
            return;
        }

        Move();
        ApplyCustomGravity();
    }

    private void LateUpdate()
    {
        PositionCamera();
    }
    void Look()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -40f, 70f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    Vector3 GetMoveInput()
    {
        var kb = Keyboard.current;

        float x = 0f, z = 0f;
        if (kb.wKey.isPressed) z += 1f;
        if (kb.sKey.isPressed) z -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.aKey.isPressed) x -= 1f;
        return new Vector3(x, 0f, z).normalized;
    }

    void Jump()
    {
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

        if (isDashing)
        {
            CancelDash();
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            return;
        }

        if (IsGrounded())
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void CancelDash()
    {
        isDashing = false;
        rb.useGravity = true;
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    void PositionCamera()
    {
        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = transform.position + Vector3.up * cameraHeight;

        cam.position = focusPoint - camRotation * Vector3.forward * cameraDistance;
        cam.rotation = camRotation;
    }

    void Move()
    {
        Vector3 input = GetMoveInput();
        Vector3 moveDir = transform.forward * input.z + transform.right * input.x;
        Vector3 targetHorizontalVelocity = moveDir * moveSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityChange = targetHorizontalVelocity - currentHorizontalVelocity;
        velocityChange = Vector3.ClampMagnitude(velocityChange, moveSpeed);
        velocityChange.y = 0f;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.y > 0f && !Keyboard.current.spaceKey.isPressed)
        {
            rb.AddForce(Physics.gravity * (lowJumpMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    // --- Dash ---

    void HandleDashInput()
    {
        if (isDashing) return;

        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && CanDash())
        {
            StartDash();
        }
    }

    bool CanDash()
    {
        if (!hasDashUnlocked) return false;
        if (dashCooldownTimer > 0f) return false;
        if (!IsGrounded() && airDashesUsed >= maxAirDashes) return false;
        return true;
    }

    public void UnlockDash()
    {
        hasDashUnlocked = true;
    }

    void StartDash()
    {
        Vector3 input = GetMoveInput();
        Vector3 dir = transform.forward * input.z + transform.right * input.x;

        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
        }

        dashDirection = dir.normalized;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        isDashing = true;

        if (!IsGrounded())
        {
            airDashesUsed++;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.useGravity = false;

        if (dashSound != null)
        {
            AudioSource.PlayClipAtPoint(dashSound, transform.position);
        }
    }

    void PerformDashStep()
    {
        dashTimer -= Time.fixedDeltaTime;

        rb.linearVelocity = dashDirection * dashSpeed;

        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.useGravity = true;
        }
    }
}
