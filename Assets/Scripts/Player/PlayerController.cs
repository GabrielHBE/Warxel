using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Multiplayer / Player")]
    [SerializeField] private GameObject first_person_player;
    [SerializeField] private MeshRenderer[] hideToOwnerItems;
    [SerializeField] private GameObject[] hideToNotOwnerItems;
    [SerializeField] private GameObject[] body_parts;

    [Header("Body")]
    public GameObject playerHead;

    [Header("Colliders")]
    public CapsuleCollider stand_collider;
    public CapsuleCollider crouch_collider;
    public CapsuleCollider prone_collider;
    public BoxCollider deah_collider;

    [Header("Camera Settings")]
    public Camera playerCamera;

    [Header("Movement Settings - Tutorial Style")]
    public Transform orientation;

    [SerializeField] private float walkSpeed = 14f;
    [SerializeField] private float sprintSpeed = 14f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airMultiplier = 0.4f;
    [SerializeField] private float playerHeight = 2f;

    [Header("Legacy Movement Settings")]
    [SerializeField] private float timeBetweenRolls = 2f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float jump_force_impact;
    [SerializeField] private float jump_force_recovety_time;
    [HideInInspector] public float moveForward;
    [HideInInspector] public float moveHorizontal;

    [Header("Stairs")]
    [SerializeField] GameObject stepRayUpper;
    [SerializeField] GameObject stepRayLower;
    [SerializeField] float stepHeight = 0.3f;
    [SerializeField] float stepSmooth = 2f;


    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Air Control Settings")]
    [SerializeField] private float airSpeedDecayRate = 0.5f;
    [SerializeField] private float minAirSpeed = 1f;

    [Header("Ground Detection")]
    [SerializeField] private float ground_detection_raycastDistance;
    public LayerMask groundLayer;

    [Header("Private References")]
    [SerializeField] private Volume volume;
    [SerializeField] private float footstepSound_interval;
    [SerializeField] private FootstepSound footstepSound;
    [SerializeField] private Weapon weapon;
    [SerializeField] private SoldierHudManager soldierHudManager;
    [SerializeField] private SwayNBobScript SwayNBob;
    [SerializeField] private ThirdPersonWeapon thirdPersonWeapon;
    [SerializeField] private PlayerProperties playerProperties;

    #endregion

    #region Private Variables

    // Components
    public Rigidbody rb;
    private CameraShake cameraShake;

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;

    // Movement - Tutorial Style
    public float currentMoveSpeed;
    private float original_sprint_speed;
    private float original_walk_speed;
    private float original_crouch_speed;
    private bool readyToJump;

    private Vector3 moveDirection;

    // Legacy
    private float original_footstepSound_interval;
    private float death_timer;
    private float colliders_difference;

    // Camera & Recoil
    private float verticalRotation;
    private float currentMouseSensitivity;
    private float recoilVerticalTarget;
    private float recoilVerticalCurrent;
    private float recoilVerticalVelocity;
    private bool is_night_vision_active = false;
    private float horizontalRecoilTarget;
    private float horizontalRecoilCurrent;
    private float horizontalRecoilVelocity;

    // Recoil Z-axis
    private float currentRecoilZ;
    private float recoilResetVelocity;
    private float targetRecoilZ;
    private float recoilZVelocity;

    // Ground Check
    private bool wasGroundedLastFrame;
    private bool grounded;

    // Recoil Timing
    private float resetRecoilSpeed;
    private float applyRecoilSpeed;

    // Roll Double-click
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    // Interaction
    public const float INTERACT_DISTANCE = 10f;

    //Settings
    private Settings settings;
    private KeyBinds keyBinds;
    private Controls controls;
    private Video video;
    private GeneralHudAlertMessages generalHudAlertMessages;


    private float yaw;

    #endregion

    #region Unity Lifecycle

    public void InitializePlayer()
    {
        stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepHeight, stepRayUpper.transform.position.z);

        original_footstepSound_interval = footstepSound_interval;

        colliders_difference = stand_collider.height - crouch_collider.height;
        original_sprint_speed = sprintSpeed;
        original_walk_speed = walkSpeed;
        original_crouch_speed = crouchSpeed;

        InitializeComponents();
        SetupPhysics();

        settings = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<Settings>();
        generalHudAlertMessages = settings.GetComponent<GeneralHudAlertMessages>();
        keyBinds = GameObject.FindGameObjectWithTag("Settings").GetComponent<KeyBinds>();
        controls = keyBinds.transform.GetComponent<Controls>();
        video = keyBinds.transform.GetComponent<Video>();

        playerHead.GetComponentInChildren<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;

        InitializeVignette();
        HideOwnerItems(true);

        readyToJump = true;
    }

    void Update()
    {
        HandleDebugInput();

        if (playerProperties.is_in_vehicle)
        {
            first_person_player.SetActive(false);
            UpdateHeadRotation();
            return;
        }

        first_person_player.SetActive(true);

        FootstepSound();
        UpdateColliderState();
        UpdateGroundCheck();
        UpdateFOV();

        if (playerProperties.is_dead)
        {
            HandleDeathState();
            return;
        }

        soldierHudManager.deadPlayerHud.gameObject.SetActive(false);
        death_timer = 0;

        HandleInteractionInput();
        HandlePlayerInput();
        RotateCamera();
        UpdateRecoil();

        // Jump handling
        if (Input.GetKeyDown(keyBinds.PLAYER_jumpKey) && readyToJump && grounded && !playerProperties.is_proned && !playerProperties.crouched && !playerProperties.roll)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // Handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 1;
    }

    void FixedUpdate()
    {
        if (playerProperties.is_dead || playerProperties.is_in_vehicle)
        {
            moveForward = 0;
            moveHorizontal = 0;
            return;
        }

        UpdateMouseSensitivity();
        stepClimb();
        MovePlayer();

        // Better jump physics
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        foreach (GameObject part in body_parts)
        {
            part.gameObject.tag = "OwnerPlayer";
        }

        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.freezeRotation = true;
        cameraShake = GetComponentInChildren<CameraShake>();
    }

    private void SetupPhysics()
    {
        currentMoveSpeed = walkSpeed;
    }

    private void InitializeVignette()
    {
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out filmGrain);
            volume.profile.TryGet(out colorAdjustments);
        }
    }

    #endregion

    #region Input Handling

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Revive();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Damage(100);
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(keyBinds.PLAYER_interactKey))
        {
            Interact();
        }
    }

    private void HandlePlayerInput()
    {
        if (playerProperties.roll)
        {
            moveForward = 1;
            moveHorizontal = 0;
        }
        else
        {
            moveHorizontal = 0;
            moveForward = 0;

            if ((Input.GetKey(keyBinds.PLAYER_moveFowardKey) && Input.GetKey(keyBinds.PLAYER_moveBackwardsdKey)) || settings.is_menu_settings_active)
            {
                moveForward = 0;
            }
            else if (Input.GetKey(keyBinds.PLAYER_moveFowardKey))
            {
                moveForward = 1;
            }
            else if (Input.GetKey(keyBinds.PLAYER_moveBackwardsdKey))
            {
                moveForward = -1;
            }

            if ((Input.GetKey(keyBinds.PLAYER_moveLeftKey) && Input.GetKey(keyBinds.PLAYER_moveRightKey)) || settings.is_menu_settings_active)
            {
                moveHorizontal = 0;
            }
            else if (Input.GetKey(keyBinds.PLAYER_moveLeftKey))
            {
                moveHorizontal = -1;
            }
            else if (Input.GetKey(keyBinds.PLAYER_moveRightKey))
            {
                moveHorizontal = 1;
            }
        }

        HandleNightVision();

        if (grounded)
        {
            HandleRoll();
            HandleSprint();
            HandleCrouch();
            HandleProne();
            UpdateMovementSpeed();
        }
    }

    private void HandleSprint()
    {
        if (playerProperties.isProneTransition) return;

        if (moveForward == 0 && moveHorizontal == 0)
        {
            playerProperties.sprinting = false;
            return;
        }

        Vector3 origin_ = playerHead.transform.position;
        float distance = colliders_difference * 4.5f;

        if (!controls.is_sprint_on_hold && Input.GetKeyDown(keyBinds.PLAYER_sprintKey))
        {
            if (playerProperties.crouched)
            {
                Debug.DrawLine(origin_, origin_ + Vector3.up * distance, Color.red, 2);

                if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer))
                {
                    generalHudAlertMessages.CreateMessage("Not Enough Space", 2);
                    return;
                }
            }
            ToggleSprint();
        }
        else if (controls.is_sprint_on_hold)
        {
            if (playerProperties.crouched)
            {
                Debug.DrawLine(origin_, origin_ + Vector3.up * distance, Color.red, 2);

                if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer))
                {
                    generalHudAlertMessages.CreateMessage("Not Enough Space", 2);
                    return;
                }
            }
            UpdateHoldSprint();
        }
    }

    private void ToggleSprint()
    {
        playerProperties.sprinting = !playerProperties.sprinting;

        if (playerProperties.sprinting)
        {
            playerProperties.crouched = false;
            playerProperties.is_proned = false;
        }
    }

    private void UpdateHoldSprint()
    {
        playerProperties.sprinting = Input.GetKey(keyBinds.PLAYER_sprintKey);

        if (playerProperties.sprinting)
        {
            playerProperties.crouched = false;
            playerProperties.is_proned = false;
        }
    }

    private void HandleProne()
    {
        Vector3 origin_ = transform.position;
        float distance = 7f;

        if (!controls.is_prone_on_hold && Input.GetKeyDown(keyBinds.PLAYER_proneKey))
        {
            if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer) && playerProperties.is_proned)
            {
                Debug.Log(hit.transform.gameObject.name);
                generalHudAlertMessages.CreateMessage("Not Enough Space", 2);
                return;
            }

            ToggleProne();
        }
        else if (controls.is_prone_on_hold)
        {
            if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer) && playerProperties.is_proned)
            {
                generalHudAlertMessages.CreateMessage("Not Enough Space", 2);
                return;
            }
            UpdateHoldProne();
        }
    }

    private void ToggleProne()
    {
        playerProperties.is_proned = !playerProperties.is_proned;

        if (playerProperties.is_proned)
        {
            if (playerProperties.sprinting)
            {
                ApplyProneTransitionImpulse();
            }

            playerProperties.sprinting = false;
            playerProperties.crouched = false;
        }
    }

    private void ApplyProneTransitionImpulse()
    {
        playerProperties.applyProneImpulse = true;
        playerProperties.proneImpulseLockTime = 0.25f;
    }

    private void UpdateHoldProne()
    {
        playerProperties.is_proned = Input.GetKey(keyBinds.PLAYER_proneKey);

        if (playerProperties.is_proned)
        {
            playerProperties.sprinting = false;
            playerProperties.crouched = false;
        }
    }

    private void HandleCrouch()
    {
        bool crouchInput = Input.GetKeyDown(keyBinds.PLAYER_crouchKey);
        bool jumpWhileCrouched = playerProperties.crouched &&
                                 Input.GetKeyDown(keyBinds.PLAYER_jumpKey);

        Vector3 origin_ = playerHead.transform.position;
        float distance = colliders_difference * 4.5f;

        if (!controls.is_crouch_on_hold && (crouchInput || jumpWhileCrouched))
        {
            if (playerProperties.is_proned)
            {
                origin_ = transform.position;
                distance = 3;
            }

            if (playerProperties.crouched || playerProperties.is_proned)
            {
                Debug.DrawLine(origin_, origin_ + Vector3.up * distance, Color.red, 2);

                if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer))
                {
                    generalHudAlertMessages.CreateMessage("Not Enough Space", 2);
                    return;
                }
            }

            ToggleCrouch();
        }
        else if (controls.is_crouch_on_hold)
        {
            if (playerProperties.is_proned)
            {
                origin_ = transform.position;
                distance = 3;
            }
            Debug.DrawLine(origin_, origin_ + Vector3.up * distance, Color.red, 2);

            if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer))
            {
                generalHudAlertMessages.CreateMessage("Not Enough Space", 2);
                return;
            }

            UpdateHoldCrouch();
        }
    }

    private void ToggleCrouch()
    {
        if (playerProperties.is_aiming) StartCoroutine(SwayNBob.CrouchWeaponShake());
        cameraShake.RequestShake(CameraShake.ShakeType.Crouch, 1);
        playerProperties.crouched = !playerProperties.crouched;

        if (playerProperties.crouched)
        {
            playerProperties.sprinting = false;
            playerProperties.is_proned = false;
        }
    }

    private void UpdateHoldCrouch()
    {
        playerProperties.crouched = Input.GetKey(keyBinds.PLAYER_crouchKey);

        if (playerProperties.crouched)
        {
            playerProperties.sprinting = false;
            playerProperties.is_proned = false;
        }
    }

    private void HandleRoll()
    {
        timeBetweenRolls -= Time.deltaTime;

        if (CanRoll())
        {
            CheckForDoubleClickRoll();
        }

        ClearOldClickCount();
    }

    private bool CanRoll()
    {
        return !playerProperties.is_proned &&
               !playerProperties.roll &&
               !playerProperties.is_reloading &&
               timeBetweenRolls <= 0;
    }

    private void CheckForDoubleClickRoll()
    {
        if (Input.GetKeyDown(keyBinds.PLAYER_rollKey))
        {
            float currentTime = Time.time;

            if (currentTime - lastClickTime <= DOUBLE_CLICK_TIME)
            {
                clickCount++;

                if (clickCount >= 2)
                {
                    ExecuteRoll();
                    clickCount = 0;
                }
            }
            else
            {
                clickCount = 1;
            }

            lastClickTime = currentTime;
        }
    }

    private void ExecuteRoll()
    {
        playerProperties.roll = true;
        timeBetweenRolls = 3f;
    }

    private void ClearOldClickCount()
    {
        if (Time.time - lastClickTime > DOUBLE_CLICK_TIME * 2)
        {
            clickCount = 0;
        }
    }

    #endregion

    #region Movement

    private void UpdateMovementSpeed()
    {
        if (playerProperties.roll) return;

        if (playerProperties.crouched || playerProperties.is_proned)
        {
            currentMoveSpeed = crouchSpeed;
        }
        else if (playerProperties.sprinting && !playerProperties.is_aiming &&
                 !playerProperties.is_proned && !playerProperties.isProneTransition)
        {
            currentMoveSpeed = sprintSpeed;
        }
        else
        {
            currentMoveSpeed = walkSpeed;
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * moveForward + orientation.right * moveHorizontal;

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            float state_multiplier = 10;

            if (!playerProperties.sprinting && !playerProperties.crouched && !playerProperties.is_proned)
            {
                state_multiplier = 18.5f;
            }
            else if (playerProperties.sprinting && !playerProperties.crouched && !playerProperties.is_proned)
            {
                state_multiplier = 12;
            }
            else if (!playerProperties.sprinting && playerProperties.crouched && !playerProperties.is_proned)
            {
                state_multiplier = 20;
            }

            rb.AddForce(GetSlopeMoveDirection() * currentMoveSpeed * state_multiplier * rb.mass, ForceMode.Force);


            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f * rb.mass, ForceMode.Force);
        }

        // on ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10 * rb.mass, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10 * airMultiplier * rb.mass, ForceMode.Force);

        // turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void FootstepSound()
    {
        if ((moveForward != 0 || moveHorizontal != 0) && !playerProperties.is_proned && !playerProperties.roll && grounded)
        {
            if (playerProperties.sprinting)
            {
                footstepSound_interval -= Time.deltaTime * 2f;
            }
            else if (playerProperties.crouched)
            {
                footstepSound_interval -= Time.deltaTime * 0.5f;
            }
            else
            {
                footstepSound_interval -= Time.deltaTime;
            }

            if (footstepSound_interval <= 0)
            {
                footstepSound.PlayStepSound();
                footstepSound_interval = original_footstepSound_interval;
            }
        }
        else
        {
            footstepSound_interval = original_footstepSound_interval;
        }
    }

    private void Jump()
    {
        cameraShake.RequestShake(CameraShake.ShakeType.Jump, 2);

        // Reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Apply jump force
        rb.AddForce(transform.up * jumpForce * rb.mass, ForceMode.Impulse);

        grounded = false;
        playerProperties.isGrounded = false;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    float raycast_distance = 0.1f;
    float raycast_distance_upper = 0.2f;
    private void stepClimb()
    {
        // Cores diferentes para cada raycast
        Color rayColorForward = Color.green;
        Color rayColor45 = Color.blue;
        Color rayColorMinus45 = Color.yellow;
        Color hitColor = Color.red;

        // Forward direction (0 graus)
        RaycastHit hitLower;
        Vector3 forwardDir = transform.TransformDirection(Vector3.forward);
        Debug.DrawRay(stepRayLower.transform.position, forwardDir * raycast_distance, rayColorForward);

        if (Physics.Raycast(stepRayLower.transform.position, forwardDir, out hitLower, raycast_distance, LayerMask.NameToLayer("Voxel")))
        {
            // Desenha o ponto de impacto
            Debug.DrawLine(stepRayLower.transform.position, hitLower.point, hitColor);
            Debug.DrawRay(hitLower.point, hitLower.normal * raycast_distance, Color.magenta);

            Vector3 upperStart = stepRayUpper.transform.position;
            Debug.DrawRay(upperStart, forwardDir * raycast_distance_upper, rayColorForward);

            RaycastHit hitUpper;
            if (!Physics.Raycast(upperStart, forwardDir, out hitUpper, raycast_distance_upper, LayerMask.NameToLayer("Voxel")))
            {
                // Desenha o movimento de subida
                Debug.DrawLine(stepRayLower.transform.position, stepRayLower.transform.position + Vector3.up * stepSmooth * Time.deltaTime, Color.white);
                rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
            else
            {
                // Se hitUpper detectou algo, desenha em vermelho
                Debug.DrawLine(upperStart, hitUpper.point, Color.red);
            }
        }

        // 45 graus direction
        RaycastHit hitLower45;
        Vector3 dir45 = transform.TransformDirection(1.5f, 0, 1).normalized;
        Debug.DrawRay(stepRayLower.transform.position, dir45 * raycast_distance, rayColor45);

        if (Physics.Raycast(stepRayLower.transform.position, dir45, out hitLower45, raycast_distance, LayerMask.NameToLayer("Voxel")))
        {
            Debug.DrawLine(stepRayLower.transform.position, hitLower45.point, hitColor);
            Debug.DrawRay(hitLower45.point, hitLower45.normal * raycast_distance, Color.magenta);

            Vector3 upperStart = stepRayUpper.transform.position;
            Debug.DrawRay(upperStart, dir45 * raycast_distance_upper, rayColor45);

            RaycastHit hitUpper45;
            if (!Physics.Raycast(upperStart, dir45, out hitUpper45, raycast_distance_upper, LayerMask.NameToLayer("Voxel")))
            {
                Debug.DrawLine(stepRayLower.transform.position, stepRayLower.transform.position + Vector3.up * stepSmooth * Time.deltaTime, Color.white);
                rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
            else
            {
                Debug.DrawLine(upperStart, hitUpper45.point, Color.red);
            }
        }

        // -45 graus direction
        RaycastHit hitLowerMinus45;
        Vector3 dirMinus45 = transform.TransformDirection(-1.5f, 0, 1).normalized;
        Debug.DrawRay(stepRayLower.transform.position, dirMinus45 * raycast_distance, rayColorMinus45);

        if (Physics.Raycast(stepRayLower.transform.position, dirMinus45, out hitLowerMinus45, raycast_distance, LayerMask.NameToLayer("Voxel")))
        {
            Debug.DrawLine(stepRayLower.transform.position, hitLowerMinus45.point, hitColor);
            Debug.DrawRay(hitLowerMinus45.point, hitLowerMinus45.normal * raycast_distance, Color.magenta);

            Vector3 upperStart = stepRayUpper.transform.position;
            Debug.DrawRay(upperStart, dirMinus45 * raycast_distance_upper, rayColorMinus45);

            RaycastHit hitUpperMinus45;
            if (!Physics.Raycast(upperStart, dirMinus45, out hitUpperMinus45, raycast_distance_upper, LayerMask.NameToLayer("Voxel")))
            {
                Debug.DrawLine(stepRayLower.transform.position, stepRayLower.transform.position + Vector3.up * stepSmooth * Time.deltaTime, Color.white);
                rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
            else
            {
                Debug.DrawLine(upperStart, hitUpperMinus45.point, Color.red);
            }
        }
    }

    #endregion

    #region Camera & Recoil

    private void HandleNightVision()
    {
        if (Input.GetKeyDown(keyBinds.PLAYER_activateNightNision))
        {
            is_night_vision_active = !is_night_vision_active;

            if (is_night_vision_active)
            {
                EnableNightVision();
            }
            else
            {
                DisableNightVision();
            }
        }
    }

    private void EnableNightVision()
    {
        filmGrain.active = true;
        colorAdjustments.active = true;
        vignette.active = true;
    }

    private void DisableNightVision()
    {
        filmGrain.active = false;
        colorAdjustments.active = false;
        vignette.active = false;
    }

    private void RotateCamera()
    {
        if (playerProperties.roll || settings.is_menu_settings_active || playerProperties.is_in_vehicle) return;

        HandleHorizontalRotation();
        HandleVerticalRotation();
        ApplyCameraRotation();
    }

    private void UpdateMouseSensitivity()
    {
        currentMouseSensitivity = playerProperties.is_aiming ? controls.infantary_aim_sensibility : controls.infantary_sensibility;
    }

    private void HandleHorizontalRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * currentMouseSensitivity;

        horizontalRecoilCurrent = Mathf.SmoothDamp(
            horizontalRecoilCurrent,
            horizontalRecoilTarget,
            ref horizontalRecoilVelocity,
            applyRecoilSpeed
        );

        yaw += mouseX + horizontalRecoilCurrent;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        horizontalRecoilTarget = 0f;
    }

    private void HandleVerticalRotation()
    {
        float mouseVertical = Input.GetAxis("Mouse Y") * currentMouseSensitivity;
        if (controls.invert_vertical_infantary_mouse)
        {
            mouseVertical *= -1;
        }

        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget,
                                                 ref recoilVerticalVelocity, applyRecoilSpeed);

        verticalRotation -= mouseVertical + recoilVerticalCurrent;
        verticalRotation = GetClampedVerticalRotation();

        recoilVerticalTarget = 0f;
    }

    private float GetClampedVerticalRotation()
    {
        if (playerProperties.is_proned)
        {
            return Mathf.Clamp(verticalRotation, -20f, 80f);
        }
        return Mathf.Clamp(verticalRotation, -80f, 70f);
    }

    private void ApplyCameraRotation()
    {
        UpdateRecoilZ();

        playerCamera.transform.localEulerAngles = new Vector3(
            verticalRotation,
            0,
            currentRecoilZ
        );

        UpdateHeadRotation();
    }

    private void UpdateRecoilZ()
    {
        float smoothedRecoilZ = Mathf.SmoothDamp(
            currentRecoilZ,
            targetRecoilZ,
            ref recoilZVelocity,
            applyRecoilSpeed
        );

        currentRecoilZ = smoothedRecoilZ;
    }

    private void UpdateHeadRotation()
    {
        Quaternion offset = Quaternion.Euler(-90, 0, 0);
        playerHead.transform.rotation = playerCamera.transform.rotation * offset;
    }

    private void UpdateRecoil()
    {
        if (Mathf.Abs(currentRecoilZ) > 0.01f)
        {
            currentRecoilZ = Mathf.SmoothDamp(
                currentRecoilZ,
                0f,
                ref recoilResetVelocity,
                resetRecoilSpeed
            );
        }
    }

    void UpdateFOV()
    {
        if (!playerProperties.is_aiming)
        {
            float lerpSpeed = 10f * Time.deltaTime;
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                video.infantary_fov,
                lerpSpeed
            );
        }
    }

    public void ApplyCameraRecoil(float verticalRecoil, float horizontalRecoil)
    {
        recoilVerticalTarget += verticalRecoil;
        horizontalRecoilTarget += horizontalRecoil;

        WeaponProperties weaponProperties = GetComponentInChildren<WeaponProperties>();
        if (weaponProperties != null)
        {
            float range = (weaponProperties.horizontal_recoil_media +
                          weaponProperties.vertical_recoil_media) * 2;
            currentRecoilZ = UnityEngine.Random.Range(-range, range);
            recoilResetVelocity = 0f;
        }
    }

    #endregion

    #region Interaction

    private void Interact()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Debug.DrawRay(origin, direction * INTERACT_DISTANCE, Color.red, 1f);

        TryInteractWithButton(origin, direction);
        if (!playerProperties.is_in_vehicle) TryInteractWithVehicle(origin, direction);
    }

    private void TryInteractWithButton(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, INTERACT_DISTANCE, LayerMask.GetMask("Interactives")))
        {
            Button button = hit.collider.GetComponent<Button>();
            button?.Interact();
        }
    }

    private void TryInteractWithVehicle(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, INTERACT_DISTANCE, LayerMask.GetMask("Vehicle")))
        {
            Vehicle vehicle = hit.collider.GetComponent<Vehicle>() ??
                             hit.collider.GetComponentInParent<Vehicle>();
            if (vehicle != null)
            {
                if (playerProperties.selected_class != ClassManager.Class.Pilot)
                {
                    generalHudAlertMessages.CreateMessage("Only the pilot Class can drive vehicles", 2);
                    return;
                }

                DisableNightVision();
                playerProperties.is_reloading = false;
                weapon.can_shoot = true;

                weapon.weaponProperties.weapon.transform.localPosition = weapon.weaponProperties.initial_potiion;
                weapon.weaponProperties.weapon.transform.localRotation = weapon.weaponProperties.inicial_rotation;
                weapon.weaponAnimation.FinishReloadAnimation();
                DisableColliders();
                HideOwnerItems(false);
                vehicle.EnterVehicle(gameObject);
            }
        }
    }

    #endregion

    #region State Management

    private void UpdateColliderState()
    {
        if (playerProperties.is_dead)
        {
            DisableColliders();
            return;
        }

        if (playerProperties.is_proned)
        {
            EnableProneCollider();
        }
        else if (playerProperties.crouched || playerProperties.roll)
        {
            EnableCrouchCollider();
        }
        else
        {
            EnableStandCollider();
        }
    }

    private void EnableStandCollider()
    {
        stand_collider.enabled = true;
        prone_collider.enabled = false;
        crouch_collider.enabled = false;
    }

    private void EnableCrouchCollider()
    {
        stand_collider.enabled = false;
        prone_collider.enabled = false;
        crouch_collider.enabled = true;
    }

    private void EnableProneCollider()
    {
        stand_collider.enabled = false;
        prone_collider.enabled = true;
        crouch_collider.enabled = false;
    }

    public void DisableColliders()
    {
        stand_collider.enabled = false;
        prone_collider.enabled = false;
        crouch_collider.enabled = false;
    }

    private void DisableDeathCollier()
    {
        deah_collider.enabled = false;
    }

    private void EnableDeathCollier()
    {
        DisableColliders();
        deah_collider.enabled = true;
    }

    private void HandleDeathState()
    {
        soldierHudManager.deadPlayerHud.gameObject.SetActive(true);
        HandleMecidProximity();
        death_timer += Time.deltaTime;

        float deathProgress = Mathf.Clamp01(death_timer / playerProperties.death_timer);

        if (deathProgress >= 1f)
        {
            vignette.intensity.value = 0;
            Destroy(gameObject);
            return;
        }

        if (vignette != null)
        {
            float maxVignetteIntensity = 1;
            vignette.intensity.value = deathProgress * maxVignetteIntensity;
        }

        HideOwnerItems(false);

        Quaternion targetRotation = new Quaternion(0, 0, 0, playerHead.transform.localRotation.w);
        playerHead.transform.localRotation = Quaternion.Lerp(
            playerHead.transform.localRotation,
            targetRotation,
            Time.deltaTime * 2
        );
    }

    private void HandleMecidProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 50, LayerMask.NameToLayer("Player"));

        List<PlayerInfo> jogadoresDetectados = new List<PlayerInfo>();

        foreach (Collider col in colliders)
        {
            PlayerProperties p = col.GetComponent<PlayerProperties>();

            if (p.selected_class == ClassManager.Class.Medic)
            {
                float distancia = Vector3.Distance(transform.position, col.transform.position);
                jogadoresDetectados.Add(new PlayerInfo(col.gameObject, p.player_name, distancia));
            }
        }

        jogadoresDetectados.Sort((a, b) => a.distance.CompareTo(b.distance));

        soldierHudManager.deadPlayerHud.UpdateCloseMedics(jogadoresDetectados);
    }

    public class PlayerInfo
    {
        public GameObject gameObject;
        public string player_name;
        public float distance;

        public PlayerInfo(GameObject go, string player_name, float distance)
        {
            this.player_name = player_name;
            gameObject = go;
            this.distance = distance;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 50);
    }

    private float altitude;

    private void UpdateGroundCheck()
    {
        bool is_holding_roll = Input.GetKey(keyBinds.PLAYER_rollKey);

        Vector3 rayOrigin = transform.position + Vector3.up;

        grounded = Physics.SphereCast(
            rayOrigin,
            stand_collider.radius,
            Vector3.down,
            out RaycastHit hitInfo,
            ground_detection_raycastDistance,
            groundLayer
        );

        playerProperties.isGrounded = grounded;

        if (wasGroundedLastFrame && !grounded)
        {
            altitude = transform.position.y;
        }

        if (!wasGroundedLastFrame && grounded)
        {
            footstepSound.PlayStepSound();
            float fall_damage = HandleFallDamage();

            if (is_holding_roll && fall_damage > 0)
            {
                fall_damage /= 2;
                ExecuteRoll();
            }

            if (fall_damage != 0) Damage(fall_damage);
            cameraShake.RequestShake(CameraShake.ShakeType.Jump, 2);
        }

        wasGroundedLastFrame = grounded;
    }

    private float HandleFallDamage()
    {
        float distance = altitude - transform.position.y;

        if (distance < 10) return 0;

        return distance * 2;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    #endregion

    #region Public Methods

    public void UpdateWeaponProperties(float speedModifier, float applyRecoilSpeed,
                                       float resetRecoilSpeed)
    {
        this.applyRecoilSpeed = applyRecoilSpeed;
        this.resetRecoilSpeed = resetRecoilSpeed;

        walkSpeed = original_walk_speed + speedModifier;
        sprintSpeed = original_sprint_speed + speedModifier;
        crouchSpeed = original_crouch_speed + speedModifier;
        UpdateMovementSpeed();
    }

    public float Damage(float damage)
    {
        float dmg = damage * ((100f - playerProperties.resistance) / 100f);
        playerProperties.hp -= dmg;

        cameraShake.RequestShake(CameraShake.ShakeType.Damage, dmg, 0.3f);
        if (dmg > 40) soldierHudManager.screenBlood.TriggerBlood();
        soldierHudManager.soldierHudHpManager.UpdateHp();

        if (playerProperties.hp <= 0)
        {
            playerProperties.hp = 0;
            playerProperties.is_dead = true;
            if (playerProperties.is_in_vehicle) playerProperties.is_in_vehicle = false;
            EnableDeathCollier();
        }
        return dmg;
    }

    private Coroutine current_DealDamageOverTime;

    public void DamageOverTime(float damage, float duration, float damage_rate)
    {
        if (current_DealDamageOverTime != null) current_DealDamageOverTime = StartCoroutine(DealDamageOverTime(damage, duration, damage_rate));
    }

    private IEnumerator DealDamageOverTime(float damage, float duration, float damage_rate)
    {
        float elapsedTime = 0f;
        float damage_timer = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            damage_timer += Time.deltaTime;

            if (damage_timer >= damage_rate)
            {
                Damage(damage);
                damage_timer = 0;
            }

            yield return null;
        }
        current_DealDamageOverTime = null;
    }

    public void Revive()
    {
        HideOwnerItems(true);
        if (vignette != null) vignette.intensity.value = 0;
        playerProperties.is_dead = false;
        playerProperties.hp = 100;
        soldierHudManager.soldierHudHpManager.UpdateHp();
        transform.rotation = new Quaternion(transform.rotation.z, transform.rotation.y, 0, transform.rotation.w);
        DisableDeathCollier();
    }

    public void Regenerate(float hp)
    {
        playerProperties.hp += hp;
        if (playerProperties.hp > playerProperties.max_hp)
        {
            playerProperties.hp = playerProperties.max_hp;
        }
        soldierHudManager.soldierHudHpManager.UpdateHp();
    }

    public void HideOwnerItems(bool hide)
    {
        foreach (MeshRenderer item in hideToOwnerItems)
        {
            if (item != null)
            {
                if (!hide)
                {
                    item.shadowCastingMode = ShadowCastingMode.On;
                }
                else
                {
                    item.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        if (!hide)
        {
            thirdPersonWeapon.ShowWeapon();
        }
        else
        {
            thirdPersonWeapon.HideWeapon();
        }
    }

    #endregion

    private void DrawWireSphere(Vector3 center, float radius, Color color)
    {
        int segments = 16;
        float angle = 0f;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            Vector3 start = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Vector3 end = center + Quaternion.Euler(0, angle + angleStep, 0) * Vector3.forward * radius;
            Debug.DrawLine(start, end, color);
            angle += angleStep;
        }

        angle = 0f;
        for (int i = 0; i < segments; i++)
        {
            Vector3 start = center + Quaternion.Euler(angle, 0, 0) * Vector3.forward * radius;
            Vector3 end = center + Quaternion.Euler(angle + angleStep, 0, 0) * Vector3.forward * radius;
            Debug.DrawLine(start, end, color);
            angle += angleStep;
        }
    }
}