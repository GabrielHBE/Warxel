using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class WeaponInspect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform inspectionTransform;
    [SerializeField] private Weapon weapon;
    [SerializeField] private SwitchWeapon switchWeapon;
    [SerializeField] private PlayerProperties playerProperties;
    [SerializeField] private CameraRotation cameraRotation;
    [SerializeField] private SwayNBobScript swayAndBob;

    [Header("Input")]
    [SerializeField] private KeyCode inspectionKey = KeyCode.I;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 8f;
    [SerializeField] private Vector2 verticalRotationLimits = new(-60f, 60f);
    [SerializeField] private Vector2 horizontalRotationLimits = new(-80f, 80f);

    [Header("Position")]
    [SerializeField] private Vector3 inspectionPositionOffset = new(-0.12f, 0.08f, 0f);
    [Tooltip("Amount of local position added for each mouse input step.")]
    [SerializeField, Min(0f)] private float positionMouseSensitivity = 0.01f;
    [Tooltip("Maximum horizontal and vertical position offsets controlled by the mouse.")]
    [SerializeField] private Vector2 mousePositionLimits = new(0.2f, 0.15f);

    [Header("Smoothing")]
    [SerializeField, Min(0f)] private float rotationSmoothSpeed = 15f;
    [SerializeField, Min(0f)] private float returnSmoothSpeed = 18f;

    public bool IsInspecting { get; private set; }

    private Transform inspectionPivot;
    private Vector3 restLocalPosition;
    private Vector3 restPivotParentLocalPosition;
    private Vector3 pivotLocalPosition;
    private Vector3 currentPositionOffset;
    private Quaternion restLocalRotation;
    private Vector2 inspectionAngles;
    private Vector2 mousePositionOffset;
    private bool isReturning;

    private void Awake()
    {
        CacheReferences();
        restLocalPosition = inspectionTransform.localPosition;
        restLocalRotation = inspectionTransform.localRotation;
    }

    private void Update()
    {
        if (!IsLocalPlayer())
        {
            if (IsInspecting || isReturning) StopInspection(true);
            return;
        }

        bool inspectionKeyPressed = InputManager.GetKeyDown(inspectionKey);

        if (IsInspecting)
        {
            if (ShouldCancelForAction() || !CanContinueInspection())
            {
                StopInspection(true);
                return;
            }

            if (inspectionKeyPressed)
            {
                StopInspection(false);
                return;
            }

            ReadInspectionInput();
            return;
        }

        if (inspectionKeyPressed) TryStartInspection();
    }

    private void LateUpdate()
    {
        if ((!IsInspecting && !isReturning) || inspectionTransform == null) return;

        Quaternion targetRotation = IsInspecting
            ? restLocalRotation * Quaternion.Euler(inspectionAngles.x, inspectionAngles.y, 0f)
            : restLocalRotation;

        float smoothSpeed = IsInspecting ? rotationSmoothSpeed : returnSmoothSpeed;
        float interpolation = smoothSpeed <= 0f
            ? 1f
            : 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        Quaternion smoothedRotation = Quaternion.Slerp(
            inspectionTransform.localRotation,
            targetRotation,
            interpolation);
        Vector3 desiredPositionOffset = IsInspecting
            ? inspectionPositionOffset + new Vector3(mousePositionOffset.x, mousePositionOffset.y, 0f)
            : Vector3.zero;
        currentPositionOffset = Vector3.Lerp(
            currentPositionOffset,
            desiredPositionOffset,
            interpolation);

        inspectionTransform.localRotation = smoothedRotation;
        inspectionTransform.localPosition = CalculatePivotedLocalPosition(
            smoothedRotation,
            currentPositionOffset);

        bool rotationReturned = Quaternion.Angle(inspectionTransform.localRotation, restLocalRotation) <= 0.05f;
        bool positionReturned = currentPositionOffset.sqrMagnitude <= 0.000001f;
        if (!isReturning || !rotationReturned || !positionReturned) return;

        currentPositionOffset = Vector3.zero;
        inspectionTransform.localPosition = restLocalPosition;
        inspectionTransform.localRotation = restLocalRotation;
        isReturning = false;
    }

    private void OnDisable()
    {
        if (inspectionTransform != null && (IsInspecting || isReturning)) StopInspection(true);
    }

    private void CacheReferences()
    {
        if (inspectionTransform == null) inspectionTransform = transform;
        if (weapon == null) weapon = GetComponent<Weapon>();
        if (switchWeapon == null) switchWeapon = GetComponentInParent<SwitchWeapon>();
        if (playerProperties == null) playerProperties = GetComponentInParent<PlayerProperties>();

        if (playerProperties != null)
        {
            if (cameraRotation == null) cameraRotation = playerProperties.GetComponentInChildren<CameraRotation>(true);
            if (swayAndBob == null) swayAndBob = playerProperties.GetComponentInChildren<SwayNBobScript>(true);
        }
    }

    private void TryStartInspection()
    {
        CacheReferences();
        if (!CanStartInspection() || !TryCacheInspectionPivot()) return;

        if (isReturning)
        {
            currentPositionOffset = Vector3.zero;
            inspectionTransform.localPosition = restLocalPosition;
            inspectionTransform.localRotation = restLocalRotation;
            isReturning = false;
        }

        restLocalPosition = inspectionTransform.localPosition;
        restLocalRotation = inspectionTransform.localRotation;
        pivotLocalPosition = inspectionTransform.InverseTransformPoint(inspectionPivot.position);
        restPivotParentLocalPosition = inspectionTransform.parent != null
            ? inspectionTransform.parent.InverseTransformPoint(inspectionPivot.position)
            : inspectionPivot.position;
        currentPositionOffset = Vector3.zero;
        inspectionAngles = Vector2.zero;
        mousePositionOffset = Vector2.zero;
        IsInspecting = true;
        SetLookInputBlocked(true);
    }

    public void StopInspection(bool immediately = true)
    {
        if (!IsInspecting && !isReturning) return;

        IsInspecting = false;
        inspectionAngles = Vector2.zero;
        mousePositionOffset = Vector2.zero;
        SetLookInputBlocked(false);

        if (immediately)
        {
            currentPositionOffset = Vector3.zero;
            inspectionTransform.localPosition = restLocalPosition;
            inspectionTransform.localRotation = restLocalRotation;
            isReturning = false;
            return;
        }

        isReturning = true;
    }

    private void ReadInspectionInput()
    {
        float mouseX = InputManager.GetAxis("Mouse X");
        float mouseY = InputManager.GetAxis("Mouse Y");

        inspectionAngles.x = Mathf.Clamp(
            inspectionAngles.x - mouseY * mouseSensitivity,
            verticalRotationLimits.x,
            verticalRotationLimits.y);

        inspectionAngles.y = Mathf.Clamp(
            inspectionAngles.y + mouseX * mouseSensitivity,
            horizontalRotationLimits.x,
            horizontalRotationLimits.y);

        float horizontalPositionLimit = Mathf.Abs(mousePositionLimits.x);
        float verticalPositionLimit = Mathf.Abs(mousePositionLimits.y);

        mousePositionOffset.x = Mathf.Clamp(
            mousePositionOffset.x + mouseX * positionMouseSensitivity,
            -horizontalPositionLimit,
            horizontalPositionLimit);

        mousePositionOffset.y = Mathf.Clamp(
            mousePositionOffset.y + mouseY * positionMouseSensitivity,
            -verticalPositionLimit,
            verticalPositionLimit);
    }

    private bool CanStartInspection()
    {
        return inspectionTransform != null &&
               weapon != null &&
               weapon.is_active &&
               CanContinueInspection() &&
               !ShouldCancelForAction();
    }

    private bool CanContinueInspection()
    {
        if (inspectionTransform == null || weapon == null || playerProperties == null) return false;
        if (!weapon.is_active || !inspectionTransform.gameObject.activeInHierarchy) return false;
        if (switchWeapon != null && switchWeapon.IsSwitchingWeapon) return false;
        if (SettingsHUD.Instance != null && SettingsHUD.Instance.is_menu_settings_active) return false;

        return !playerProperties.aiming &&
               !playerProperties.reloading &&
               !playerProperties.firing &&
               !playerProperties.roll &&
               !playerProperties.isInVehicle &&
               !playerProperties.isDead.Value;
    }

    private bool TryCacheInspectionPivot()
    {
        WeaponProperties activeWeapon = inspectionTransform.GetComponentInChildren<WeaponProperties>();
        if (activeWeapon == null) return false;

        EquippableItemHandTargets handTargets = activeWeapon.GetComponent<EquippableItemHandTargets>() ??
                                                activeWeapon.GetComponentInChildren<EquippableItemHandTargets>();
        if (handTargets == null) return false;

        inspectionPivot = handTargets.GetRightHandPos();
        return inspectionPivot != null && inspectionPivot.IsChildOf(inspectionTransform);
    }

    private Vector3 CalculatePivotedLocalPosition(Quaternion localRotation, Vector3 positionOffset)
    {
        Vector3 scaledPivotPosition = Vector3.Scale(pivotLocalPosition, inspectionTransform.localScale);
        return restPivotParentLocalPosition + positionOffset - localRotation * scaledPivotPosition;
    }

    private bool ShouldCancelForAction()
    {
        if (Settings.Instance == null) return false;

        return InputManager.GetKey(Settings.Instance._keybinds.WEAPON_shootKey) ||
               InputManager.GetKeyDown(Settings.Instance._keybinds.WEAPON_reloadKey) ||
               InputManager.GetKey(Settings.Instance._keybinds.WEAPON_aimKey);
    }

    private bool IsLocalPlayer() => playerProperties != null && playerProperties.IsOwner;

    private void SetLookInputBlocked(bool blocked)
    {
        cameraRotation?.SetLookInputBlocked(blocked);
        swayAndBob?.SetLookInputBlocked(blocked);
    }
}
