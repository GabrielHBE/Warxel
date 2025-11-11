using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/*
    Optimized Player Controller - Gatsby
*/

public class PlayerController : MonoBehaviour
{
    [Header("Key Bindings")]
    [SerializeField] private KeyBinds keyBinds;

    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public float mouse_aim_sensitivity = 1f;
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintMultiplier = 1.7f;
    public float crouchSpeed = 2f;
    public float jumpForce = 2f;
    public float fallMultiplier = 2.5f;


    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundCheckDelay = 0.3f;

    // Private variables
    private Rigidbody rb;
    private PlayerProperties playerProperties;
    private CameraShake cameraShake;

    // Movement
    public float moveHorizontal;
    public float moveForward;
    public float currentMoveSpeed;
    private float sprintSpeed;
    private float originalMoveSpeed;

    // Camera
    private float verticalRotation;
    private float currentMouseSensitivity;
    private float recoilVerticalTarget;
    private float recoilVerticalCurrent;
    private float recoilVelocity;
    private const float recoilSmoothTime = 0.1f;

    // Recoil Z-axis (optimized)
    private float currentRecoilZ;
    private float recoilResetVelocity;

    // Ground check
    private bool wasGroundedLastFrame;
    private float groundCheckTimer;
    private float playerHeight;
    private float raycastDistance;

    // Input flags
    public bool isSprintOnHold = false;
    public bool isCrouchOnHold = false;

    float intecact_distance = 3f;

    void Start()
    {
        InitializeComponents();
        SetupPhysics();
        CalculateDimensions();
        SetupCursor();
    }


    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // ESSENCIAL!
        playerProperties = GetComponent<PlayerProperties>();
        cameraShake = GetComponentInChildren<CameraShake>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void SetupPhysics()
    {
        rb.freezeRotation = true;
        originalMoveSpeed = walkSpeed;
        sprintSpeed = walkSpeed * sprintMultiplier;
        currentMoveSpeed = walkSpeed;
    }

    void CalculateDimensions()
    {
        CapsuleCollider collider = GetComponent<CapsuleCollider>();
        playerHeight = collider.height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.02f;
    }

    void SetupCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        if (Input.GetKeyDown(keyBinds.interactKey))
        {
            Interact();
        }

        HandleInput();
        RotateCamera();
        UpdateRecoil();

        if (Input.GetKeyDown(keyBinds.jumpKey) && playerProperties.isGrounded)
        {
            Jump();
        }

        UpdateGroundCheck();
    }

    void Interact()
    {
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        RaycastHit hit;

        Debug.DrawRay(origin, direction * intecact_distance, Color.red, 1f);

        if (Physics.Raycast(origin, direction, out hit, intecact_distance, LayerMask.GetMask("Interactives")))
        {
            ElevatorCallButton button = hit.collider.GetComponent<ElevatorCallButton>();
            if (button != null)
            {
                button.Interact();
            }
        }

        if(Physics.Raycast(origin, direction, out hit, intecact_distance, LayerMask.GetMask("Vehicle")))
        {
            Vehicle vehicle = hit.collider.GetComponent<Vehicle>();
            Debug.Log(vehicle);
            if (vehicle != null)
            {
                vehicle.EnterVehicle(gameObject);
            }
        }
    }

    void HandleInput()
    {
        // Get input once per frame
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");

        HandleSprint();
        HandleCrouch();
        UpdateMovementSpeed();
    }

    void HandleSprint()
    {
        if (!isSprintOnHold && Input.GetKeyDown(keyBinds.sprintKey))
        {
            playerProperties.sprinting = !playerProperties.sprinting;
            if (playerProperties.sprinting)
                playerProperties.crouched = false;
        }
        else if (isSprintOnHold)
        {
            playerProperties.sprinting = Input.GetKey(keyBinds.sprintKey);
            if (playerProperties.sprinting)
                playerProperties.crouched = false;
        }
    }

    void HandleCrouch()
    {
        if (!isCrouchOnHold && Input.GetKeyDown(keyBinds.crouchKey))
        {
            playerProperties.crouched = !playerProperties.crouched;
            if (playerProperties.crouched)
                playerProperties.sprinting = false;
        }
        else if (isCrouchOnHold)
        {
            playerProperties.crouched = Input.GetKey(keyBinds.crouchKey);
            if (playerProperties.crouched)
                playerProperties.sprinting = false;
        }
    }

    void UpdateMovementSpeed()
    {
        if (playerProperties.crouched)
        {
            currentMoveSpeed = crouchSpeed;
        }
        else if (playerProperties.sprinting && !playerProperties.is_aiming && !playerProperties.is_reloading)
        {
            currentMoveSpeed = sprintSpeed;
        }
        else
        {
            currentMoveSpeed = walkSpeed;
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyJumpPhysics();
    }

    void MovePlayer()
    {
        if (moveHorizontal == 0 && moveForward == 0)
        {
            if (playerProperties.isGrounded)
            {
                playerProperties.sprinting = false;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
            return;
        }

        Vector3 moveDirection = new Vector3(moveHorizontal, 0, moveForward).normalized;
        Vector3 worldDirection = transform.TransformDirection(moveDirection);

        Vector3 targetVelocity = worldDirection * currentMoveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = targetVelocity;
    }

    void RotateCamera()
    {
        currentMouseSensitivity = playerProperties.is_aiming ? mouse_aim_sensitivity : mouseSensitivity;

        // Horizontal rotation
        float horizontalRotation = Input.GetAxis("Mouse X") * currentMouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);

        // Vertical rotation with recoil
        float mouseVertical = Input.GetAxis("Mouse Y") * currentMouseSensitivity;
        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVelocity, recoilSmoothTime);

        verticalRotation -= mouseVertical + recoilVerticalCurrent;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        // Apply combined rotation (vertical + recoil Z)
        cameraTransform.localEulerAngles = new Vector3(verticalRotation, 0, currentRecoilZ);

        // Reset vertical recoil target
        recoilVerticalTarget = 0f;
    }

    void UpdateRecoil()
    {
        // Smoothly reset Z recoil every frame
        if (Mathf.Abs(currentRecoilZ) > 0.01f)
        {
            currentRecoilZ = Mathf.SmoothDamp(currentRecoilZ, 0f, ref recoilResetVelocity, 0.1f);
        }
    }

    public void ApplyCameraRecoil(float verticalRecoil, float horizontalRecoil)
    {
        recoilVerticalTarget += verticalRecoil;
        transform.Rotate(0, horizontalRecoil, 0);

        WeaponProperties weaponProperties = GetComponentInChildren<WeaponProperties>();
        if (weaponProperties != null)
        {
            float range = (weaponProperties.horizontal_recoil_media + weaponProperties.vertical_recoil_media) / 6f;
            currentRecoilZ = Random.Range(-range, range);
            recoilResetVelocity = 0f;
        }
    }

    void UpdateGroundCheck()
    {
        if (groundCheckTimer > 0f)
        {
            groundCheckTimer -= Time.deltaTime;
            return;
        }

        Vector3 rayOrigin = transform.position;
        bool isGroundedNow = Physics.Raycast(rayOrigin, Vector3.down, raycastDistance, groundLayer | LayerMask.GetMask("Voxel"));

        if (!wasGroundedLastFrame && isGroundedNow)
        {
            StartCoroutine(cameraShake.JumpCameraShake());
        }

        playerProperties.isGrounded = isGroundedNow;
        wasGroundedLastFrame = isGroundedNow;
    }

    void Jump()
    {
        StartCoroutine(cameraShake.JumpCameraShake());
        playerProperties.isGrounded = false;
        groundCheckTimer = groundCheckDelay;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void ApplyJumpPhysics()
    {

        Vector3 gravity = Physics.gravity.y * Vector3.up;

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += gravity * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

    }


    public void ChangeWeaponVelocitySpeed(float speedModifier)
    {
        walkSpeed = originalMoveSpeed + speedModifier;
        sprintSpeed = walkSpeed * sprintMultiplier;
        UpdateMovementSpeed();
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = playerProperties.isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, Vector3.down * raycastDistance);
        }
    }
}