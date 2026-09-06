using FishNet.Object;
using UnityEngine;

public class CameraRotation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform horizontalRotation;
    [SerializeField] private Transform verticalRotation;
    [SerializeField] private Transform playerHead;
    [SerializeField] private ProcessCameraRecoil processCameraRecoil;
    [SerializeField] private PlayerProperties playerProperties;

    private float currentMouseSensitivity;
    private float pitch;
    private float yaw;
    private float currentRecoilZ;
    private float currentHorizontalRecoil;
    private bool initialized;
    private float lastVerticalRecoil = 0f;
    private bool wasInVehicle;
    private bool lookInputBlocked;

    public Quaternion PlanarRotation => initialized
        ? Quaternion.Euler(0f, yaw + currentHorizontalRecoil, 0f)
        : Quaternion.Euler(0f, horizontalRotation != null ? horizontalRotation.eulerAngles.y : 0f, 0f);

    public Vector3 PlanarForward => PlanarRotation * Vector3.forward;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner) InitializeLookState();
    }

    private void Update()
    {
        if (!IsOwner || playerProperties == null) return;

        EnsureInitialized();

        if (wasInVehicle && !playerProperties.isInVehicle) SynchronizeYawWithBody();
        wasInVehicle = playerProperties.isInVehicle;

        if (playerProperties.isDead.Value) return;

        UpdateMouseSensitivity();

        if (playerProperties.roll || playerProperties.isInVehicle) return;

        if (!lookInputBlocked) ReadLookInput();
        ReadAndApplyRecoil();
        ApplyHorizontalRotation();
    }

    private void LateUpdate()
    {
        if (!IsOwner || !initialized || playerProperties == null || playerProperties.isDead.Value || playerProperties.roll || playerProperties.isInVehicle) return;

        ApplyCameraRotation(currentRecoilZ);
        UpdateHeadRotation();
    }

    private void EnsureInitialized()
    {
        if (!initialized) InitializeLookState();
    }

    private void InitializeLookState()
    {
        if (horizontalRotation == null || verticalRotation == null) return;

        yaw = horizontalRotation.eulerAngles.y;
        pitch = NormalizeSignedAngle(verticalRotation.localEulerAngles.x);

        UpdateMouseSensitivity();
        wasInVehicle = playerProperties != null && playerProperties.isInVehicle;
        initialized = true;
    }

    public void SynchronizeYawWithBody()
    {
        if (horizontalRotation == null) return;

        yaw = horizontalRotation.eulerAngles.y;
    }

    public void SetLookInputBlocked(bool blocked) => lookInputBlocked = blocked;

    private void UpdateMouseSensitivity()
    {
        if (playerProperties == null) return;

        currentMouseSensitivity = playerProperties.aiming
            ? Settings.Instance._controls.infantary_aim_sensibility
            : Settings.Instance._controls.infantary_sensibility;
    }

    private void ReadLookInput()
    {
        float mouseX = InputManager.GetAxis("Mouse X") * currentMouseSensitivity;
        float mouseY = InputManager.GetAxis("Mouse Y") * currentMouseSensitivity;

        if (Settings.Instance._controls.invert_vertical_infantary_mouse) mouseY *= -1f;

        yaw += mouseX;
        pitch -= mouseY;

        bool isProned = playerProperties != null && playerProperties.proned;
        pitch = isProned ? Mathf.Clamp(pitch, -20f, 80f) : Mathf.Clamp(pitch, -80f, 70f);
    }

    private void ReadAndApplyRecoil()
    {
        float horizontalRecoil = 0f;
        float verticalRecoil = 0f;
        float recoilZ = 0f;

        if (processCameraRecoil != null)
            processCameraRecoil.ProcessRecoil(out horizontalRecoil, out verticalRecoil, out recoilZ);

        currentHorizontalRecoil = horizontalRecoil;

        float verticalDelta = verticalRecoil - lastVerticalRecoil;
        pitch -= verticalDelta;
        lastVerticalRecoil = verticalRecoil;

        bool isProned = playerProperties != null && playerProperties.proned;
        pitch = isProned ? Mathf.Clamp(pitch, -20f, 80f) : Mathf.Clamp(pitch, -80f, 70f);

        currentRecoilZ = recoilZ;
    }

    private void ApplyHorizontalRotation()
    {
        if (horizontalRotation != null) horizontalRotation.rotation = PlanarRotation;
    }

    private void ApplyCameraRotation(float recoilZ)
    {
        if (verticalRotation == null) return;

        Quaternion bodyRotation = horizontalRotation != null
            ? horizontalRotation.rotation
            : Quaternion.identity;

        Quaternion parentOffset = verticalRotation.parent != null
            ? Quaternion.Inverse(bodyRotation) * verticalRotation.parent.rotation
            : Quaternion.identity;

        Quaternion viewYaw = Quaternion.Euler(0f, yaw + currentHorizontalRecoil, 0f);

        Quaternion viewPitchAndRoll = Quaternion.Euler(pitch, 0f, recoilZ);

        verticalRotation.rotation = viewYaw * parentOffset * viewPitchAndRoll;
    }

    public void UpdateHeadRotation()
    {
        if (playerHead != null && verticalRotation != null)
            playerHead.rotation = verticalRotation.rotation;
    }

    public void ApplyCameraRecoil(float verticalRecoil, float horizontalRecoil)
    {
        if (processCameraRecoil != null)
            processCameraRecoil.ApplyRecoil(verticalRecoil, horizontalRecoil);
    }

    public void ResetRecoilPreservingAim()
    {
        if (initialized)
        {
            yaw += currentHorizontalRecoil;
            currentHorizontalRecoil = 0f;
            lastVerticalRecoil = 0f;
            currentRecoilZ = 0f;
        }

        if (processCameraRecoil != null) processCameraRecoil.ResetState();
        if (IsOwner && initialized) ApplyHorizontalRotation();
    }

    private static float NormalizeSignedAngle(float angle) => angle > 180f ? angle - 360f : angle;
}
