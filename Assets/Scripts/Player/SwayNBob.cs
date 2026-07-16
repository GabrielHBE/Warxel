using System.Collections;
using UnityEngine;

public class SwayNBobScript : MonoBehaviour
{
    private PlayerProperties playerProperties;
    private SwitchWeapon switchWeapon;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    [HideInInspector] public Vector3 swayPos;

    [Header("Aiming Movement Rotation")]
    public float aimMoveRotationAmount = 5f;
    public float aimMoveRotationSpeed = 5f;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    [HideInInspector] public Vector3 swayEulerRot;
    public float smooth = 10f;
    private float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve;
    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    public float bobExaggeration;

    [HideInInspector] public float[] vector3Values = new float[3];
    [HideInInspector] public float[] quaternionValues = new float[3];

    private PlayerController playerController;
    private Transform myTransform;

    // Player state cache
    private bool isAiming;
    private bool isSprinting;
    private bool isGrounded;
    private bool isCrouched;
    private bool isReloading;
    private bool isFiring;
    private bool isRolling;
    private bool isProne;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 initialVector3;
    private bool isRestarted;
    private Vector2 lookInput;
    private Vector3 shakeOffset = Vector3.zero;
    private Vector3 bobPosition;
    private Vector3 bobEulerRotation;
    private Vector2 walkInput;
    private float CurveSin => Mathf.Sin(speedCurve);
    private float CurveCos => Mathf.Cos(speedCurve);
    private const float MaxPositionSprinting = 1f;
    private float currentPositionSprinting;
    private int sprintDirection = 1;
    private Quaternion sprintTargetWeaponRotation;
    private Vector3 sprintTargetWeaponPosition;
    private float bobWalkExaggeration;
    private float bobSprintExaggeration;
    private float bobCrouchExaggeration;
    private float bobAimExaggeration;
    private Vector3 walkMultiplier;
    private Vector3 sprintMultiplier;
    private Vector3 aimMultiplier;
    private Vector3 crouchMultiplier;
    private Vector3 currentMultiplier;

    private const int YRotationMultiplier = 4;
    private const int ZRotationMultiplier = 4;
    private const int XRotationMultiplier = 4;

    #region Unity Lifecycle

    private void Awake()
    {
        isRestarted = false;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        myTransform = transform;
    }

    private void Update()
    {
        if (!isRestarted) return;

        CachePlayerProperties();

        if (ShouldApplyDeadState())
        {
            ApplyDeadState();
            return;
        }

        UpdateBobMultipliers();
        HandleSprinting();

        StoreWeapon();
        UpdateSwayRotation();
        UpdateSway();
        UpdateBobOffset();
        UpdateBobRotation();
        UpdateCompositePositionRotation();
    }

    #endregion

    #region Public Methods

    public void Restart(
        float bobWalkExaggeration,
        float bobSprintExaggeration,
        float bobCrouchExaggeration,
        float bobAimExaggeration,
        Vector3 walkMultiplier,
        Vector3 sprintMultiplier,
        Vector3 aimMultiplier,
        Vector3 crouchMultiplier,
        float[] vector3Values,
        float[] quaternionValues)
    {
        this.bobWalkExaggeration = bobWalkExaggeration;
        this.bobSprintExaggeration = bobSprintExaggeration;
        this.bobCrouchExaggeration = bobCrouchExaggeration;
        this.bobAimExaggeration = bobAimExaggeration;
        this.walkMultiplier = walkMultiplier;
        this.sprintMultiplier = sprintMultiplier;
        this.aimMultiplier = aimMultiplier;
        this.crouchMultiplier = crouchMultiplier;
        this.vector3Values = vector3Values;
        this.quaternionValues = quaternionValues;

        switchWeapon = GetComponent<SwitchWeapon>();
        playerController = GetComponentInParent<PlayerController>();
        playerProperties = GetComponentInParent<PlayerProperties>();

        currentMultiplier = this.walkMultiplier;
        initialVector3 = currentMultiplier;
        sprintTargetWeaponRotation = initialRotation;

        isRestarted = true;
    }

    public IEnumerator CrouchWeaponShake()
    {
        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            float crouchTime = Time.time * 5f;
            float intensity = 2f;

            shakeOffset = new Vector3(
                (Mathf.PerlinNoise(crouchTime * 1.2f, 0f) * 2f - 1f) * 3f * intensity,
                ((Mathf.PerlinNoise(0f, crouchTime * 1.5f) * 2f - 1f) * 0.4f +
                 (Mathf.Sin(crouchTime * 3f) * 0.6f)) * 0.8f * intensity,
                (Mathf.PerlinNoise(crouchTime * 0.8f, crouchTime * 0.8f) * 2f - 1f) * 0.7f * intensity
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        float returnTime = Mathf.Min(0.1f, duration * 0.5f);
        elapsed = 0f;
        Vector3 startingShake = shakeOffset;

        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            shakeOffset = Vector3.Lerp(startingShake, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }

    #endregion

    #region State Management

    private void CachePlayerProperties()
    {
        isAiming = playerProperties.is_aiming;
        isSprinting = playerProperties.sprinting;
        isGrounded = playerProperties.isGrounded;
        isCrouched = playerProperties.crouched;
        isReloading = playerProperties.is_reloading;
        isFiring = playerProperties.is_firing;
        isProne = playerProperties.is_proned;
        isRolling = playerProperties.roll;
    }

    private bool ShouldApplyDeadState()
    {
        return playerProperties.is_dead.Value ||
               playerProperties.isProneTransition ||
               playerProperties.is_composing_bullets;
    }

    private void ApplyDeadState()
    {
        Quaternion deadRotation = Quaternion.Euler(40f, 0f, 0f);
        myTransform.localRotation = Quaternion.Lerp(
            myTransform.localRotation,
            deadRotation,
            Time.deltaTime * smoothRot
        );
    }

    #endregion

    #region Bob Multipliers Logic

    private void UpdateBobMultipliers()
    {
        bool isSprintingActive = playerProperties.sprinting &&
                                 !switchWeapon._switch &&
                                 !playerProperties.is_aiming &&
                                 !playerProperties.is_proned &&
                                 !playerProperties.roll &&
                                 !playerProperties.isProneTransition;

        if (isSprintingActive)
        {
            bobExaggeration = bobSprintExaggeration;
            currentMultiplier = sprintMultiplier;
        }
        else
        {
            currentPositionSprinting = 0f;
            bobExaggeration = bobWalkExaggeration;
            sprintTargetWeaponPosition = initialPosition;
            sprintTargetWeaponRotation = initialRotation;
            currentMultiplier = initialVector3;
        }

        bool isCrouchingOrProne = (playerProperties.crouched || playerProperties.is_proned) &&
                                  !playerProperties.sprinting;

        if (isCrouchingOrProne || playerProperties.is_aiming)
        {
            bobExaggeration = bobCrouchExaggeration;
            currentMultiplier = crouchMultiplier;
        }

        bool isNotMoving = Mathf.Approximately(playerController.moveForward, 0f) &&
                          Mathf.Approximately(playerController.moveHorizontal, 0f);

        if (isNotMoving)
        {
            bobExaggeration = 0f;
            sprintTargetWeaponPosition = initialPosition;
            sprintTargetWeaponRotation = initialRotation;
            currentMultiplier = new Vector3(0.1f, 0.1f, 0.1f);
        }

        bool isAimingAndMoving = playerProperties.is_aiming && !isNotMoving;

        if (isAimingAndMoving)
        {
            bobExaggeration = bobAimExaggeration;
            currentMultiplier = aimMultiplier;
        }
    }

    #endregion

    #region Sprinting Logic

    private void HandleSprinting()
    {
        if (!playerProperties.sprinting || playerProperties.is_firing) return;

        currentPositionSprinting += sprintDirection * Time.deltaTime * 8f;

        if (!isGrounded)
        {
            currentPositionSprinting = 0f;
            return;
        }

        if (currentPositionSprinting >= MaxPositionSprinting)
        {
            currentPositionSprinting = MaxPositionSprinting;
            sprintDirection = -1;
        }
        else if (currentPositionSprinting <= -MaxPositionSprinting)
        {
            currentPositionSprinting = -MaxPositionSprinting;
            sprintDirection = 1;
        }
    }

    private void StoreWeapon()
    {
        bool shouldStoreWeapon = (playerProperties.sprinting || playerProperties.roll) &&
                                 !playerProperties.is_reloading &&
                                 !playerProperties.is_firing;

        if (shouldStoreWeapon || SettingsHUD.Instance.is_menu_settings_active)
        {
            sprintTargetWeaponPosition = initialPosition + new Vector3(
                vector3Values[0],
                vector3Values[1],
                vector3Values[2]
            );
            sprintTargetWeaponRotation = initialRotation * Quaternion.Euler(
                quaternionValues[0],
                quaternionValues[1],
                quaternionValues[2]
            );
        }
        else
        {
            sprintTargetWeaponPosition = initialPosition;
            sprintTargetWeaponRotation = initialRotation;
        }
    }

    #endregion

    #region Input Handling

    private void UpdateInputs()
    {
        walkInput.x = playerController.moveHorizontal;
        walkInput.y = playerController.moveForward;
        walkInput.Normalize();

        lookInput.x = -InputManager.GetAxis("Mouse X");
        lookInput.y = InputManager.GetAxis("Mouse Y");
    }

    #endregion

    #region Sway Logic

    private void UpdateSway()
    {
        Vector3 invertedLook = (lookInput * -step).normalized;
        invertedLook.x = Mathf.Clamp(invertedLook.x, -maxStepDistance, maxStepDistance);
        invertedLook.y = Mathf.Clamp(invertedLook.y, -maxStepDistance, maxStepDistance);
        swayPos = invertedLook;
    }

    private void UpdateSwayRotation()
    {
        Vector2 invertedLook = (lookInput * -rotationStep).normalized;
        invertedLook.x = Mathf.Clamp(invertedLook.x, -maxRotationStep, maxRotationStep);
        invertedLook.y = Mathf.Clamp(invertedLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertedLook.y, invertedLook.x, invertedLook.x).normalized;
    }

    #endregion

    #region Bobbing Logic

    private void UpdateBobOffset()
    {
        float movementInput = (Mathf.Abs(InputManager.GetAxis("Vertical")) > 0.01f ||
                               Mathf.Abs(InputManager.GetAxis("Horizontal")) > 0.01f) ? 1f : 0f;

        float deltaTime = Time.deltaTime;
        speedCurve += deltaTime * (isGrounded ? movementInput * bobExaggeration : 1f) + 0.005f;

        float cosCurve = CurveCos;
        float sinCurve = CurveSin;
        float groundedMultiplier = isGrounded ? 1f : 0f;

        bobPosition.x = (cosCurve * bobLimit.x * groundedMultiplier) - (walkInput.x * travelLimit.x);
        bobPosition.y = (sinCurve * bobLimit.y) - (InputManager.GetAxis("Vertical") * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    private void UpdateBobRotation()
    {
        bool isMoving = walkInput != Vector2.zero;
        float sin2x = Mathf.Sin(2f * speedCurve);
        float cosCurve = CurveCos;

        bobEulerRotation.x = currentMultiplier.x * sin2x * (isMoving ? 1f : 0.5f);
        bobEulerRotation.y = isMoving ? currentMultiplier.y * cosCurve : 0f;
        bobEulerRotation.z = isMoving ? currentMultiplier.z * cosCurve * walkInput.x : 0f;
    }

    #endregion

    #region Composite Position & Rotation

    private void UpdateCompositePositionRotation()
    {
        UpdateInputs();

        float deltaTime = Time.deltaTime;
        float clampedX = Mathf.Clamp(lookInput.x, -5f, 5f);
        float clampedY = Mathf.Clamp(lookInput.y, -5f, 5f);

        float enhancedYRotation = clampedX * YRotationMultiplier;
        float enhancedZRotation = clampedX * ZRotationMultiplier;
        float enhancedXRotation = clampedY * XRotationMultiplier;

        float verticalAimMoveRotation = CalculateAimMoveRotation();

        Quaternion combinedRotation = CalculateCombinedRotation(enhancedXRotation, verticalAimMoveRotation);
        Vector3 combinedPosition = CalculateCombinedPosition();

        combinedRotation *= Quaternion.Euler(shakeOffset);

        if (!isGrounded)
        {
            ApplyAirborneState(deltaTime, enhancedYRotation, enhancedZRotation, combinedRotation, combinedPosition);
        }
        else
        {
            ApplyGroundedState(deltaTime, enhancedXRotation, enhancedYRotation, enhancedZRotation, combinedRotation, combinedPosition);
        }
    }

    #endregion

    #region Aim Movement Rotation

    private float CalculateAimMoveRotation()
    {
        if (!isAiming) return 0f;

        bool isMoving = !Mathf.Approximately(playerController.moveForward, 0f) ||
                       !Mathf.Approximately(playerController.moveHorizontal, 0f);

        if (!isMoving || isSprinting || isReloading || isRolling) return 0f;

        float sinTime = Mathf.Sin(Time.time * aimMoveRotationSpeed);
        float rotation = sinTime * aimMoveRotationAmount;

        if (isCrouched || isProne)
        {
            rotation *= 0.5f;
        }

        return rotation;
    }

    #endregion

    #region Combined Calculation

    private Quaternion CalculateCombinedRotation(float enhancedXRotation, float verticalAimMoveRotation)
    {
        Quaternion baseRotation;

        if (!isAiming)
        {
            baseRotation = sprintTargetWeaponRotation *
                          Quaternion.Euler(swayEulerRot) *
                          Quaternion.Euler(bobEulerRotation);
        }
        else
        {
            float divisor = isFiring ? 20f : 5f;

            Vector3 aimingRotation = swayEulerRot / divisor;
            aimingRotation.x += verticalAimMoveRotation;

            baseRotation = sprintTargetWeaponRotation *
                          Quaternion.Euler(aimingRotation) *
                          Quaternion.Euler(bobEulerRotation / divisor);
        }

        return baseRotation;
    }

    private Vector3 CalculateCombinedPosition()
    {
        if (!isAiming)
        {
            return sprintTargetWeaponPosition + swayPos + bobPosition;
        }

        float divisor = isFiring ? 20f : 5f;
        return sprintTargetWeaponPosition + (swayPos / divisor) + (bobPosition / divisor);
    }

    #endregion

    #region State Application

    private void ApplyAirborneState(
        float deltaTime,
        float enhancedYRotation,
        float enhancedZRotation,
        Quaternion combinedRotation,
        Vector3 combinedPosition)
    {
        float tiltAmount = InputManager.GetAxis("Horizontal") * 10f;
        Quaternion targetRotation = Quaternion.Euler(15f, enhancedYRotation, -tiltAmount + enhancedZRotation);

        myTransform.localRotation = Quaternion.Lerp(
            myTransform.localRotation,
            combinedRotation * targetRotation,
            deltaTime * smoothRot
        );

        myTransform.localPosition = Vector3.Lerp(
            myTransform.localPosition,
            new Vector3(combinedPosition.x, combinedPosition.y - 0.01f, combinedPosition.z),
            deltaTime * smooth
        );
    }

    private void ApplyGroundedState(
        float deltaTime,
        float enhancedXRotation,
        float enhancedYRotation,
        float enhancedZRotation,
        Quaternion combinedRotation,
        Vector3 combinedPosition)
    {
        Quaternion targetRotation = isFiring
            ? Quaternion.Euler(enhancedXRotation / 8f, enhancedYRotation, -enhancedZRotation)
            : Quaternion.Euler(enhancedXRotation, enhancedYRotation, -enhancedZRotation);

        myTransform.localRotation = Quaternion.Lerp(
            myTransform.localRotation,
            combinedRotation * targetRotation,
            deltaTime * smoothRot
        );

        myTransform.localPosition = Vector3.Lerp(
            myTransform.localPosition,
            combinedPosition,
            deltaTime * smooth
        );

    }

    #endregion
}