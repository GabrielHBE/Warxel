using FishNet.Connection;
using FishNet.Object;
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

            UpdatePlayerSeatTransform();
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

        Vector3 currentEuler = currentSeat.playerCamera.transform.localEulerAngles;
        float currentX = NormalizeAngle(currentEuler.x);
        float currentY = NormalizeAngle(currentEuler.y);

        currentX += mouseY;
        currentY += mouseX;

        currentX = Mathf.Clamp(currentX, -80f, 20f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        currentSeat.playerCamera.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void ReturnToCenter()
    {
        currentSeat.playerCamera.transform.localRotation = Quaternion.Lerp(
            currentSeat.playerCamera.transform.localRotation,
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



    private void SwitchSeats()
    {
        int searchIndex;

        if (playerSeatIndex == vehicleSeats.Length - 1)
            searchIndex = 0;
        else
            searchIndex = playerSeatIndex + 1;

        for (int i = searchIndex; i < vehicleSeats.Length; i++)
        {
            VehicleSeats seat = vehicleSeats[i];

            if (!seat.isOccupied)
            {
                int oldIndex = playerSeatIndex;
                int newIndex = i;

                PlayerProperties props = currentSeat.playerProperties;
                PlayerController controller = currentSeat.playerController;
                Rigidbody rb = currentSeat.playerRigidbody;
                GameObject pGo = currentSeat.playerGameObject;

                currentSeat.ClearReferences();
                currentSeat = seat;
                playerSeatIndex = newIndex;
                currentSeat.EnterSeat(props, controller, seat.playerSeat, rb, pGo);

                UpdateServerSwitchSeatsStatus(oldIndex, newIndex, pGo);

                break;
            }
        }
    }

    #endregion

    #region HELPER METHODS

    private void UpdatePlayerSeatTransform()
    {
        currentSeat.playerGameObject.transform.position = currentSeat.playerSeat.position;
        currentSeat.playerGameObject.transform.rotation = currentSeat.playerSeat.rotation;
    }

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
        if (currentSeat.playerCamera.enabled)
        {
            if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key))
            {
                float targetFov = minFov / heliProperties.zoom;
                currentSeat.playerCamera.fieldOfView = Mathf.Lerp(currentSeat.playerCamera.fieldOfView, targetFov, 4 * Time.deltaTime);
            }
            else
            {
                currentSeat.playerCamera.fieldOfView = Mathf.Lerp(
                    currentSeat.playerCamera.fieldOfView,
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

    #region NETWORKED ACTIONS

    [ServerRpc(RequireOwnership = false)]
    private void UpdateServerSwitchSeatsStatus(int oldSeatIndex, int newSeatIndex, GameObject playerGameObject)
    {
        NetworkConnection conn = playerGameObject.GetComponent<NetworkObject>().Owner;

        // Remove autoridade do antigo
        if (vehicleSeats[oldSeatIndex].vehicleArmory != null)
            vehicleSeats[oldSeatIndex].SetAuthority(null);

        // Dá autoridade ao novo
        if (vehicleSeats[newSeatIndex].vehicleArmory != null)
            vehicleSeats[newSeatIndex].SetAuthority(conn);

        RpcUpdateSeatStatus(oldSeatIndex, false, null);
        NetworkObject netObj = playerGameObject.GetComponent<NetworkObject>();
        RpcUpdateSeatStatus(newSeatIndex, true, netObj);

        if (vehicleSeats[newSeatIndex].seatType == VehicleSeats.SeatType.Pilot)
            this.NetworkObject.GiveOwnership(conn);
    }
    #endregion
}