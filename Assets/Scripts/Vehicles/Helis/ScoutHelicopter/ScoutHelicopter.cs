using UnityEngine;

public class ScoutHelicopter : Helicopter
{

    #region INITIALIZATION

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsSpawned)
        {
            Debug.LogError($"{gameObject.name} : ScoutHelicopter not spawned in network yet");
            return;
        }

        throttle = 0;
        SetHpProperties(heliProperties.hp, heliProperties.resistance);

    }

    #endregion

    #region UNITY LIFECYCLE

    protected override void Update()
    {
        if (!IsPlayerValid())
            return;

        if (IsPlayerDead())
            return;

        PropellerRotation();

        if (is_in_vehicle)
        {
            SwitchWeapon();

            if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
            {
                Zoom();

                currentSeat.currentArmory.Shoot();
            }

            SyncPlayerPosition();
            HandleDebugInput();

            if (!SettingsHUD.Instance.is_menu_settings_active)
                HandleVehicleInput();
        }
    }

    protected override void FixedUpdate()
    {
        if (CanMove())
        {
            Move();
            Rotate();
            rb.AddForce(liftDirection * throttle, ForceMode.Acceleration);
        }
        else
        {
            HandleIdleState();
        }
    }

    #endregion

    #region VALIDATION METHODS

    private bool IsPlayerValid()
    {
        if (currentSeat != null && currentSeat.playerGameObject != null)
            return true;

        if (is_in_vehicle)
        {
            Debug.LogWarning("Player reference lost, exiting vehicle");
            ExitVehicle();
        }

        return false;
    }

    private bool IsPlayerDead()
    {
        if (currentSeat?.playerProperties != null && currentSeat.playerProperties.is_dead.Value)
        {
            ExitVehicle();
            return true;
        }

        return false;
    }

    private bool CanMove()
    {
        return start_engine &&
               is_in_vehicle &&
               !SettingsHUD.Instance.is_menu_settings_active &&
               !vehicle_destroyed.Value &&
               currentSeat != null &&
               currentSeat.seatType == VehicleSeats.SeatType.Pilot;
    }

    private void HandleIdleState()
    {
        if (vehicle_destroyed.Value)
        {
            DestroyAnimation();
        }

        throttle = 0;
        rb.AddForce(Vector3.down * 50, ForceMode.Acceleration);
    }

    #endregion

    #region INPUT HANDLING

    private void HandleVehicleInput()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot && !vehicle_destroyed.Value) StartStopEngine();

        FreeLook();

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchSeatKey)) SwitchSeats();

        if (start_engine && !vehicle_destroyed.Value)
            HandleThrottleControls();

        HandleExitVehicle();
    }

    #endregion

    // ==================== FREELOOK SYSTEM ====================

    #region FREELOOK SYSTEM

    private void FreeLook()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
            HandlePilotFreeLook();
        else
            ApplyFreeLookRotation();
    }

    private void HandlePilotFreeLook()
    {
        if (Input.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey))
            ApplyFreeLookRotation();
        else
            ReturnToCenter();
    }

    private void ApplyFreeLookRotation()
    {
        float mouseY = Input.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
        float mouseX = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = currentSeat.activeCamera.transform.localEulerAngles;
        float currentX = NormalizeAngle(currentEuler.x);
        float currentY = NormalizeAngle(currentEuler.y);

        currentX += mouseY;
        currentY += mouseX;

        currentX = Mathf.Clamp(currentX, -80f, 20f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        currentSeat.activeCamera.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void ReturnToCenter()
    {
        currentSeat.activeCamera.transform.localRotation = Quaternion.Lerp(
            currentSeat.activeCamera.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * 3
        );
    }

    private float NormalizeAngle(float angle)
    {
        return (angle > 180) ? angle - 360 : angle;
    }

    #endregion

    #region PLAYER INTERACTION  

    private void HandleExitVehicle()
    {
        exit_cooldown += Time.deltaTime;

        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
        {
            currentSeat.playerController.playerCamera.enabled = true;
            ExitVehicle();
        }
    }

    protected override void OnVehicleEntered(int seatIndex, GameObject player)
    {
        base.OnVehicleEntered(seatIndex, player);

        if (!ValidatePlayerReferences())
            return;

        exit_cooldown = 0f;
    }

    private bool ValidatePlayerReferences()
    {
        if (currentSeat == null || currentSeat.playerController == null)
        {
            Debug.LogError("Error: Player references were not filled by the base class.");
            return false;
        }

        return true;
    }
    #endregion

    #region HELPER METHODS
    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            RequestDamage(100);
        }

        minFov = Settings.Instance._video.helicopter_fov;
    }

    void Zoom()
    {
        if (currentSeat.activeCamera.enabled)
        {
            if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key))
            {
                float targetFov = minFov / heliProperties.zoom;
                currentSeat.activeCamera.fieldOfView = Mathf.Lerp(currentSeat.activeCamera.fieldOfView, targetFov, 4 * Time.deltaTime);
            }
            else
            {
                currentSeat.activeCamera.fieldOfView = Mathf.Lerp(
                    currentSeat.activeCamera.fieldOfView,
                    minFov,
                    4 * Time.deltaTime);
            }
        }
        else
        {
            if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key))
            {
                float targetFov = minFov / heliProperties.zoom;
                currentSeat.playerController.playerCamera.fieldOfView = Mathf.Lerp(currentSeat.playerController.playerCamera.fieldOfView, targetFov, 4 * Time.deltaTime);
            }
            else
            {
                currentSeat.playerController.playerCamera.fieldOfView = Mathf.Lerp(
                    currentSeat.playerController.playerCamera.fieldOfView,
                    minFov,
                    4 * Time.deltaTime);
            }
        }
    }

    #endregion

}