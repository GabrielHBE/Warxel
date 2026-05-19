using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : NetworkBehaviour, ISspottable
{
    public static PlayerController Instance { get; private set; }

    #region Serialized Fields
    public Transform spot_position;
    [Header("Multiplayer / Player")]
    [SerializeField] private BoxCollider[] player_hit_colliders;
    [SerializeField] private AudioListener camera_audio;
    public GameObject first_person_player_components;
    [SerializeField] private MeshRenderer[] hideToOwnerItems;
    public GameObject[] body_parts;
    [SerializeField] private GameObject fist_person;
    [SerializeField] private GameObject firt_person_canvas;

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

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Ground Detection")]
    [SerializeField] private float ground_detection_raycastDistance;
    public LayerMask groundLayer;

    [Header("Private References")]
    [SerializeField] private WeaponIcon weaponIcon;
    [SerializeField] private float footstepSound_interval = 0.45f;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private FootstepSound footstepSound;
    [SerializeField] private Weapon weapon;
    public SoldierHudManager soldierHudManager;
    [SerializeField] private SwayNBobScript SwayNBob;
    [SerializeField] private ThirdPersonWeaponController thirdPersonWeapon;
    public PlayerProperties playerProperties;
    public PlayerAnimation playerAnimation;

    [Header("Volumes")]
    [SerializeField] private Volume nightVision_volume;
    [SerializeField] private Volume damageTaken_volume;

    #endregion

    #region Private Variables

    // Components
    public Rigidbody rb;

    // Volume
    private Vignette nightVision_vignette;
    private ColorAdjustments nightVision_Color_adjustments;
    private FilmGrain nightVision_filmGrain;
    private Vignette damageTaken_vignette;

    // Movement 
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
    private float altitude;
    private float cold_damage_timer = 0;
    private float damage_dealt;

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
    private float currentRecoilZ;
    private float recoilResetVelocity;
    private float targetRecoilZ;
    private float recoilZVelocity;
    private float resetRecoilSpeed;
    private float applyRecoilSpeed;
    private float targetVignetteIntensity;
    private float currentVignetteIntensity;
    private float vignetteVelocity;
    private float yaw;

    // Ground Check
    private bool wasGroundedLastFrame;
    private bool grounded;

    // Interaction & Caching
    public const float INTERACT_DISTANCE = 10f;
    private int interactivesLayer;
    private int vehicleLayer;
    private int playerLayer;

    // Performance: Array cache para Physics.OverlapSphereNonAlloc
    private Collider[] medicCollidersCache = new Collider[6];
    private Coroutine current_DealDamageOverTime;

    // Syncvars
    /*
    private readonly SyncVar<bool> is_dead = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private readonly SyncVar<bool> is_in_vehicle = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private readonly SyncVar<bool> is_proned = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private readonly SyncVar<bool> crouched = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    private readonly SyncVar<bool> roll = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    */

    private enum PlayerStance { Stand, Crouch, Prone, Disabled }
    private PlayerStance currentStance = PlayerStance.Stand;

    #endregion

    #region Unity Lifecycle

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            ConfigureOwner();
        }
        else
        {
            Destroy(fist_person);
            Destroy(firt_person_canvas);
        }
    }

    void Update()
    {

        if (!IsOwner) return;

        UpdateColliderStateLocal();

        // Se estiver em um veículo, roda a lógica do veículo e aborta o resto
        if (playerProperties.is_in_vehicle)
        {
            playerProperties.isGrounded = true;
            //if (first_person_player_components.activeSelf) first_person_player_components.SetActive(false);
            UpdateHeadRotation();
            return;
        }

        HandleDebugInput();
        UpdateDamageVignette();

        //if (!first_person_player_components.activeSelf) first_person_player_components.SetActive(true);

        FootstepSound();
        UpdateGroundCheck();
        UpdateFOV();

        if (playerProperties.is_dead.Value)
        {
            HandleDeathState();
            return;
        }

        if (soldierHudManager.deadPlayerHud.gameObject.activeSelf) soldierHudManager.deadPlayerHud.gameObject.SetActive(false);
        death_timer = 0;

        HandleInteractionInput();
        HandlePlayerInput();
        RotateCamera();
        UpdateRecoil();
        HandleJumpInput();
        HandleEnvironmentEffects();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (playerProperties.is_dead.Value || playerProperties.is_in_vehicle)
        {
            moveForward = 0;
            moveHorizontal = 0;
            return;
        }

        UpdateMouseSensitivity();
        MovePlayer();
        ApplyCustomGravity();
        ApplyWindPhysics();
    }

    #endregion

    #region Initialization

    [Client]
    public void ConfigureOwner()
    {

        Instance = this;
        HideOwnerItems(true);

        playerAnimation.can_update_animation = true;
        playerCamera.enabled = true;
        playerCamera.GetComponent<AudioListener>().enabled = true;
        soldierHudManager.hud.gameObject.SetActive(true);
        fist_person.SetActive(true);

        playerProperties.faction.Value = AccountManager.Instance.faction;
        playerProperties.selected_class = AccountManager.Instance.selected_class;

        foreach (BoxCollider c in player_hit_colliders)
        {
            c.enabled = false;
        }

        camera_audio.enabled = true;
        footstepSound_interval = footstepSound_interval <= 0 ? 0.45f : footstepSound_interval;
        original_footstepSound_interval = footstepSound_interval;

        colliders_difference = stand_collider.height - crouch_collider.height;
        original_sprint_speed = sprintSpeed;
        original_walk_speed = walkSpeed;
        original_crouch_speed = crouchSpeed;
        currentMoveSpeed = walkSpeed;

        // Caching das layers para performance
        interactivesLayer = LayerMask.GetMask("Interactives");
        vehicleLayer = LayerMask.GetMask("Vehicle");
        playerLayer = LayerMask.GetMask("Player");

        playerHead.GetComponentInChildren<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        InitializeVolume();

        readyToJump = true;

        StartCoroutine(weaponIcon.Initialize());
    }


    private void InitializeVolume()
    {
        if (nightVision_volume != null && nightVision_volume.profile != null)
        {
            nightVision_volume.profile.TryGet(out nightVision_vignette);
            nightVision_volume.profile.TryGet(out nightVision_filmGrain);
            nightVision_volume.profile.TryGet(out nightVision_Color_adjustments);
        }

        if (damageTaken_volume != null && damageTaken_volume.profile != null)
        {
            damageTaken_volume.profile.TryGet(out damageTaken_vignette);
        }
    }

    #endregion

    #region Input Handling

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.K)) Revive();
        if (Input.GetKeyDown(KeyCode.G)) RequestDamage(100);
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey))
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
            UpdateMovementInput();
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

    private void UpdateMovementInput()
    {
        moveHorizontal = 0;
        moveForward = 0;

        bool moveFwd = Input.GetKey(Settings.Instance._keybinds.PLAYER_moveFowardKey);
        bool moveBck = Input.GetKey(Settings.Instance._keybinds.PLAYER_moveBackwardsdKey);
        bool moveLft = Input.GetKey(Settings.Instance._keybinds.PLAYER_moveLeftKey);
        bool moveRgt = Input.GetKey(Settings.Instance._keybinds.PLAYER_moveRightKey);

        if ((moveFwd && moveBck) || SettingsHUD.Instance.is_menu_settings_active)
            moveForward = 0;
        else if (moveFwd) moveForward = 1;
        else if (moveBck) moveForward = -1;

        if ((moveLft && moveRgt) || SettingsHUD.Instance.is_menu_settings_active)
            moveHorizontal = 0;
        else if (moveLft) moveHorizontal = -1;
        else if (moveRgt) moveHorizontal = 1;
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_jumpKey) && readyToJump && grounded &&
            !playerProperties.is_proned && !playerProperties.crouched && !playerProperties.roll)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
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

        bool isSprintingKeyHit = Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_sprintKey);
        bool isSprintOnHold = Settings.Instance._controls.is_sprint_on_hold;

        if ((!isSprintOnHold && isSprintingKeyHit) || isSprintOnHold)
        {
            if (playerProperties.crouched)
            {
                if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer))
                {
                    GeneralHudAlertMessages.Instance.CreateMessage("Not Enough Space", 2);
                    return;
                }
            }

            if (isSprintOnHold) UpdateHoldSprint();
            else ToggleSprint();
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
        playerProperties.sprinting = Input.GetKey(Settings.Instance._keybinds.PLAYER_sprintKey);

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

        bool isProneKeyHit = Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_proneKey);
        bool isProneOnHold = Settings.Instance._controls.is_prone_on_hold;

        if ((!isProneOnHold && isProneKeyHit) || isProneOnHold)
        {
            if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer) && playerProperties.is_proned)
            {
                GeneralHudAlertMessages.Instance.CreateMessage("Not Enough Space", 2);
                return;
            }

            if (isProneOnHold) UpdateHoldProne();
            else ToggleProne();
        }
    }

    private void ToggleProne()
    {
        playerProperties.is_proned = !playerProperties.is_proned;

        if (playerProperties.is_proned)
        {
            if (playerProperties.sprinting) ApplyProneTransitionImpulse();

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
        playerProperties.is_proned = Input.GetKey(Settings.Instance._keybinds.PLAYER_proneKey);

        if (playerProperties.is_proned)
        {
            playerProperties.sprinting = false;
            playerProperties.crouched = false;
        }
    }

    private void HandleCrouch()
    {
        bool crouchInput = Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_crouchKey);
        bool jumpWhileCrouched = playerProperties.crouched && Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_jumpKey);
        bool isCrouchOnHold = Settings.Instance._controls.is_crouch_on_hold;

        Vector3 origin_ = playerProperties.is_proned ? transform.position : playerHead.transform.position;
        float distance = playerProperties.is_proned ? 3f : colliders_difference * 4.5f;

        if ((!isCrouchOnHold && (crouchInput || jumpWhileCrouched)) || isCrouchOnHold)
        {
            if (playerProperties.crouched || playerProperties.is_proned || isCrouchOnHold)
            {
                if (Physics.SphereCast(origin_, stand_collider.radius, Vector3.up, out RaycastHit hit, distance, groundLayer))
                {
                    GeneralHudAlertMessages.Instance.CreateMessage("Not Enough Space", 2);
                    return;
                }
            }

            if (isCrouchOnHold) UpdateHoldCrouch();
            else ToggleCrouch();
        }
    }

    private void ToggleCrouch()
    {
        if (playerProperties.is_aiming) StartCoroutine(SwayNBob.CrouchWeaponShake());
        cameraShake.RequestShake(0.8f, 0.2f);
        playerProperties.crouched = !playerProperties.crouched;

        if (playerProperties.crouched)
        {
            playerProperties.sprinting = false;
            playerProperties.is_proned = false;
        }
    }

    private void UpdateHoldCrouch()
    {
        playerProperties.crouched = Input.GetKey(Settings.Instance._keybinds.PLAYER_crouchKey);

        if (playerProperties.crouched)
        {
            playerProperties.sprinting = false;
            playerProperties.is_proned = false;
        }
    }

    private void HandleRoll()
    {
        timeBetweenRolls -= Time.deltaTime;

        if (CanRoll() && Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_rollKey))
        {
            ExecuteRoll();
        }
    }

    private bool CanRoll()
    {
        return !playerProperties.is_proned &&
               !playerProperties.roll &&
               !playerProperties.is_reloading &&
               timeBetweenRolls <= 0;
    }

    private void ExecuteRoll()
    {
        playerProperties.roll = true;
        timeBetweenRolls = 3f;
    }

    #endregion

    #region Movement & Physics

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
        moveDirection = orientation.forward * moveForward + orientation.right * moveHorizontal;

        if (OnSlope() && !exitingSlope)
        {
            float state_multiplier = 10;

            if (!playerProperties.sprinting && !playerProperties.crouched && !playerProperties.is_proned)
                state_multiplier = 18.5f;
            else if (playerProperties.sprinting && !playerProperties.crouched && !playerProperties.is_proned)
                state_multiplier = 12;
            else if (!playerProperties.sprinting && playerProperties.crouched && !playerProperties.is_proned)
                state_multiplier = 20;

            rb.AddForce(GetSlopeMoveDirection() * currentMoveSpeed * state_multiplier * rb.mass, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f * rb.mass, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10 * rb.mass, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10 * airMultiplier * rb.mass, ForceMode.Force);
        }

        rb.useGravity = !OnSlope();
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void ApplyWindPhysics()
    {
        if (WeatherStateManager.Instance != null &&
            WeatherStateManager.Instance.ActiveWeatherType.Value == WeatherStateManager.WeatherType.Hurricane)
        {
            rb.AddForce(Vector3.forward.normalized * 15f * rb.mass, ForceMode.Force);
        }
    }

    private void HandleEnvironmentEffects()
    {
        if (WeatherStateManager.Instance.ActiveWeatherType.Value == WeatherStateManager.WeatherType.Snow)
        {
            cold_damage_timer += Time.deltaTime;
            if (cold_damage_timer > 5)
            {
                cold_damage_timer = 0;
                RequestDamage(5);
            }
        }

        rb.linearDamping = grounded ? groundDrag : 1;
    }

    private void FootstepSound()
    {
        if ((moveForward != 0 || moveHorizontal != 0) && !playerProperties.is_proned && !playerProperties.roll && grounded)
        {
            if (playerProperties.sprinting) footstepSound_interval -= Time.deltaTime * 2f;
            else if (playerProperties.crouched) footstepSound_interval -= Time.deltaTime * 0.5f;
            else footstepSound_interval -= Time.deltaTime;

            if (footstepSound_interval <= 0)
            {
                footstepSound.CmdPlayStepSound();
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
        cameraShake.RequestShake(3, 0.15f);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce * rb.mass, ForceMode.Impulse);
        grounded = false;
        playerProperties.isGrounded = false;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void UpdateGroundCheck()
    {
        bool is_holding_roll = Input.GetKey(Settings.Instance._keybinds.PLAYER_rollKey);
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
            footstepSound.CmdPlayStepSound();
            float fall_damage = HandleFallDamage();

            if (is_holding_roll && fall_damage > 0)
            {
                fall_damage /= 2;
                ExecuteRoll();
            }

            if (fall_damage != 0) RequestDamage(fall_damage);
            cameraShake.RequestShake(3, 0.15f);
        }

        wasGroundedLastFrame = grounded;
    }

    private float HandleFallDamage()
    {
        float distance = altitude - transform.position.y;
        return distance < 10 ? 0 : distance * 2;
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

    #region Camera & Recoil

    private void HandleNightVision()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_activateNightNision))
        {
            is_night_vision_active = !is_night_vision_active;

            if (nightVision_filmGrain != null) nightVision_filmGrain.active = is_night_vision_active;
            if (nightVision_Color_adjustments != null) nightVision_Color_adjustments.active = is_night_vision_active;
            if (nightVision_vignette != null) nightVision_vignette.active = is_night_vision_active;
        }
    }

    private void UpdateDamageVignette()
    {
        if (damageTaken_vignette == null) return;

        float hpPercentage = playerProperties.hp.Value / playerProperties.max_hp;
        targetVignetteIntensity = 1f - hpPercentage;

        currentVignetteIntensity = Mathf.SmoothDamp(
            currentVignetteIntensity,
            targetVignetteIntensity,
            ref vignetteVelocity,
            0.2f
        );

        damageTaken_vignette.intensity.value = currentVignetteIntensity;
    }

    private void RotateCamera()
    {
        if (playerProperties.roll || SettingsHUD.Instance.is_menu_settings_active || playerProperties.is_in_vehicle) return;

        HandleHorizontalRotation();
        HandleVerticalRotation();
        ApplyCameraRotation();
    }

    private void UpdateMouseSensitivity()
    {
        currentMouseSensitivity = playerProperties.is_aiming ?
            Settings.Instance._controls.infantary_aim_sensibility :
            Settings.Instance._controls.infantary_sensibility;
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
        if (Settings.Instance._controls.invert_vertical_infantary_mouse)
        {
            mouseVertical *= -1;
        }

        recoilVerticalCurrent = Mathf.SmoothDamp(recoilVerticalCurrent, recoilVerticalTarget, ref recoilVerticalVelocity, applyRecoilSpeed);

        verticalRotation -= mouseVertical + recoilVerticalCurrent;
        verticalRotation = playerProperties.is_proned ? Mathf.Clamp(verticalRotation, -20f, 80f) : Mathf.Clamp(verticalRotation, -80f, 70f);

        recoilVerticalTarget = 0f;
    }

    private void ApplyCameraRotation()
    {
        currentRecoilZ = Mathf.SmoothDamp(currentRecoilZ, targetRecoilZ, ref recoilZVelocity, applyRecoilSpeed);

        playerCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0, currentRecoilZ);
        UpdateHeadRotation();
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
            currentRecoilZ = Mathf.SmoothDamp(currentRecoilZ, 0f, ref recoilResetVelocity, resetRecoilSpeed);
        }
    }

    void UpdateFOV()
    {
        if (!playerProperties.is_aiming)
        {
            float targetFov = Settings.Instance._video.infantary_fov;
            // Só interpola se houver diferença considerável para economizar cálculos do renderizador
            if (Mathf.Abs(playerCamera.fieldOfView - targetFov) > 0.1f)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 10f * Time.deltaTime);
            }
        }
    }

    public void ApplyCameraRecoil(float verticalRecoil, float horizontalRecoil)
    {
        recoilVerticalTarget += verticalRecoil;
        horizontalRecoilTarget += horizontalRecoil;

        // Otimização: Acesso direto à propriedade em cache ao invés de buscar via GetComponent todo frame
        if (weapon != null && weapon.weaponProperties != null)
        {
            float range = (weapon.weaponProperties.horizontal_recoil_media + weapon.weaponProperties.vertical_recoil_media) * 2;
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
        if (Physics.Raycast(origin, direction, out RaycastHit hit, INTERACT_DISTANCE, interactivesLayer))
        {
            Button button = hit.collider.GetComponent<Button>();
            button?.Interact();
        }
    }

    private void TryInteractWithVehicle(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, INTERACT_DISTANCE, vehicleLayer))
        {
            Vehicle vehicle = hit.collider.GetComponent<Vehicle>() ?? hit.collider.GetComponentInParent<Vehicle>();

            if (vehicle != null)
            {
                if (playerProperties.selected_class != ClassManager.Class.Pilot)
                {
                    GeneralHudAlertMessages.Instance.CreateMessage("Only the pilot Class can drive vehicles", 2);
                    return;
                }

                InteractWithVehicle(vehicle);
            }
        }
    }

    public void InteractWithVehicle(Vehicle vehicle)
    {
        if (gameObject == null || !gameObject.activeSelf) return;

        if (is_night_vision_active)
        {
            is_night_vision_active = false;
            if (nightVision_filmGrain != null) nightVision_filmGrain.active = false;
            if (nightVision_Color_adjustments != null) nightVision_Color_adjustments.active = false;
            if (nightVision_vignette != null) nightVision_vignette.active = false;
        }

        if (weapon.weaponProperties != null)
        {
            weapon.can_shoot = true;
            weapon.weaponProperties.weapon.transform.localPosition = weapon.weaponProperties.initial_potiion;
            weapon.weaponProperties.weapon.transform.localRotation = weapon.weaponProperties.inicial_rotation;
            weapon.weaponAnimation.FinishReloadAnimation();

        }

        RequestEnterVehicle(vehicle, gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterVehicle(Vehicle vehicle, GameObject player)
    {
        if (vehicle == null || player == null || !vehicle.IsSpawned || !player.activeInHierarchy) return;

        //vehicle.NetworkObject.GiveOwnership(Owner);
        vehicle.EnterVehicle(Owner, player);
    }

    #endregion

    #region State Management

    private void UpdateColliderStateLocal()
    {
        // 1. Define qual deve ser a postura (estado) alvo neste frame
        PlayerStance targetStance;

        if (playerProperties.is_dead.Value || playerProperties.is_in_vehicle)
            targetStance = PlayerStance.Disabled;
        else if (playerProperties.is_proned)
            targetStance = PlayerStance.Prone;
        else if (playerProperties.crouched || playerProperties.roll)
            targetStance = PlayerStance.Crouch;
        else
            targetStance = PlayerStance.Stand;

        // 2. Só executa a troca e o RPC se o estado alvo for diferente do atual
        if (targetStance != currentStance)
        {
            // Aplica as mudanças locais baseadas no novo estado
            if (targetStance == PlayerStance.Disabled)
                DisableColliders();
            else if (targetStance == PlayerStance.Prone)
                EnableProneCollider();
            else if (targetStance == PlayerStance.Crouch)
                EnableCrouchCollider();
            else
                EnableStandCollider();

            // Salva o novo estado para o próximo frame
            currentStance = targetStance;

            // Envia para a rede APENAS no frame em que ocorreu a transição!
            CmdUpdateColliderStateRemote(targetStance);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdUpdateColliderStateRemote(PlayerStance playerStance)
    {
        RpcUpdateColliderStateRemote(playerStance);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcUpdateColliderStateRemote(PlayerStance playerStance)
    {
        switch (playerStance)
        {
            case PlayerStance.Disabled:
                DisableColliders();
                break;
            case PlayerStance.Prone:
                EnableProneCollider();
                break;
            case PlayerStance.Crouch:
                EnableCrouchCollider();
                break;
            case PlayerStance.Stand:
                EnableStandCollider();
                break;
        }

    }

    private void EnableStandCollider()
    {
        if (!stand_collider.enabled) stand_collider.enabled = true;
        if (prone_collider.enabled) prone_collider.enabled = false;
        if (crouch_collider.enabled) crouch_collider.enabled = false;
    }

    private void EnableCrouchCollider()
    {
        if (stand_collider.enabled) stand_collider.enabled = false;
        if (prone_collider.enabled) prone_collider.enabled = false;
        if (!crouch_collider.enabled) crouch_collider.enabled = true;
    }

    private void EnableProneCollider()
    {
        if (stand_collider.enabled) stand_collider.enabled = false;
        if (!prone_collider.enabled) prone_collider.enabled = true;
        if (crouch_collider.enabled) crouch_collider.enabled = false;
    }

    public void DisableColliders()
    {
        if (stand_collider.enabled) stand_collider.enabled = false;
        if (prone_collider.enabled) prone_collider.enabled = false;
        if (crouch_collider.enabled) crouch_collider.enabled = false;
    }

    private void DisableDeathCollier()
    {
        if (deah_collider.enabled) deah_collider.enabled = false;
    }

    private void EnableDeathCollier()
    {
        DisableColliders();
        if (!deah_collider.enabled) deah_collider.enabled = true;
    }

    [Client]
    public void SetCollidersState(bool enabled)
    {
        if (stand_collider != null) stand_collider.enabled = enabled;
        if (crouch_collider != null) crouch_collider.enabled = enabled;
        if (prone_collider != null) prone_collider.enabled = enabled;

        if (deah_collider != null && !enabled)
        {
            deah_collider.enabled = false;
        }
    }

    private void HandleDeathState()
    {
        if (!soldierHudManager.deadPlayerHud.gameObject.activeSelf) soldierHudManager.deadPlayerHud.gameObject.SetActive(true);

        HandleMecidProximity();
        death_timer += Time.deltaTime;

        float deathProgress = Mathf.Clamp01(death_timer / playerProperties.death_timer);

        if (deathProgress >= 1)
        {
            AccountManager.Instance.status.AddDeath();
            AccountManager.Instance.RemoveBattleCoin(10);
            PlayerSpawnController.Instance.Reestart();

            if (nightVision_vignette != null) nightVision_vignette.intensity.value = 0;

            if (IsSpawned) RequestDespawn();
            else Destroy(gameObject);

            return;
        }

        if (nightVision_vignette != null)
        {
            nightVision_vignette.intensity.value = deathProgress;
        }

        HideOwnerItems(false);

        Quaternion targetRotation = new Quaternion(0, 0, 0, playerHead.transform.localRotation.w);
        playerHead.transform.localRotation = Quaternion.Lerp(playerHead.transform.localRotation, targetRotation, Time.deltaTime * 2);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestDespawn()
    {
        Despawn(gameObject);
    }

    private void HandleMecidProximity()
    {
        // Otimização: Uso de NonAlloc para evitar geração excessiva de lixo no GC a cada frame
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 50f, medicCollidersCache, playerLayer);

        List<PlayerInfo> jogadoresDetectados = new List<PlayerInfo>(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            PlayerProperties p = medicCollidersCache[i].GetComponent<PlayerProperties>();

            if (p != null && p.selected_class == ClassManager.Class.Medic)
            {
                float distancia = Vector3.Distance(transform.position, medicCollidersCache[i].transform.position);
                jogadoresDetectados.Add(new PlayerInfo(medicCollidersCache[i].gameObject, p.player_name.Value, distancia));
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

    [ServerRpc(RequireOwnership = true)]
    private void CmdUpdateServerHP(float hp)
    {
        playerProperties.hp.Value = hp;
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdUpdateServerIsDead(bool is_dead)
    {
        playerProperties.is_dead.Value = is_dead;
    }

    #endregion

    #region Public Methods

    public float GetDamageDealt()
    {
        return damage_dealt;
    }

    public void UpdateWeaponProperties(float speedModifier, float applyRecoilSpeed, float resetRecoilSpeed)
    {
        this.applyRecoilSpeed = applyRecoilSpeed;
        this.resetRecoilSpeed = resetRecoilSpeed;

        walkSpeed = original_walk_speed + speedModifier;
        sprintSpeed = original_sprint_speed + speedModifier;
        crouchSpeed = original_crouch_speed + speedModifier;
        UpdateMovementSpeed();
    }

    public void RequestDamage(float rawDamage, string player_who_dealt_damage = null)
    {
        if (playerProperties.is_dead.Value) return;
        CmdApplyDamage(rawDamage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdApplyDamage(float rawDamage)
    {
        TargetReceiveDamage(Owner, rawDamage);
    }

    [TargetRpc]
    private void TargetReceiveDamage(NetworkConnection conn, float dmg)
    {
        damage_dealt = dmg * ((100f - playerProperties.resistance.Value) / 100f);
        playerProperties.hp.Value -= damage_dealt;

        CmdUpdateServerHP(playerProperties.hp.Value);

        cameraShake.RequestShake(damage_dealt / 2, 0.1f);
        if (damage_dealt > 40) soldierHudManager.screenBlood.TriggerBlood();
        soldierHudManager.soldierHudHpManager.UpdateHp();

        if (playerProperties.hp.Value <= 0)
        {
            if (playerProperties.is_in_vehicle) playerProperties.is_in_vehicle = false;
            playerProperties.hp.Value = 0;
            playerProperties.is_dead.Value = true;

            CmdUpdateServerHP(0);
            CmdUpdateServerIsDead(true);
            EnableDeathCollier();
        }
    }

    public void DamageOverTime(float damage, float duration, float damage_rate)
    {
        if (current_DealDamageOverTime != null) return;
        current_DealDamageOverTime = StartCoroutine(DealDamageOverTime(damage, duration, damage_rate));
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
                RequestDamage(damage);
                damage_timer = 0;
            }

            yield return null;
        }
        current_DealDamageOverTime = null;
    }

    public void Revive()
    {
        CmdUpdateServerHP(100);
        CmdUpdateServerIsDead(false);
        HideOwnerItems(true);

        if (nightVision_vignette != null) nightVision_vignette.intensity.value = 0;

        playerProperties.is_dead.Value = false;
        playerProperties.hp.Value = 100;

        soldierHudManager.soldierHudHpManager.UpdateHp();
        transform.rotation = new Quaternion(transform.rotation.z, transform.rotation.y, 0, transform.rotation.w);
        DisableDeathCollier();
    }

    public void Regenerate(float hp)
    {
        playerProperties.hp.Value += hp;
        if (playerProperties.hp.Value > playerProperties.max_hp)
        {
            playerProperties.hp.Value = playerProperties.max_hp;
        }
        soldierHudManager.soldierHudHpManager.UpdateHp();
    }

    [Client]
    public void HideOwnerItems(bool hide)
    {
        if (!IsOwner)
        {
            thirdPersonWeapon.ShowWeapon();
            return;
        }

        foreach (MeshRenderer item in hideToOwnerItems)
        {
            if (item != null)
            {
                item.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }

        if (!hide) thirdPersonWeapon.ShowWeapon();
        else thirdPersonWeapon.HideWeapon();
    }

    public float GetHp() => playerProperties.hp.Value;
    public float GetResistance() => playerProperties.resistance.Value;
    public bool IsPlayerDead() => playerProperties.is_dead.Value;

    public FactionManager.Faction GetFaction()
    {
        return playerProperties.faction.Value;
    }

    public Transform GetSpotPosition()
    {
        return spot_position;
    }

    #endregion
}