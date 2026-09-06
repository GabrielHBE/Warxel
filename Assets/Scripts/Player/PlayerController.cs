using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : ServerSingleton<PlayerController>, ISspottable, EntityFaction, IDamageable
{
    #region Serialized Fields
    public Transform spot_position;
    [Header("Multiplayer / Player")]
    [SerializeField] private AudioListener camera_audio;
    public GameObject first_person_player_components;
    [SerializeField] private GameObject fist_person;

    [Header("Body")]
    public GameObject playerHead;
    
    [Header("Colliders")]
    public CapsuleCollider stand_collider;
    public CapsuleCollider crouch_collider;
    public CapsuleCollider prone_collider;

    [Header("Camera Settings")]
    public Camera playerCamera;
    [SerializeField] private ProcessCameraRecoil processCameraRecoil;
    [SerializeField] private CameraRotation cameraRotation;

    [Header("Movement")]
    public Transform orientation;
    [SerializeField] private float walkSpeed = 14f;
    [SerializeField] private float sprintSpeed = 14f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Movement Actions")]
    [SerializeField] private float timeBetweenRolls = 2f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [HideInInspector] public float moveForward;
    [HideInInspector] public float moveHorizontal;

    [Header("Ground Detection")]
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.18f;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.65f;
    public LayerMask groundLayer;

    [Header("Voxel Step Climbing")]
    [SerializeField] private bool enableVoxelStepClimbing = true;
    [Tooltip("Maximum height in world units. The project's standard voxel is 1 unit tall.")]
    [SerializeField, Min(0.01f)] private float maxStepHeight = 1.05f;
    [SerializeField, Min(0.01f)] private float stepSearchDistance = 0.35f;
    [SerializeField, Min(0.01f)] private float stepUpSpeed = 8f;
    [SerializeField, Min(0f)] private float stepForwardAssistSpeed = 2f;
    [SerializeField, Min(0.001f)] private float stepLandingInset = 0.08f;
    [SerializeField, Min(0.001f)] private float collisionSkin = 0.03f;
    [Tooltip("Layers that can be climbed. When empty, only the Voxel layer is used.")]
    [SerializeField] private LayerMask voxelStepLayer;
    [Tooltip("Solid layers used to validate overhead and body clearance while climbing.")]
    [SerializeField] private LayerMask stepClearanceLayer = ~0;

    [Header("Private References")]
    [SerializeField] private SwitchWeapon switchWeapon;
    [SerializeField] private WeaponIcon weaponIcon;
    [SerializeField] private float footstepSound_interval = 0.45f;
    public CameraShake cameraShake;
    [SerializeField] private FootstepSound footstepSound;
    [SerializeField] private Weapon weapon;
    public SoldierHudManager soldierHudManager;
    [SerializeField] private SwayNBobScript SwayNBob;
    [SerializeField] private ThirdPersonWeaponController thirdPersonWeapon;
    [SerializeField] private SkinApplier skinApplier;
    public PlayerProperties playerProperties;

    [Header("Volumes")]
    [SerializeField] private NightVisionPostFX nightVisionPostFX;
    [SerializeField] private Volume damageTaken_volume;
    #endregion

    #region Private Variables
    public Rigidbody rb;

    private Vignette damageTaken_vignette;

    //Consts
    private const float groundDrag = 5f;
    private const float jumpForce = 8f;
    private const float jumpCooldown = 0.25f;
    private const float airMultiplier = 0.4f;

    // Movement 
    [HideInInspector] public float currentMoveSpeed;
    private float original_sprint_speed;
    private float original_walk_speed;
    private float original_crouch_speed;
    private bool readyToJump;
    private bool jumpRequested;
    private Vector3 moveDirection;

    // Voxel step state.
    private bool isStepping;
    private float stepTargetRootY;
    private float stepStartTime;
    private float stepLastRootY;
    private int stalledStepTicks;
    private Vector3 stepDirection;
    private Vector3 stepLandingPoint;
    private CapsuleCollider stepCollider;

    // Legacy
    private float original_footstepSound_interval;
    private float deathTimer;
    private float altitude;
    private float cold_damage_timer = 0;

    // Camera Settings & FX
    private bool is_night_vision_active = false;
    private float targetVignetteIntensity;
    private float currentVignetteIntensity;
    private float vignetteVelocity;

    // Ground Check
    private bool wasGroundedLastFrame;
    private bool hasGroundContact;
    private bool grounded;

    // Interaction & Caching
    private int interactivesLayer;
    private int playerLayer;

    private Collider[] medicCollidersCache = new Collider[6];
    private readonly RaycastHit[] groundCastHitsCache = new RaycastHit[12];
    private readonly Collider[] stanceOverlapCache = new Collider[16];

    private enum PlayerStance { Stand, Crouch, Prone, Disabled }
    private PlayerStance currentStance = PlayerStance.Disabled;

    private readonly struct CapsuleGeometry
    {
        public readonly Vector3 PointA;
        public readonly Vector3 PointB;
        public readonly float Radius;
        public readonly float FootY;

        public CapsuleGeometry(Vector3 pointA, Vector3 pointB, float radius)
        {
            PointA = pointA;
            PointB = pointB;
            Radius = radius;
            FootY = Mathf.Min(pointA.y, pointB.y) - radius;
        }

        public Vector3 Center => (PointA + PointB) * 0.5f;
    }
    #endregion

    #region Unity Lifecycle
    public override void OnStartClient()
    {
        base.OnStartClient();

        NormalizeStanceColliders();

        if (IsOwner) ConfigureOwner();
        else
        {
            Destroy(fist_person);
            Destroy(soldierHudManager.gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NormalizeStanceColliders();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (playerProperties.isInVehicle)
        {
            UpdateColliderStateLocal();
            CancelVoxelStep();
            playerProperties.grounded = true;
            return;
        }

        UpdateDamageVignette();

        FootstepSound();

        if (playerProperties.isDead.Value)
        {
            UpdateColliderStateLocal();
            CancelVoxelStep();
            HandleDeathState();
            return;
        }

        deathTimer = 0;

        HandleInteractionInputManager();
        HandlePlayerInputManager();
        HandleJumpInputManager();
        UpdateColliderStateLocal();
        HandleEnvironmentEffects();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (playerProperties.isDead.Value || playerProperties.isInVehicle)
        {
            moveForward = 0;
            moveHorizontal = 0;
            jumpRequested = false;
            CancelVoxelStep();
            return;
        }

        UpdateGroundCheck();
        ProcessJumpRequest();
        UpdateMovementSpeed();
        UpdateMovementDirection();
        UpdateVoxelStep();
        ApplyMovementForce();

        if (!isStepping) ApplyCustomGravity();

        ApplyWindPhysics();
        UpdateRigidbodyDamping();
    }
    #endregion

    #region Initialization
    public void ConfigureOwner()
    {
        skinApplier.ApplySkin(this);

        soldierHudManager.ActivateStandardHUD();

        SetInstance();

        playerCamera.enabled = true;
        playerCamera.GetComponent<AudioListener>().enabled = true;
        soldierHudManager.hud.gameObject.SetActive(true);
        fist_person.SetActive(true);

        camera_audio.enabled = true;
        footstepSound_interval = footstepSound_interval <= 0 ? 0.45f : footstepSound_interval;
        original_footstepSound_interval = footstepSound_interval;

        original_sprint_speed = sprintSpeed;
        original_walk_speed = walkSpeed;
        original_crouch_speed = crouchSpeed;
        currentMoveSpeed = walkSpeed;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        interactivesLayer = LayerMask.GetMask("Interactives");
        playerLayer = LayerMask.GetMask("Player");

        InitializeVolume();
        readyToJump = true;

        StartCoroutine(weaponIcon.Initialize());

        HideOwnerItems(true);
    }

    private void InitializeVolume()
    {
        if (damageTaken_volume != null && damageTaken_volume.profile != null) damageTaken_volume.profile.TryGet(out damageTaken_vignette);
    }
    #endregion

    #region InputManager Handling
    private void HandleInteractionInputManager()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey)) Interact();
    }

    private void HandlePlayerInputManager()
    {
        if (playerProperties.roll)
        {
            moveForward = 1;
            moveHorizontal = 0;
        }
        else UpdateMovementInputManager();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_activateNightNision)) HandleNightVision();

        if (hasGroundContact) HandleRoll();

        HandleSprint();
        HandleCrouch();
        HandleProne();
        UpdateMovementSpeed();
    }

    private void UpdateMovementInputManager()
    {
        moveHorizontal = 0;
        moveForward = 0;

        bool moveFwd = InputManager.GetKey(Settings.Instance._keybinds.PLAYER_moveFowardKey);
        bool moveBck = InputManager.GetKey(Settings.Instance._keybinds.PLAYER_moveBackwardsdKey);
        bool moveLft = InputManager.GetKey(Settings.Instance._keybinds.PLAYER_moveLeftKey);
        bool moveRgt = InputManager.GetKey(Settings.Instance._keybinds.PLAYER_moveRightKey);

        if (moveFwd && moveBck)
            moveForward = 0;
        else if (moveFwd) moveForward = 1;
        else if (moveBck) moveForward = -1;

        if (moveLft && moveRgt)
            moveHorizontal = 0;
        else if (moveLft) moveHorizontal = -1;
        else if (moveRgt) moveHorizontal = 1;
    }

    private void HandleJumpInputManager()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_jumpKey)) jumpRequested = true;
    }

    private void HandleSprint()
    {
        if (playerProperties.isProneTransition) return;

        if ((moveForward == 0 && moveHorizontal == 0) || playerProperties.firing)
        {
            playerProperties.sprinting = false;
            return;
        }

        KeyCode sprintKey = Settings.Instance._keybinds.PLAYER_sprintKey;
        bool sprintOnHold = Settings.Instance._controls.is_sprint_on_hold;
        bool sprintInput = sprintOnHold ? InputManager.GetKey(sprintKey) : InputManager.GetKeyDown(sprintKey);

        if (!sprintInput)
        {
            if (sprintOnHold) playerProperties.sprinting = false;
            return;
        }

        bool wantsToSprint = sprintOnHold || !playerProperties.sprinting;
        if (wantsToSprint && !TryPrepareStandingStance()) return;

        playerProperties.sprinting = wantsToSprint;
    }

    private void HandleProne()
    {
        KeyCode proneKey = Settings.Instance._keybinds.PLAYER_proneKey;
        bool proneOnHold = Settings.Instance._controls.is_prone_on_hold;
        bool proneInput = proneOnHold ? InputManager.GetKey(proneKey) : InputManager.GetKeyDown(proneKey);

        if (!proneInput)
        {
            if (proneOnHold && playerProperties.proned) TrySetProne(false);
            return;
        }

        bool wantsToProne = proneOnHold || !playerProperties.proned;
        TrySetProne(wantsToProne);
    }

    private void ApplyProneTransitionImpulse()
    {
        playerProperties.applyProneImpulse = true;
        playerProperties.proneImpulseLockTime = 0.25f;
    }

    private bool TrySetProne(bool wantsToProne)
    {
        if (wantsToProne == playerProperties.proned) return true;

        if (wantsToProne)
        {
            if (isStepping || !hasGroundContact || !HasClearanceForCollider(prone_collider)) return false;

            if (playerProperties.sprinting) ApplyProneTransitionImpulse();

            playerProperties.proned = true;
            playerProperties.sprinting = false;
            playerProperties.crouched = false;
            return true;
        }

        if (!HasClearanceForCollider(stand_collider))
        {
            ShowNotEnoughSpaceMessage();
            return false;
        }

        playerProperties.proned = false;
        return true;
    }

    private void HandleCrouch()
    {
        KeyCode crouchKey = Settings.Instance._keybinds.PLAYER_crouchKey;
        bool crouchOnHold = Settings.Instance._controls.is_crouch_on_hold;
        bool jumpWhileCrouched = playerProperties.crouched && InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_jumpKey);
        bool crouchInput = crouchOnHold ? InputManager.GetKey(crouchKey) : InputManager.GetKeyDown(crouchKey) || jumpWhileCrouched;

        if (!crouchInput)
        {
            if (crouchOnHold && playerProperties.crouched) TrySetCrouched(false);
            return;
        }

        bool wantsToCrouch = crouchOnHold || !playerProperties.crouched;
        TrySetCrouched(wantsToCrouch);
    }

    private bool TrySetCrouched(bool wantsToCrouch)
    {
        if (wantsToCrouch == playerProperties.crouched && !playerProperties.proned) return true;

        CapsuleCollider targetCollider = wantsToCrouch ? crouch_collider : stand_collider;
        if ((wantsToCrouch && (isStepping || !hasGroundContact)) || !HasClearanceForCollider(targetCollider))
        {
            if (!wantsToCrouch || playerProperties.proned) ShowNotEnoughSpaceMessage();
            return false;
        }

        playerProperties.crouched = wantsToCrouch;

        if (wantsToCrouch)
        {
            if (playerProperties.aiming && SwayNBob != null) StartCoroutine(SwayNBob.CrouchWeaponShake());
            if (cameraShake != null) cameraShake.RequestShake(0.8f, 0.2f);

            playerProperties.sprinting = false;
            playerProperties.proned = false;
        }

        return true;
    }

    private void HandleRoll()
    {
        timeBetweenRolls -= Time.deltaTime;
        if (CanRoll() && InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_rollKey)) ExecuteRoll();
    }

    private bool CanRoll() => !playerProperties.proned && !playerProperties.roll && !isStepping && !playerProperties.reloading && timeBetweenRolls <= 0;

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

        if (playerProperties.crouched || playerProperties.proned) currentMoveSpeed = crouchSpeed;
        else if (playerProperties.sprinting && !playerProperties.aiming && !playerProperties.proned && !playerProperties.isProneTransition) currentMoveSpeed = sprintSpeed;
        else currentMoveSpeed = walkSpeed;
    }

    private void UpdateMovementDirection()
    {
        Transform movementReference = orientation != null ? orientation : transform;
        moveDirection = movementReference.forward * moveForward + movementReference.right * moveHorizontal;
    }

    private void ApplyMovementForce()
    {
        // Calcula a velocidade alvo nos eixos X e Z (ignorando o Y para não quebrar a gravidade/pulo)
        Vector3 targetVelocity = moveDirection.normalized * currentMoveSpeed;

        // Isola a velocidade horizontal atual
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Calcula exatamente o quanto de velocidade falta para atingir o movimento desejado
        Vector3 velocityDifference = targetVelocity - currentVelocity;

        float controlMultiplier = grounded || isStepping ? 1f : airMultiplier;

        // Aplica apenas a diferença, resultando em uma aceleração suave e controlada
        rb.AddForce(velocityDifference * controlMultiplier, ForceMode.VelocityChange);
    }
    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0) rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
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
        if (WeatherStateManager.Instance != null &&
            WeatherStateManager.Instance.ActiveWeatherType.Value == WeatherStateManager.WeatherType.Snow)
        {
            cold_damage_timer += Time.deltaTime;
            if (cold_damage_timer > 5)
            {
                cold_damage_timer = 0;
                TakeDamage(5);
            }
        }
    }

    private void UpdateRigidbodyDamping() => rb.linearDamping = grounded || isStepping ? groundDrag : 1f;

    private void FootstepSound()
    {
        if ((moveForward != 0 || moveHorizontal != 0) && !playerProperties.proned && !playerProperties.roll && grounded)
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
        CancelVoxelStep();
        cameraShake.RequestShake(3, 0.15f);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce * rb.mass, ForceMode.Impulse);
        grounded = false;
        playerProperties.grounded = false;
    }

    private void ProcessJumpRequest()
    {
        if (!jumpRequested) return;

        jumpRequested = false;

        if (!readyToJump || !hasGroundContact || isStepping || playerProperties.proned ||
            playerProperties.crouched || playerProperties.roll)
        {
            return;
        }

        readyToJump = false;
        Jump();
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump() => readyToJump = true;

    private void UpdateGroundCheck()
    {
        bool is_holding_roll = InputManager.GetKey(Settings.Instance._keybinds.PLAYER_rollKey);
        CapsuleCollider activeCollider = GetActiveCapsuleCollider();
        bool detectedGround = false;

        if (activeCollider != null)
        {
            CapsuleGeometry capsule = GetCapsuleGeometry(activeCollider);
            float probeRadius = Mathf.Max(0.01f, capsule.Radius - collisionSkin);
            Vector3 probeOrigin = new Vector3(capsule.Center.x, capsule.FootY + probeRadius + collisionSkin, capsule.Center.z);

            int hitCount = Physics.SphereCastNonAlloc(
                probeOrigin,
                probeRadius,
                Vector3.down,
                groundCastHitsCache,
                groundCheckDistance + collisionSkin,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );

            // Um contato lateral pode ser retornado antes do chao. Avaliamos todos
            // os resultados para nao alternar grounded ao encostar em uma parede.
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundCastHitsCache[i];
                groundCastHitsCache[i] = default;

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.normal.y < minimumGroundNormalY) continue;

                detectedGround = true;
                break;
            }
        }

        hasGroundContact = detectedGround;
        grounded = hasGroundContact || isStepping;

        playerProperties.grounded = grounded;

        if (wasGroundedLastFrame && !grounded)
        {
            altitude = rb.position.y;
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

            if (fall_damage != 0) TakeDamage(fall_damage);
            cameraShake.RequestShake(3, 0.15f);
        }

        wasGroundedLastFrame = grounded;
    }

    private float HandleFallDamage()
    {
        float distance = altitude - rb.position.y;
        return distance < 10 ? 0 : distance * 2;
    }

    private void UpdateVoxelStep()
    {
        if (!isStepping && !TryStartVoxelStep()) return;

        if (stepCollider == null || stepCollider != GetActiveCapsuleCollider() ||
            playerProperties.proned || playerProperties.roll)
        {
            CancelVoxelStep();
            return;
        }

        bool madeVerticalProgress = rb.position.y > stepLastRootY + 0.001f;
        stalledStepTicks = madeVerticalProgress ? 0 : stalledStepTicks + 1;
        stepLastRootY = rb.position.y;

        float maximumStepDuration = maxStepHeight / Mathf.Max(stepUpSpeed, 0.01f) + 0.35f;
        if (stalledStepTicks >= 4 || Time.fixedTime - stepStartTime > maximumStepDuration ||
            !IsStepLandingStillValid())
        {
            CancelVoxelStep();
            return;
        }

        float remainingHeight = stepTargetRootY - rb.position.y;
        if (remainingHeight <= collisionSkin * 0.5f)
        {
            Vector3 completedVelocity = rb.linearVelocity;
            if (completedVelocity.y > 0f) completedVelocity.y = 0f;
            rb.linearVelocity = completedVelocity;
            CancelVoxelStep(false);
            return;
        }

        float verticalSpeed = Mathf.Min(stepUpSpeed, remainingHeight / Time.fixedDeltaTime);
        Vector3 velocity = rb.linearVelocity;

        float forwardSpeed = Vector3.Dot(Vector3.ProjectOnPlane(velocity, Vector3.up), stepDirection);
        if (forwardSpeed < stepForwardAssistSpeed)
            velocity += stepDirection * (stepForwardAssistSpeed - forwardSpeed);

        velocity.y = Mathf.Max(velocity.y, verticalSpeed);
        rb.linearVelocity = velocity;

        grounded = true;
        playerProperties.grounded = true;
    }

    private bool TryStartVoxelStep()
    {
        if (!enableVoxelStepClimbing || !hasGroundContact || playerProperties.proned ||
            playerProperties.roll || moveDirection.sqrMagnitude < 0.0001f ||
            rb.linearVelocity.y > 0.5f)
        {
            return false;
        }

        CapsuleCollider activeCollider = GetActiveCapsuleCollider();
        if (activeCollider == null) return false;

        CapsuleGeometry capsule = GetCapsuleGeometry(activeCollider);
        float castRadius = Mathf.Max(0.01f, capsule.Radius - collisionSkin);
        Vector3 direction = Vector3.ProjectOnPlane(moveDirection, Vector3.up).normalized;
        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(planarVelocity, direction));
        float minimumProbeDistance = Mathf.Min(stepSearchDistance, Mathf.Max(collisionSkin * 2f, capsule.Radius * 0.2f));
        float forwardDistance = Mathf.Clamp(
            forwardSpeed * Time.fixedDeltaTime + collisionSkin * 2f,
            minimumProbeDistance,
            stepSearchDistance
        );

        if (!Physics.CapsuleCast(
                capsule.PointA,
                capsule.PointB,
                castRadius,
                direction,
                out RaycastHit obstacleHit,
                forwardDistance,
                VoxelStepMask,
                QueryTriggerInteraction.Ignore) ||
            obstacleHit.normal.y >= minimumGroundNormalY)
        {
            return false;
        }

        Vector3 inwardDirection = Vector3.ProjectOnPlane(-obstacleHit.normal, Vector3.up).normalized;
        if (inwardDirection.sqrMagnitude < 0.0001f) inwardDirection = direction;

        Vector3 landingProbeOrigin = obstacleHit.point + inwardDirection * stepLandingInset;
        landingProbeOrigin.y = capsule.FootY + maxStepHeight + groundCheckDistance + collisionSkin;

        if (!Physics.Raycast(
                landingProbeOrigin,
                Vector3.down,
                out RaycastHit landingHit,
                maxStepHeight + groundCheckDistance + collisionSkin * 2f,
                VoxelStepMask,
                QueryTriggerInteraction.Ignore) ||
            landingHit.normal.y < minimumGroundNormalY)
        {
            return false;
        }

        float stepHeight = landingHit.point.y - capsule.FootY;
        if (stepHeight <= collisionSkin || stepHeight > maxStepHeight) return false;

        Vector3 raisedOffset = Vector3.up * (stepHeight + collisionSkin);
        if (Physics.CapsuleCast(
                capsule.PointA,
                capsule.PointB,
                castRadius,
                Vector3.up,
                out _,
                stepHeight + collisionSkin,
                BodyClearanceMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (Physics.CheckCapsule(
                capsule.PointA + raisedOffset,
                capsule.PointB + raisedOffset,
                castRadius,
                BodyClearanceMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        float elevatedForwardDistance = Mathf.Max(forwardDistance, obstacleHit.distance + stepLandingInset);
        if (Physics.CapsuleCast(
                capsule.PointA + raisedOffset,
                capsule.PointB + raisedOffset,
                castRadius,
                direction,
                out _,
                elevatedForwardDistance,
                BodyClearanceMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 landingOffset = raisedOffset + direction * (obstacleHit.distance + stepLandingInset);
        if (Physics.CheckCapsule(
                capsule.PointA + landingOffset,
                capsule.PointB + landingOffset,
                castRadius,
                BodyClearanceMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        isStepping = true;
        stepTargetRootY = rb.position.y + stepHeight + collisionSkin;
        stepStartTime = Time.fixedTime;
        stepLastRootY = rb.position.y;
        stalledStepTicks = 0;
        stepDirection = direction;
        stepLandingPoint = landingHit.point;
        stepCollider = activeCollider;
        return true;
    }

    private bool IsStepLandingStillValid()
    {
        Vector3 probeOrigin = stepLandingPoint + Vector3.up * (groundCheckDistance + collisionSkin);
        float probeDistance = groundCheckDistance + collisionSkin * 2f;

        return Physics.Raycast(
                   probeOrigin,
                   Vector3.down,
                   out RaycastHit hit,
                   probeDistance,
                   VoxelStepMask,
                   QueryTriggerInteraction.Ignore) &&
               hit.normal.y >= minimumGroundNormalY &&
               Mathf.Abs(hit.point.y - stepLandingPoint.y) <= collisionSkin * 2f;
    }

    private void CancelVoxelStep(bool stopVerticalMotion = true)
    {
        bool wasStepping = isStepping;
        isStepping = false;
        stepTargetRootY = 0f;
        stepStartTime = 0f;
        stepLastRootY = 0f;
        stalledStepTicks = 0;
        stepDirection = Vector3.zero;
        stepLandingPoint = Vector3.zero;
        stepCollider = null;

        if (wasStepping && stopVerticalMotion && rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            if (velocity.y > 0f) velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        grounded = hasGroundContact;
        if (playerProperties != null) playerProperties.grounded = grounded;
    }

    private int VoxelStepMask => voxelStepLayer.value != 0
        ? voxelStepLayer.value
        : LayerMask.GetMask("Voxel");

    private int BodyClearanceMask => stepClearanceLayer.value & ~(1 << gameObject.layer);

    private CapsuleCollider GetActiveCapsuleCollider()
    {
        if (stand_collider != null && stand_collider.enabled) return stand_collider;
        if (crouch_collider != null && crouch_collider.enabled) return crouch_collider;
        if (prone_collider != null && prone_collider.enabled) return prone_collider;
        return null;
    }

    private static CapsuleGeometry GetCapsuleGeometry(CapsuleCollider collider)
    {
        Transform capsuleTransform = collider.transform;
        Vector3 scale = capsuleTransform.lossyScale;
        Rigidbody attachedBody = collider.attachedRigidbody;
        bool colliderIsOnBodyRoot = attachedBody != null && attachedBody.transform == capsuleTransform;
        Quaternion worldRotation = colliderIsOnBodyRoot ? attachedBody.rotation : capsuleTransform.rotation;
        Vector3 axis;
        float axisScale;
        float radiusScale;

        switch (collider.direction)
        {
            case 0:
                axis = worldRotation * Vector3.right;
                axisScale = Mathf.Abs(scale.x);
                radiusScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                break;
            case 2:
                axis = worldRotation * Vector3.forward;
                axisScale = Mathf.Abs(scale.z);
                radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                break;
            default:
                axis = worldRotation * Vector3.up;
                axisScale = Mathf.Abs(scale.y);
                radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                break;
        }

        float radius = collider.radius * radiusScale;
        float height = Mathf.Max(collider.height * axisScale, radius * 2f);
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 center = colliderIsOnBodyRoot
            ? attachedBody.position + worldRotation * Vector3.Scale(collider.center, scale)
            : capsuleTransform.TransformPoint(collider.center);

        return new CapsuleGeometry(center + axis * halfSegment, center - axis * halfSegment, radius);
    }
    #endregion

    #region Camera & FX
    private void HandleNightVision()
    {
        is_night_vision_active = !is_night_vision_active;
        nightVisionPostFX.SetActive(is_night_vision_active);
    }

    private void UpdateDamageVignette()
    {
        if (damageTaken_vignette == null) return;

        float hpPercentage = playerProperties.hp.Value / playerProperties.maxHp;
        targetVignetteIntensity = 1f - hpPercentage;

        currentVignetteIntensity = Mathf.SmoothDamp(
            currentVignetteIntensity,
            targetVignetteIntensity,
            ref vignetteVelocity,
            0.2f
        );

        damageTaken_vignette.intensity.value = currentVignetteIntensity;
    }

    #endregion

    #region Interaction
    private void Interact()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (!playerProperties.isInVehicle) TryInteractWithButton(origin, direction);
    }

    private void TryInteractWithButton(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, InteractiveButton.INTERACT_DISTANCE, interactivesLayer))
        {
            InteractiveButton button = hit.collider.GetComponent<InteractiveButton>();
            button?.Interact(this);
        }
    }
    #endregion

    #region State Management
    public void ResetWeaponAnimation()
    {
        if (weapon != null)
        {
            weapon.can_shoot = true;
            weapon.weaponAnimation.FinishReloadAnimation();
        }
    }

    private void NormalizeStanceColliders()
    {
        if (stand_collider == null || crouch_collider == null) return;

        crouch_collider.radius = Mathf.Min(crouch_collider.radius, stand_collider.radius);
    }

    private bool TryPrepareStandingStance()
    {
        if (!playerProperties.crouched && !playerProperties.proned) return true;

        if (!HasClearanceForCollider(stand_collider))
        {
            ShowNotEnoughSpaceMessage();
            playerProperties.sprinting = false;
            return false;
        }

        playerProperties.crouched = false;
        playerProperties.proned = false;
        return true;
    }

    private bool HasClearanceForCollider(CapsuleCollider targetCollider)
    {
        if (targetCollider == null) return false;

        CapsuleCollider activeCollider = GetActiveCapsuleCollider();
        if (activeCollider == targetCollider) return true;

        CapsuleGeometry targetCapsule = GetCapsuleGeometry(targetCollider);
        float currentFootY = activeCollider != null
            ? GetCapsuleGeometry(activeCollider).FootY
            : targetCapsule.FootY;

        Vector3 feetAlignment = Vector3.up * (currentFootY - targetCapsule.FootY + collisionSkin);
        float queryRadius = Mathf.Max(0.01f, targetCapsule.Radius - collisionSkin);
        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            targetCapsule.PointA + feetAlignment,
            targetCapsule.PointB + feetAlignment,
            queryRadius,
            stanceOverlapCache,
            BodyClearanceMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = stanceOverlapCache[i];
            stanceOverlapCache[i] = null;

            if (overlap == null || overlap.transform.IsChildOf(transform)) continue;
            return false;
        }

        return overlapCount < stanceOverlapCache.Length;
    }

    private static void ShowNotEnoughSpaceMessage()
    {
        if (AlertMessages.Instance != null) AlertMessages.Instance.CreateMessage("Not Enough Space", 2);
    }

    private void UpdateColliderStateLocal()
    {
        PlayerStance targetStance = GetTargetStance();

        if (targetStance != currentStance)
        {
            ApplyColliderStance(targetStance);
            currentStance = targetStance;
            CmdUpdateColliderStateRemote(targetStance);
        }
    }

    private PlayerStance GetTargetStance()
    {
        if (playerProperties.isDead.Value || playerProperties.isInVehicle) return PlayerStance.Disabled;
        if (playerProperties.proned) return PlayerStance.Prone;
        if (playerProperties.crouched || playerProperties.roll) return PlayerStance.Crouch;
        return PlayerStance.Stand;
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdUpdateColliderStateRemote(PlayerStance playerStance) => RpcUpdateColliderStateRemote(playerStance);

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcUpdateColliderStateRemote(PlayerStance playerStance)
    {
        ApplyColliderStance(playerStance);
        currentStance = playerStance;
    }

    private void ApplyColliderStance(PlayerStance playerStance)
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


    [Client]
    public void SetCollidersState(bool enabled)
    {
        PlayerStance targetStance = enabled ? GetTargetStance() : PlayerStance.Disabled;
        ApplyColliderStance(targetStance);
        currentStance = targetStance;
    }

    private void HandleDeathState()
    {
        HandleMecidProximity();
        deathTimer += Time.deltaTime;

        float deathProgress = Mathf.Clamp01(deathTimer / playerProperties.deathTimer);

        if (deathProgress >= 1)
        {
            AccountManager.Instance.accountStatus.AddDeath();
            AccountManager.Instance.RemoveBattleCoin(10);
            PlayerSpawnController.Instance.Reestart();

            if (IsSpawned) RequestDespawn();
            else Destroy(gameObject);

            return;
        }

        Quaternion targetRotation = new Quaternion(0, 0, 0, playerHead.transform.localRotation.w);
        playerHead.transform.localRotation = Quaternion.Lerp(playerHead.transform.localRotation, targetRotation, Time.deltaTime * 2);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestDespawn() => Despawn(gameObject);

    private void HandleMecidProximity()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 50f, medicCollidersCache, playerLayer);

        List<PlayerInfo> jogadoresDetectados = new List<PlayerInfo>(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            PlayerProperties p = medicCollidersCache[i].GetComponent<PlayerProperties>();

            if (p != null && p.selectedClass.Value == ClassManager.Class.Medic)
            {
                float distancia = Vector3.Distance(transform.position, medicCollidersCache[i].transform.position);
                jogadoresDetectados.Add(new PlayerInfo(medicCollidersCache[i].gameObject, p.playerName.Value, distancia));
            }
        }

        jogadoresDetectados.Sort((a, b) => a.distance.CompareTo(b.distance));
    }

    public class PlayerInfo
    {
        public GameObject gameObject;
        public string playerName;
        public float distance;

        public PlayerInfo(GameObject go, string playerName, float distance)
        {
            this.playerName = playerName;
            gameObject = go;
            this.distance = distance;
        }
    }
    #endregion

    #region Damage / Kill and Revive
    public void UpdateWeaponProperties(float speedModifier, float applyRecoilSpeed, float resetRecoilSpeed)
    {
        if (processCameraRecoil != null)
        {
            processCameraRecoil.SetApplyRecoilSpeed(applyRecoilSpeed);

            if (cameraRotation != null) cameraRotation.ResetRecoilPreservingAim();
            else processCameraRecoil.ResetState();
        }

        walkSpeed = original_walk_speed + speedModifier;
        sprintSpeed = original_sprint_speed + speedModifier;
        crouchSpeed = original_crouch_speed + speedModifier;
        UpdateMovementSpeed();
    }

    public void TakeDamage(float rawDamage)
    {
        if (playerProperties.isDead.Value) return;

        if (IsServerStarted) ServerApplyDamage(rawDamage);
        else CmdApplyDamage(rawDamage);

    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdApplyDamage(float rawDamage) => ServerApplyDamage(rawDamage);

    [Server]
    private void ServerApplyDamage(float rawDamage)
    {
        if (playerProperties.isDead.Value) return;

        playerProperties.hp.Value = Mathf.Clamp(playerProperties.hp.Value - rawDamage, 0, playerProperties.maxHp);

        if (Owner.IsValid) TargetOnDamageReceived(Owner, rawDamage);

        if (playerProperties.hp.Value <= 0)
        {
            CmdSetKnematicRb(true);
            playerProperties.isDead.Value = true;
            TargetProcessDeadPlayer(Owner);
        }
    }

    [TargetRpc]
    private void TargetOnDamageReceived(NetworkConnection conn, float dmg)
    {
        // Executado apenas na máquina do jogador que tomou dano (visuais e HUD)
        if (cameraShake != null) cameraShake.RequestShake(dmg / 2f, 0.1f);
        if (dmg > 40 && soldierHudManager != null && soldierHudManager.screenBlood != null) soldierHudManager.screenBlood.TriggerBlood();

    }

    [TargetRpc]
    private void TargetProcessDeadPlayer(NetworkConnection conn) => ProcessDeadPlayer();

    public void ProcessDeadPlayer()
    {
        if (playerProperties.isInVehicle) playerProperties.isInVehicle = false;

        soldierHudManager.ActivateDeadHUD();
    }

    public void Revive(float hp)
    {
        UpdateColliderStateLocal();

        if (IsServerStarted) ServerRevive(hp);
        else RequestRevive(hp);
    }

    public void Revive(float hp, Vector3 position)
    {
        transform.position = position;
        Revive(hp);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRevive(float hp) => ServerRevive(hp);

    [Server]
    private void ServerRevive(float hp)
    {
        CmdSetKnematicRb(false);
        playerProperties.hp.Value = hp;
        playerProperties.isDead.Value = false;
        if (Owner.IsValid) TargetRevive(Owner);
    }

    [TargetRpc]
    private void TargetRevive(NetworkConnection conn)
    {
        HideOwnerItems(true);

        if (soldierHudManager != null) soldierHudManager.ActivateStandardHUD();

        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        if (cameraRotation != null) cameraRotation.SynchronizeYawWithBody();
    }

    public void Regenerate(float hp)
    {
        if (!IsServerStarted) return;
        playerProperties.hp.Value = Mathf.Min(playerProperties.hp.Value + hp, playerProperties.maxHp);
    }

    [ObserversRpc]
    private void CmdSetKnematicRb(bool state) => SetKnematicRb(state);
    private void SetKnematicRb(bool state) => rb.isKinematic = state;
    #endregion

    #region Utility
    public void HideOwnerItems(bool hide)
    {
        if (skinApplier.instantiatedLeftUpperArm != null) skinApplier.instantiatedLeftUpperArm.meshRenderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        if (skinApplier.instantiatedLeftLowerArm != null) skinApplier.instantiatedLeftLowerArm.meshRenderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        if (skinApplier.instantiatedLeftHand != null) skinApplier.instantiatedLeftHand.meshRenderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        if (skinApplier.instantiatedRightUpperArm != null) skinApplier.instantiatedRightUpperArm.meshRenderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        if (skinApplier.instantiatedRightLowerArm != null) skinApplier.instantiatedRightLowerArm.meshRenderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        if (skinApplier.instantiatedRightHand != null) skinApplier.instantiatedRightHand.meshRenderer.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;

        if (hide) thirdPersonWeapon.HideWeapon();
        else thirdPersonWeapon.ShowWeapon();
    }
    public FactionManager.Faction GetFaction() => playerProperties.faction.Value;
    public Transform GetSpotPosition() => spot_position;
    public Vector3 GetVelocoty() => rb.linearVelocity;
    #endregion
}
