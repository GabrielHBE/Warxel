using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class AttackHelicopter : Helicopter
{
    #region Inspector Variables
    [Header("Attatck Helicopter variables")]
    [SerializeField] private Transform gunner_gun_shoot_pos;
    [HideInInspector] public float overheat;
    #endregion

    #region Unity Lifecycle 

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsSpawned)
        {
            Debug.LogError($"{gameObject.name} : AttackHelicopter not spawned in network yet");
            return;
        }

        throttle = 0;
        SetHpProperties(heliProperties.hp, heliProperties.resistance);

    }

    protected override void FixedUpdate()
    {
        print("start_engine: " + start_engine);
        print("is_in_vehicle: " + is_in_vehicle);
        print("!SettingsHUD.Instance.is_menu_settings_active:" + !SettingsHUD.Instance.is_menu_settings_active);
        print("!vehicle_destroyed.Value:" + !vehicle_destroyed.Value);
        print("currentSeat != null: " + currentSeat != null);
        print("currentSeat.seatType == VehicleSeats.SeatType.Pilot: " + (currentSeat.seatType == VehicleSeats.SeatType.Pilot));
        print("IsOwner: " + IsOwner);
        if (start_engine && is_in_vehicle && !SettingsHUD.Instance.is_menu_settings_active && !vehicle_destroyed.Value && currentSeat != null && currentSeat.seatType == VehicleSeats.SeatType.Pilot && IsOwner)
        {
            Move();
            Rotate();
            rb.AddForce(liftDirection * throttle, ForceMode.Acceleration);
        }
        else
        {
            if (vehicle_destroyed.Value)
            {
                DestroyAnimation();
            }
            throttle = 0;
            rb.AddForce(Vector3.down * 50, ForceMode.Acceleration);
        }
    }

    protected override void Update()
    {
        if (currentSeat == null || currentSeat.playerGameObject == null)
        {
            // Se estiver no veículo mas sem referências válidas, sair
            if (is_in_vehicle)
            {
                Debug.LogWarning("Player reference lost, exiting vehicle");
                ExitVehicle();

            }
            return;
        }

        if (currentSeat.playerProperties != null)
        {
            if (currentSeat.playerProperties.is_dead.Value)
            {
                ExitVehicle();

                return;
            }
        }

        PropellerRotation();

        if (is_in_vehicle)
        {
            // Usando playerSeatIndex diretamente como int
            VehicleSeats seat = vehicleSeats[playerSeatIndex];

            seat.playerGameObject.transform.position = currentSeat.playerSeat.position;
            seat.playerGameObject.transform.rotation = currentSeat.playerSeat.rotation;

            SwitchWeapon();
            if (Input.GetKeyDown(KeyCode.P))
            {
                RequestDamage(100);
            }

            minFov = Settings.Instance._video.helicopter_fov;

            if (!SettingsHUD.Instance.is_menu_settings_active) HandleVehicleInput();
        }
    }

    #endregion

    #region Input Handling 

    private void HandleVehicleInput()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot && !vehicle_destroyed.Value) StartStopEngine();
        CameraController();
        FreeLook();
        if (!vehicle_destroyed.Value) Shoot();

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchSeatKey))
        {
            SwitchSeats();
        }

        if (start_engine == true)
        {

            if (!vehicle_destroyed.Value) HandleThrottleControls();
        }

        HandleExitVehicle();
    }

    private void HandleExitVehicle()
    {
        exit_cooldown += Time.deltaTime;

        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
        {
            currentSeat.playerController.playerCamera.enabled = true;
            //helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
            //helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);


            ExitVehicle();
        }
    }
    #endregion

    #region Gunner Gun 
    private void Shoot()
    {
        currentSeat.currentArmory.Shoot();
    }
    #endregion

    #region Camera 

    protected override void CameraController()
    {
        Zoom();
    }

    void Zoom()
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

    private void FreeLook()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot) HandlePilotFreeLook();
    }

    private void HandlePilotFreeLook()
    {
        if (Input.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey))
        {
            ApplyFreeLookRotation();
        }
        else
        {
            ReturnToCenter();
        }
    }

    private void ApplyFreeLookRotation()
    {
        float mouseY_freelook = Input.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
        float mouseX_freelook = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = currentSeat.playerCamera.transform.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentX += mouseY_freelook;
        currentY += mouseX_freelook;

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
    #endregion

    #region Vehicle Entry/Exit 

    // SOBRESCREVE APENAS A LÓGICA LOCAL (Sem tags de rede)
    protected override void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        // 1. Roda a base (Agora vai funcionar 100% sem o FishNet interferir!)
        base.OnVehicleEntered(seatIndex, _player);

        // Travas de segurança para garantir que a base rodou certinho
        if (currentSeat == null || currentSeat.playerController == null)
        {
            Debug.LogError("Erro: Referências do Player não foram preenchidas pela classe base.");
            return;
        }

        InitializeVehicleEntry();

    }

    private void InitializeVehicleEntry()
    {
        exit_cooldown = 0f;
    }

    protected override void ExitVehicle()
    {
        base.ExitVehicle();

        //if (helicopterHudManager.helicopterGunnerHUD.gameObject != null) helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
        //if (helicopterHudManager.helicopterPilotHUD.gameObject != null) helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);
    }

    private void SwitchSeats()
    {
        for (int i = 0; i < vehicleSeats.Length; i++)
        {
            VehicleSeats seat = vehicleSeats[i];
            if (seat.seatType != VehicleSeats.SeatType.Gunner || seat.isOccupied) continue;

            int oldIndex = playerSeatIndex;
            int newIndex = i;

            // Cache das referências locais
            PlayerProperties props = currentSeat.playerProperties;
            PlayerController controller = currentSeat.playerController;
            Rigidbody rb = currentSeat.playerRigidbody;
            GameObject pGo = currentSeat.playerGameObject;
            NetworkConnection conn = pGo.GetComponent<NetworkObject>().Owner;

            // Limpeza local
            currentSeat.ClearReferences();

            // Setup local do novo assento
            currentSeat = seat;
            playerSeatIndex = newIndex;
            currentSeat.EnterSeat(props, controller, seat.playerSeat, rb, pGo);

            // SOLICITA AO SERVIDOR A TROCA
            UpdateServerSwitchSeatsStatus(oldIndex, newIndex, pGo, conn);

            break;
        }
    }



    [ServerRpc(RequireOwnership = false)]
    private void UpdateServerSwitchSeatsStatus(int oldSeatIndex, int newSeatIndex, GameObject playerGameObject, NetworkConnection conn)
    {
        if (playerGameObject == null) return;

        NetworkObject playerNetObj = playerGameObject.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        // Remove autoridade das armas do assento antigo
        if (vehicleSeats[oldSeatIndex].vehicleArmory != null)
        {
            vehicleSeats[oldSeatIndex].SetAuthority(null);
        }

        // Dá autoridade das armas do novo assento
        if (vehicleSeats[newSeatIndex].vehicleArmory != null)
        {
            vehicleSeats[newSeatIndex].SetAuthority(conn);
        }

        // Esvazia a cadeira antiga e preenche a nova na rede
        RpcUpdateSeatStatus(oldSeatIndex, false, null, null);
        RpcUpdateSeatStatus(newSeatIndex, true, playerNetObj, conn);

        // Se mudou para piloto, dá o Ownership do helicóptero
        if (vehicleSeats[newSeatIndex].seatType == VehicleSeats.SeatType.Pilot)
        {
            this.NetworkObject.GiveOwnership(playerNetObj.Owner);
        }
    }
    #endregion

}