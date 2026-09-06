using System.Collections;
using UnityEngine;

public class SwayNBobScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private SwitchWeapon switchWeapon;
    [SerializeField] private PlayerController playerController;

    private const float SWAY_POSITION_STEP = 0.01f;
    private const float MAX_SWAY_POSITION = 0.06f;
    private const float SWAY_ROTATION_STEP = 4f;
    private const float MAX_SWAY_ROTATION = 5f;
    private const float AIM_MOVE_ROTATION_AMOUNT = 1.5f;
    private const float AIM_MOVE_ROTATION_SPEED = 5f;
    private const float POSITION_SMOOTH_SPEED = 10f;
    private const float ROTATION_SMOOTH_SPEED = 12f;
    private const float CROUCH_SHAKE_DURATION = 0.1f;
    private const float CROUCH_SHAKE_INTENSITY = 2f;
    private const float AIRBORNE_POSITION_OFFSET = 0.01f;

    private static readonly Vector3 TRAVEL_LIMIT = new Vector3(0.025f, 0.025f, 0.025f);
    private static readonly Vector3 BOB_LIMIT = new Vector3(0.01f, 0.01f, 0.01f);

    // Configuração da arma atual.
    private float bobWalkExaggeration;
    private float bobSprintExaggeration;
    private float bobCrouchExaggeration;
    private Vector3 walkMultiplier;
    private Vector3 sprintMultiplier;
    private Vector3 crouchMultiplier;
    private Vector3 storePosition;
    private Quaternion storeRotation;

    // Estado e pose base.
    private bool isInitialized;
    private bool canUseStoredPose;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 baseTargetPosition;
    private Quaternion baseTargetRotation;
    private Vector3 defaultMovementMultiplier;
    private Vector3 currentMovementMultiplier;

    // Offset externo composto com sway/bob. Apenas este script escreve no Transform.
    private Vector3 visualRecoilPositionOffset;
    private Quaternion visualRecoilRotationOffset = Quaternion.identity;

    // Entradas e offsets calculados a cada frame.
    private Vector2 lookInput;
    private Vector2 walkInput;
    private Vector3 swayPos;
    private Vector3 swayEulerRot;
    private Vector3 bobPosition;
    private Vector3 bobEulerRotation;
    private Vector3 shakeOffset;
    private float speedCurve;
    private float bobExaggeration;
    private bool lookInputBlocked;

    #region Unity Lifecycle
    private void Awake()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        baseTargetRotation = initialRotation;
        baseTargetPosition = initialPosition;
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (ShouldApplyDeadState())
        {
            ApplyDeadState();
            return;
        }

        UpdateInputs();
        UpdateMovementProfile();
        UpdateStoredPoseTargets();
        UpdateSwayRotation();
        UpdateSwayPosition();
        UpdateBobOffset();
        UpdateBobRotation();
        ApplyCompositeTransform();
    }
    #endregion

    #region Public Methods
    /// <summary>Configura o sway e o bob para a arma atualmente equipada.</summary>
    public void Restart(SwayAndBobValues values)
    {
        bobWalkExaggeration = values.bobWalkExaggeration;
        bobSprintExaggeration = values.bobSprintExaggeration;
        bobCrouchExaggeration = values.bobCrouchExaggeration;
        walkMultiplier = values.walkMultiplier;
        sprintMultiplier = values.sprintMultiplier;
        crouchMultiplier = values.crouchMultiplier;
        storePosition = values.storePosition;
        storeRotation = values.storeRotation;

        currentMovementMultiplier = walkMultiplier;
        defaultMovementMultiplier = walkMultiplier;
        baseTargetPosition = initialPosition;
        baseTargetRotation = initialRotation;
        ResetVisualRecoilOffset();

        isInitialized = true;
    }

    public void SetVisualRecoilOffset(Vector3 positionOffset, Quaternion rotationOffset)
    {
        visualRecoilPositionOffset = positionOffset;
        visualRecoilRotationOffset = rotationOffset;
    }

    public void ResetVisualRecoilOffset()
    {
        visualRecoilPositionOffset = Vector3.zero;
        visualRecoilRotationOffset = Quaternion.identity;
    }

    public void SetLookInputBlocked(bool blocked) => lookInputBlocked = blocked;

    public IEnumerator CrouchWeaponShake()
    {
        float elapsed = 0f;

        while (elapsed < CROUCH_SHAKE_DURATION)
        {
            float crouchTime = Time.time * 5f;
            shakeOffset = CalculateCrouchShake(crouchTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        float returnTime = CROUCH_SHAKE_DURATION * 0.5f;
        elapsed = 0f;
        Vector3 startingShakeOffset = shakeOffset;

        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            shakeOffset = Vector3.Lerp(startingShakeOffset, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }

    private static Vector3 CalculateCrouchShake(float time)
    {
        float x = (Mathf.PerlinNoise(time * 1.2f, 0f) * 2f - 1f) * 3f;
        float y = ((Mathf.PerlinNoise(0f, time * 1.5f) * 2f - 1f) * 0.4f +
                   Mathf.Sin(time * 3f) * 0.6f) * 0.8f;
        float z = (Mathf.PerlinNoise(time * 0.8f, time * 0.8f) * 2f - 1f) * 0.7f;

        return new Vector3(x, y, z) * CROUCH_SHAKE_INTENSITY;
    }

    public void EnableStoreWeapon(bool enabled)
    {
        canUseStoredPose = enabled;

        if (!enabled)
        {
            baseTargetPosition = initialPosition;
            baseTargetRotation = initialRotation;
        }
    }
    #endregion

    #region State Management
    private bool ShouldApplyDeadState()
    {
        return playerProperties.isDead.Value ||
               playerProperties.isProneTransition ||
               playerProperties.isComposingBullets;
    }

    private void ApplyDeadState()
    {
        Quaternion deadRotation = Quaternion.Euler(40f, 0f, 0f);
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            deadRotation,
            Time.deltaTime * ROTATION_SMOOTH_SPEED
        );
    }
    #endregion

    #region Movement Profile
    private void UpdateMovementProfile()
    {
        if (CanUseSprintProfile())
        {
            bobExaggeration = bobSprintExaggeration;
            currentMovementMultiplier = sprintMultiplier;
        }
        else
        {
            bobExaggeration = bobWalkExaggeration;
            currentMovementMultiplier = defaultMovementMultiplier;
        }

        bool isCrouchingOrProne = (playerProperties.crouched || playerProperties.proned) && !playerProperties.sprinting;

        if (isCrouchingOrProne || playerProperties.aiming)
        {
            bobExaggeration = bobCrouchExaggeration;
            currentMovementMultiplier = crouchMultiplier;
        }

        if (IsPlayerStationary())
        {
            bobExaggeration = 0f;
            currentMovementMultiplier = Vector3.one * 0.1f;
        }
    }

    private bool CanUseSprintProfile()
    {
        return playerProperties.sprinting &&
               !IsSwitchingWeapon() &&
               !playerProperties.aiming &&
               !playerProperties.proned &&
               !playerProperties.roll &&
               !playerProperties.isProneTransition;
    }

    private bool IsPlayerStationary() =>
        Mathf.Approximately(playerController.moveForward, 0f) &&
        Mathf.Approximately(playerController.moveHorizontal, 0f);
    #endregion

    #region Stored Pose
    private void UpdateStoredPoseTargets()
    {
        bool isSwitchingWeapon = IsSwitchingWeapon();
        bool isSprintOrRollActive = playerProperties.sprinting || playerProperties.roll;
        bool canEnterStoredPose = isSprintOrRollActive &&
                                  !playerProperties.reloading &&
                                  !playerProperties.firing;
        bool isSettingsMenuOpen = SettingsHUD.Instance != null && SettingsHUD.Instance.is_menu_settings_active;

        bool shouldUseStoredPose = canUseStoredPose &&
                                   !isSwitchingWeapon &&
                                   (canEnterStoredPose || isSettingsMenuOpen);

        if (shouldUseStoredPose)
        {
            baseTargetPosition = initialPosition + storePosition;
            baseTargetRotation = initialRotation * storeRotation;
        }
        else
        {
            baseTargetPosition = initialPosition;
            baseTargetRotation = initialRotation;
        }
    }

    private bool IsSwitchingWeapon() => switchWeapon != null && switchWeapon.IsSwitchingWeapon;
    #endregion

    #region Input Handling
    private void UpdateInputs()
    {
        walkInput.x = playerController.moveHorizontal;
        walkInput.y = playerController.moveForward;
        walkInput.Normalize();

        if (lookInputBlocked)
        {
            lookInput = Vector2.zero;
            return;
        }

        lookInput.x = -InputManager.GetAxis("Mouse X");
        lookInput.y = InputManager.GetAxis("Mouse Y");
    }
    #endregion

    #region Sway Logic
    private void UpdateSwayPosition()
    {
        Vector3 invertedLook = (lookInput * -SWAY_POSITION_STEP).normalized;
        invertedLook.x = Mathf.Clamp(invertedLook.x, -MAX_SWAY_POSITION, MAX_SWAY_POSITION);
        invertedLook.y = Mathf.Clamp(invertedLook.y, -MAX_SWAY_POSITION, MAX_SWAY_POSITION);
        swayPos = invertedLook;
    }

    private void UpdateSwayRotation()
    {
        Vector2 invertedLook = (lookInput * -SWAY_ROTATION_STEP).normalized;
        invertedLook.x = Mathf.Clamp(invertedLook.x, -MAX_SWAY_ROTATION, MAX_SWAY_ROTATION);
        invertedLook.y = Mathf.Clamp(invertedLook.y, -MAX_SWAY_ROTATION, MAX_SWAY_ROTATION);
        swayEulerRot = new Vector3(invertedLook.y, invertedLook.x, invertedLook.x).normalized;
    }
    #endregion

    #region Bobbing Logic
    private void UpdateBobOffset()
    {
        float verticalInput = InputManager.GetAxis("Vertical");
        float horizontalInput = InputManager.GetAxis("Horizontal");
        bool hasMovementInput = Mathf.Abs(verticalInput) > 0.01f || Mathf.Abs(horizontalInput) > 0.01f;
        float groundedMultiplier = playerProperties.grounded ? 1f : 0f;

        float bobSpeed = playerProperties.grounded
            ? (hasMovementInput ? bobExaggeration : 0f)
            : 1f;

        speedCurve += Time.deltaTime * bobSpeed + 0.005f;

        float cosCurve = Mathf.Cos(speedCurve);
        float sinCurve = Mathf.Sin(speedCurve);

        bobPosition.x = (cosCurve * BOB_LIMIT.x * groundedMultiplier) - (walkInput.x * TRAVEL_LIMIT.x);
        bobPosition.y = (sinCurve * BOB_LIMIT.y) - (verticalInput * TRAVEL_LIMIT.y);
        bobPosition.z = -(walkInput.y * TRAVEL_LIMIT.z);
    }

    private void UpdateBobRotation()
    {
        bool isMoving = walkInput != Vector2.zero;
        float sin2x = Mathf.Sin(2f * speedCurve);
        float cosCurve = Mathf.Cos(speedCurve);

        bobEulerRotation.x = currentMovementMultiplier.x * sin2x * (isMoving ? 1f : 0.5f);
        bobEulerRotation.y = isMoving ? currentMovementMultiplier.y * cosCurve : 0f;
        bobEulerRotation.z = isMoving ? currentMovementMultiplier.z * cosCurve * walkInput.x : 0f;
    }
    #endregion

    #region Composite Position & Rotation
    private void ApplyCompositeTransform()
    {
        float yawOffset = Mathf.Clamp(lookInput.x, -5f, 5f);
        float rollOffset = yawOffset;
        float pitchOffset = Mathf.Clamp(lookInput.y, -5f, 5f);

        float verticalAimMoveRotation = CalculateAimMoveRotation();
        Quaternion combinedRotation = CalculateCombinedRotation(verticalAimMoveRotation);
        Vector3 combinedPosition = CalculateCombinedPosition();

        combinedRotation *= Quaternion.Euler(shakeOffset);

        if (playerProperties.grounded)
            ApplyGroundedTransform(pitchOffset, yawOffset, rollOffset, combinedRotation, combinedPosition);
        else
            ApplyAirborneTransform(yawOffset, rollOffset, combinedRotation, combinedPosition);
    }
    #endregion

    #region Aim Movement Rotation
    private float CalculateAimMoveRotation()
    {
        if (!playerProperties.aiming) return 0f;

        if (IsPlayerStationary() ||
            playerProperties.sprinting ||
            playerProperties.reloading ||
            playerProperties.roll) return 0f;

        float sinTime = Mathf.Sin(Time.time * AIM_MOVE_ROTATION_SPEED);
        float rotation = sinTime * AIM_MOVE_ROTATION_AMOUNT;

        if (playerProperties.crouched || playerProperties.proned) rotation *= 0.5f;

        return rotation;
    }

    #endregion

    #region Combined Calculation

    private Quaternion CalculateCombinedRotation(float verticalAimMoveRotation)
    {
        if (!playerProperties.aiming)
        {
            return baseTargetRotation *
                   Quaternion.Euler(swayEulerRot) *
                   Quaternion.Euler(bobEulerRotation);
        }

        float divisor = playerProperties.firing ? 20f : 5f;
        Vector3 aimingRotation = swayEulerRot / divisor;
        aimingRotation.x += verticalAimMoveRotation;

        return baseTargetRotation *
               Quaternion.Euler(aimingRotation) *
               Quaternion.Euler(bobEulerRotation / divisor);
    }

    private Vector3 CalculateCombinedPosition()
    {
        if (!playerProperties.aiming) return baseTargetPosition + swayPos + bobPosition;

        float divisor = playerProperties.firing ? 20f : 5f;
        return baseTargetPosition + (swayPos / divisor) + (bobPosition / divisor);
    }
    #endregion

    #region State Application
    private void ApplyAirborneTransform(
        float yawOffset,
        float rollOffset,
        Quaternion combinedRotation,
        Vector3 combinedPosition)
    {
        float tiltAmount = InputManager.GetAxis("Horizontal") * 10f;
        Quaternion targetRotation = Quaternion.Euler(15f, yawOffset, -tiltAmount + rollOffset);
        Vector3 targetPosition = new Vector3(
            combinedPosition.x,
            combinedPosition.y - AIRBORNE_POSITION_OFFSET,
            combinedPosition.z
        );

        ApplyTransform(
            targetPosition + visualRecoilPositionOffset,
            combinedRotation * targetRotation * visualRecoilRotationOffset);
    }

    private void ApplyGroundedTransform(
        float pitchOffset,
        float yawOffset,
        float rollOffset,
        Quaternion combinedRotation,
        Vector3 combinedPosition)
    {
        Quaternion targetRotation = playerProperties.firing
            ? Quaternion.Euler(pitchOffset / 8f, yawOffset, -rollOffset)
            : Quaternion.Euler(pitchOffset, yawOffset, -rollOffset);

        ApplyTransform(
            combinedPosition + visualRecoilPositionOffset,
            combinedRotation * targetRotation * visualRecoilRotationOffset);
    }

    private void ApplyTransform(Vector3 targetPosition, Quaternion targetRotation)
    {
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * ROTATION_SMOOTH_SPEED
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * POSITION_SMOOTH_SPEED
        );
    }
    #endregion

    #region Structs
    [System.Serializable]
    public struct SwayAndBobValues
    {
        [Header("Sway Exaggeration")]
        public float bobWalkExaggeration;
        public float bobSprintExaggeration;
        public float bobCrouchExaggeration;

        [Header("Sway Multipliers")]
        public Vector3 walkMultiplier;
        public Vector3 sprintMultiplier;
        public Vector3 crouchMultiplier;

        [Header("Stored Pose")]
        public Vector3 storePosition;
        public Quaternion storeRotation;
    }
    #endregion
}
