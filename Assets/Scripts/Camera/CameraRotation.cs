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

    [Header("Rotation State")]
    private float currentMouseSensitivity;
    private float _verticalRotation;
    private float yaw;

    void Update()
    {
        if(!IsOwner) return;

        RotateCamera();
        UpdateHeadRotation();
        UpdateMouseSensitivity();
    }

    public void Initialize(Transform pTransform, Transform pCamera, Transform pHead, ProcessCameraRecoil pRecoil, PlayerProperties pProperties)
    {
        horizontalRotation = pTransform;
        verticalRotation = pCamera;
        playerHead = pHead;
        processCameraRecoil = pRecoil;
        playerProperties = pProperties;

        if (horizontalRotation != null) yaw = horizontalRotation.eulerAngles.y;
        
    }

    public void UpdateMouseSensitivity()
    {
        if (playerProperties == null) return;

        currentMouseSensitivity = playerProperties.is_aiming ?
            Settings.Instance._controls.infantary_aim_sensibility :
            Settings.Instance._controls.infantary_sensibility;
    }

    public void RotateCamera()
    {
        if (playerProperties != null && (playerProperties.roll || playerProperties.is_in_vehicle)) return;

        float horizontalRecoil = 0f;
        float verticalRecoil = 0f;
        float recoilZ = 0f;

        // Obtém os deltas de recoil
        if (processCameraRecoil != null) processCameraRecoil.ProcessRecoil(out horizontalRecoil, out verticalRecoil, out recoilZ);
    
        HandleHorizontalRotation(horizontalRecoil);
        HandleVerticalRotation(verticalRecoil);
        ApplyCameraRotation(recoilZ);
    }

    private void HandleHorizontalRotation(float horizontalRecoil)
    {
        float mouseX = InputManager.GetAxis("Mouse X") * currentMouseSensitivity;

        // Soma input do mouse com recoil horizontal
        yaw += mouseX + horizontalRecoil;

        if (horizontalRotation != null) horizontalRotation.rotation = Quaternion.Euler(0f, yaw, 0f);
        
    }

    private void HandleVerticalRotation(float verticalRecoil)
    {
        float mouseVertical = InputManager.GetAxis("Mouse Y") * currentMouseSensitivity;

        if (Settings.Instance._controls.invert_vertical_infantary_mouse) mouseVertical *= -1;
        
        // Soma input do mouse com recoil vertical
        _verticalRotation -= (mouseVertical + verticalRecoil);

        bool isProned = playerProperties != null && playerProperties.is_proned;
        _verticalRotation = isProned ? Mathf.Clamp(_verticalRotation, -20f, 80f) : Mathf.Clamp(_verticalRotation, -80f, 70f);
    }

    private void ApplyCameraRotation(float recoilZ)
    {
        if (verticalRotation != null) verticalRotation.transform.localEulerAngles = new Vector3(_verticalRotation, 0f, recoilZ);
        
        UpdateHeadRotation();
    }

    public void UpdateHeadRotation()
    {
        if (playerHead != null && verticalRotation != null) playerHead.transform.rotation = verticalRotation.transform.rotation;
    }

    public void ApplyCameraRecoil(float verticalRecoil, float horizontalRecoil)
    {
        if (processCameraRecoil != null) processCameraRecoil.ApplyRecoil(verticalRecoil, horizontalRecoil);
    }
}