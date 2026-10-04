using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[Serializable]
public class VehicleSeats
{
    [Header("Seat Configuration")]
    public SeatType seatType;
    public VehicleCameraData[] seatCameras;
    public GameObject seatHUD;

    [Header("Seat Armory")]
    public VehicleArmory[] vehicleArmory;


    [Header("Player Hand IK Targets")]
    public Transform vehicleLeftHandTarget;
    public Transform vehicleRightHandTarget;

    [Header("Runtime References (Auto-assigned)")]
    [HideInInspector] public List<UIElementsColor> setHudElementColors = new List<UIElementsColor>();
    [HideInInspector] public CameraZoomController seatCameraZoomController;
    [HideInInspector] public bool isOccupied;
    [HideInInspector] public PlayerProperties playerProperties;
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public Rigidbody playerRigidbody;
    [HideInInspector] public GameObject playerGameObject;
    private ThirdPersonArms thirdPersonArms;

    [Header("Transform References")]
    public Transform playerSeat;
    public Transform exitPosition;

    [NonSerialized] public NetworkConnection authorizedConnection;
    private int activeCameraIndex;
    private Transform cameraLagPivot;
    private Quaternion appliedCameraLag = Quaternion.identity;
    private Vector3 cameraLagAngles;
    private Vector3 cameraPositionOffset;
    private Vector3 appliedLocalPositionOffset;
    private VehicleArmory currentArmory;
    private VehicleArmory cameraArmory;
    public bool IsArmoryCameraActive => cameraArmory != null;
    private CameraModifiers activeCameraModifier;
    private bool cameraModifierRequested;
    private bool appliedCameraModifierState;

    public void EnterSeat(PlayerProperties playerProperties, PlayerController playerController, Transform playerSeat, Rigidbody playerRigidbody, GameObject playerGameObject)
    {
        ResetCameraOffset();
        if (vehicleArmory != null && vehicleArmory.Length > 0)
        {
            currentArmory = vehicleArmory[0];
            if (currentArmory != null) currentArmory.ActivateArmory();
            else Debug.LogWarning($"The {seatType} seat has no valid IVehicleArmory assigned.");
        }

        this.playerProperties = playerProperties;
        this.playerController = playerController;
        this.playerRigidbody = playerRigidbody;
        this.playerGameObject = playerGameObject;
        this.playerSeat = playerSeat;
        this.playerProperties.isInVehicle = true;
        thirdPersonArms = playerController.GetComponentInChildren<ThirdPersonArms>();

        //if (playerAnimation != null) playerAnimation.SetVehicleIKTargets(vehicleLeftHandTarget, vehicleRightHandTarget);

        this.playerRigidbody.isKinematic = true;
        this.playerRigidbody.interpolation = RigidbodyInterpolation.None;
        this.playerProperties.reloading = false;
        this.playerProperties.isInVehicle = true;

        if (seatType != SeatType.Passenger)
        {
            thirdPersonArms.RequestToDisableWeapon();
            //playerAnimation.DeactivateCurrentWeapon();
            playerController.first_person_player_components.SetActive(false);
            playerController.HideOwnerItems(false);
        }
        else
        {
            thirdPersonArms.RequestToEnableWeapon();
            //playerAnimation.ActivateCurrentWeapon();
            playerController.first_person_player_components.SetActive(true);
            playerController.HideOwnerItems(true);
        }

        if (seatHUD != null)
        {
            if (seatHUD != null)
            {
                seatHUD.SetActive(true);
                UIElementsColor seatHudUIElementColor = seatHUD.GetComponent<UIElementsColor>();
                if (seatHudUIElementColor != null) setHudElementColors.Add(seatHudUIElementColor);
                foreach (UIElementsColor e in seatHUD.GetComponentsInChildren<UIElementsColor>())
                {
                    setHudElementColors.Add(e);
                }

                foreach (UIElementsColor e in setHudElementColors)
                {
                    e.GetComponent<UIElementsColor>().SetColor(Color.limeGreen, 2);
                }
            }
            if (playerController.soldierHudManager != null) playerController.soldierHudManager.ActivateInVehicleHUD();

        }

        for (int i = 0; i < seatCameras.Length; i++)
        {
            EnableCamera(seatCameras[i].camera, false);
            CameraZoomController zoomController = seatCameras[i].camera.GetComponent<CameraZoomController>();
            if (zoomController != null) zoomController.SetBaseFOV(60);
            seatCameras[i].InitializeCameraModifier();

            if (seatCameras[i].isMainCamera) activeCameraIndex = i;
        }
        EnableCamera(GetCurrentCamera(), true);
        activeCameraModifier = null;
        cameraModifierRequested = false;
        appliedCameraModifierState = false;
        ActivateCameraEffect(false);

        playerController.playerCamera.enabled = false;
        playerController.playerCamera.GetComponent<AudioListener>().enabled = false;

        isOccupied = true;

    }

    public void ExitSeat()
    {
        //Player Animation Exit State
        if (thirdPersonArms != null)
        {
            //playerAnimation.ActivateCurrentWeapon();
            //playerAnimation.SetVehicleIKTargets(null, null);
        }

        //Player Controls Exit State
        if (playerController != null)
        {
            playerController.first_person_player_components.SetActive(true);
            playerController.HideOwnerItems(true);
            if (playerProperties != null) playerProperties.isInVehicle = false;
        }

        //Player GameObject Exit State
        if (playerGameObject != null)
        {
            if (!playerGameObject.activeSelf) playerGameObject.SetActive(true);
            playerGameObject.transform.SetParent(null); // Detach the player from the vehicle seat
        }

        //Player Rigidbody Exit State
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (playerController != null && playerController.playerCamera != null)
            EnableCamera(playerController.playerCamera, true);
        ClearReferences();
    }

    public void ClearReferences()
    {
        ResetCameraOffset();
        activeCameraModifier?.SetActive(false);
        activeCameraModifier = null;
        cameraModifierRequested = false;
        appliedCameraModifierState = false;
        cameraArmory?.SetCameraActive(false);
        cameraArmory = null;
        DeactivateAllArmory();
        if (thirdPersonArms != null)
        {
            thirdPersonArms.RequestToEnableWeapon();
            //playerAnimation.SetVehicleIKTargets(null, null);
        }

        EnableCamera(GetCurrentCamera(), false);

        if (seatHUD != null)
        {
            if (playerController != null && playerController.soldierHudManager != null)
                playerController.soldierHudManager.ActivateStandardHUD();
            seatHUD.SetActive(false);
        }

        playerRigidbody = null;
        playerGameObject = null;
        playerProperties = null;
        playerController = null;
        currentArmory = null;
        isOccupied = false;
    }

    private void DeactivateAllArmory()
    {
        if (vehicleArmory == null) return;

        foreach (VehicleArmory armory in vehicleArmory)
        {
            if (armory == null) continue;

            armory.DeactivateArmory();
        }
    }

    // Verifica se a conexão tem autoridade sobre as armas deste assento
    public bool HasAuthority(NetworkConnection conn)
    {
        return authorizedConnection != null && authorizedConnection == conn;
    }

    // Define a autoridade das armas para uma conexão específica
    public void SetAuthority(NetworkConnection conn)
    {
        authorizedConnection = conn;

        if (vehicleArmory == null) return;

        foreach (VehicleArmory armoryObj in vehicleArmory)
        {
            if (armoryObj == null) continue;

            NetworkObject nObj = armoryObj.GetComponent<NetworkObject>();
            if (nObj != null)
            {
                if (conn != null) nObj.GiveOwnership(conn);
                else nObj.RemoveOwnership();
            }
        }
    }

    public VehicleArmory GetCurrentArmory() => currentArmory;
    public void SetCurrentArmory(VehicleArmory armmory)
    {
        currentArmory = armmory;
        ActivateCameraEffect(cameraModifierRequested);
    }

    #region Camera
    public void RemoveCameraOffset()
    {
        if (cameraLagPivot != null)
        {
            cameraLagPivot.localRotation *= Quaternion.Inverse(appliedCameraLag);
            cameraLagPivot.localPosition -= appliedLocalPositionOffset;
        }

        appliedCameraLag = Quaternion.identity;
        appliedLocalPositionOffset = Vector3.zero;
    }

    public void UpdateCameraOffset(Vector3 angularVelocity, Vector3 worldVelocity, float deltaTime,
        float defaultStrength, float smoothing, float defaultMaxAngle)
    {
        if (IsArmoryCameraActive)
        {
            ResetCameraOffset();
            return;
        }
        VehicleCameraData cameraData = seatCameras[activeCameraIndex];
        if (!cameraData.applyCameraOffset || cameraData.camera == null ||
            !cameraData.camera.isActiveAndEnabled || cameraData.rotationPivot == null)
        {
            ResetCameraOffset();
            return;
        }

        if (cameraLagPivot != cameraData.rotationPivot)
        {
            ResetCameraOffset();
            cameraLagPivot = cameraData.rotationPivot;
        }

        if (deltaTime <= 0f) return;
        float strength = cameraData.cameraRotationLagStrength > 0f
            ? cameraData.cameraRotationLagStrength
            : defaultStrength;
        float maxAngle = cameraData.cameraRotationLagMaxAngle > 0f
            ? cameraData.cameraRotationLagMaxAngle
            : defaultMaxAngle;
        Vector3 targetAngles = new Vector3(
            Mathf.Clamp(-angularVelocity.x * strength, -maxAngle, maxAngle),
            Mathf.Clamp(-angularVelocity.y * strength, -maxAngle, maxAngle),
            Mathf.Clamp(-angularVelocity.z * strength, -maxAngle, maxAngle));

        float blend = 1f - Mathf.Exp(-deltaTime / Mathf.Max(0.01f, smoothing));
        cameraLagAngles = Vector3.Lerp(cameraLagAngles, targetAngles, blend);
        appliedCameraLag = Quaternion.Euler(cameraLagAngles);
        cameraLagPivot.localRotation *= appliedCameraLag;

        Vector3 targetPositionOffset = Vector3.ClampMagnitude(
            -worldVelocity * cameraData.cameraPositionOffsetPerSpeed,
            Mathf.Max(0f, cameraData.cameraPositionOffsetMaxDistance));
        float positionBlend = 1f - Mathf.Exp(-deltaTime /
            Mathf.Max(0.01f, cameraData.cameraPositionOffsetSmoothing));
        cameraPositionOffset = Vector3.Lerp(cameraPositionOffset, targetPositionOffset, positionBlend);
        appliedLocalPositionOffset = cameraLagPivot.parent != null
            ? cameraLagPivot.parent.InverseTransformVector(cameraPositionOffset)
            : cameraPositionOffset;
        cameraLagPivot.localPosition += appliedLocalPositionOffset;
    }

    private void ResetCameraOffset()
    {
        RemoveCameraOffset();
        cameraLagPivot = null;
        cameraLagAngles = Vector3.zero;
        cameraPositionOffset = Vector3.zero;
    }

    public Camera GetCurrentCamera() => IsArmoryCameraActive ? cameraArmory.ArmoryCamera : seatCameras[activeCameraIndex].camera;
    public Transform GetCurrentCameraRotationPivot() => IsArmoryCameraActive ? cameraArmory.CameraRotationPivot : seatCameras[activeCameraIndex].rotationPivot;
    public bool IsCurrentCameraMain() => !IsArmoryCameraActive && seatCameras[activeCameraIndex].isMainCamera;
    public bool CanMainCameraFreeLook() => IsArmoryCameraActive
        ? cameraArmory.CanUseCamera && cameraArmory.ArmoryCamera.isActiveAndEnabled && cameraArmory.CanRotateCamera
        : seatCameras[activeCameraIndex].canFreeLook;
    public void ActivateCameraEffect(bool state)
    {
        cameraModifierRequested = state;
        VehicleArmory requestedArmory = state && currentArmory != null && currentArmory.CanUseCamera
            ? currentArmory : null;
        if (cameraArmory != requestedArmory)
        {
            ResetCameraOffset();
            cameraArmory?.SetCameraActive(false);
            cameraArmory = requestedArmory;
            EnableCamera(seatCameras[activeCameraIndex].camera, !IsArmoryCameraActive);
        }
        cameraArmory?.SetCameraActive(true);
        state = state && !IsArmoryCameraActive;
        CameraModifiers modifier = seatCameras[activeCameraIndex].GetCurrentCameraModifier();
        if (modifier == activeCameraModifier && state == appliedCameraModifierState) return;

        if (activeCameraModifier != null && activeCameraModifier != modifier)
            activeCameraModifier.SetActive(false);

        activeCameraModifier = modifier;
        appliedCameraModifierState = state;
        modifier?.SetActive(state);
    }

    public void SwitchCamera()
    {
        ResetCameraOffset();
        activeCameraModifier?.SetActive(false);
        activeCameraModifier = null;
        appliedCameraModifierState = false;
        EnableCamera(seatCameras[activeCameraIndex].camera, false);

        if (activeCameraIndex == seatCameras.Length - 1) activeCameraIndex = 0;
        else activeCameraIndex += 1;

        EnableCamera(seatCameras[activeCameraIndex].camera, !IsArmoryCameraActive);
        ActivateCameraEffect(cameraModifierRequested);
    }

    private void EnableCamera(Camera camera, bool state)
    {
        if (camera == null) return;
        camera.enabled = state;
        AudioListener listener = camera.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = state;
    }
    #endregion

    #region enums / structs
    public enum SeatType
    {
        Pilot,
        Passenger,
        Gunner
    }

    [Serializable]
    public class VehicleCameraData
    {
        public Camera camera;
        public Transform rotationPivot;
        public bool isMainCamera;
        public bool canFreeLook;
        public CameraModifier cameraModifier = CameraModifier.Zoom;
        public bool applyCameraOffset = true;
        [Min(0f), Tooltip("Intensidade do offset de rotacao. Zero usa o valor anterior do veiculo.")]
        public float cameraRotationLagStrength = 0.12f;
        [Range(0f, 30f), Tooltip("Angulo maximo do offset. Zero usa o valor anterior do veiculo.")]
        public float cameraRotationLagMaxAngle = 15;
        [Min(0f), Tooltip("Metros de deslocamento oposto por unidade de velocidade. Zero desativa o offset de posicao.")]
        public float cameraPositionOffsetPerSpeed = 0.05f;
        [Min(0f), Tooltip("Distancia maxima do offset de posicao, em metros.")]
        public float cameraPositionOffsetMaxDistance = 0.1f;
        [Min(0.01f), Tooltip("Tempo de suavizacao do offset de posicao, em segundos.")]
        public float cameraPositionOffsetSmoothing = 0.18f;

        private CameraModifiers currentCameraModifier;

        public void InitializeCameraModifier()
        {
            currentCameraModifier = null;
            if (camera == null) return;

            switch (cameraModifier)
            {
                case CameraModifier.None:
                    return;

                case CameraModifier.Zoom:
                    CameraZoomController cameraZoomController = camera.GetComponent<CameraZoomController>();
                    currentCameraModifier = cameraZoomController;
                    if (cameraZoomController != null) cameraZoomController.SetZoomMultiplier(2);
                    break;

                case CameraModifier.NightVision:
                    currentCameraModifier = camera.GetComponentInChildren<NightVisionPostFX>(true);
                    break;
            }

            if (currentCameraModifier == null)
                Debug.LogWarning($"Camera modifier {cameraModifier} was not found on {camera.name}.");

        }

        public CameraModifiers GetCurrentCameraModifier() => currentCameraModifier;

        public enum CameraModifier { None, Zoom, NightVision }
    }
    #endregion
}
