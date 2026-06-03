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
        if (start_engine && is_in_vehicle && !vehicle_destroyed.Value && currentSeat != null && currentSeat.seatType == VehicleSeats.SeatType.Pilot && IsOwner)
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

            SyncPlayerPosition();

            SwitchWeapon();
            if (InputManager.GetKeyDown(KeyCode.P))
            {
                RequestDamage(100);
            }

            HandleVehicleInputManager();
        }
    }

    #endregion

    #region Input Handling 

    private void HandleVehicleInputManager()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot && !vehicle_destroyed.Value) StartStopEngine();
        CameraController();
        FreeLook();
        if (!vehicle_destroyed.Value) Shoot();

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchSeatKey)) SwitchSeats();
        
        if (start_engine == true)
        {

            if (!vehicle_destroyed.Value) HandleThrottleControls();
        }

        HandleExitVehicle();
    }

    private void HandleExitVehicle()
    {
        exit_cooldown += Time.deltaTime;

        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
        {
            currentSeat.playerController.playerCamera.enabled = true;

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

    }

    private void FreeLook()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot) HandlePilotFreeLook();
    }

    private void HandlePilotFreeLook()
    {
        if (InputManager.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey))
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
        float mouseY_freelook = InputManager.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
        float mouseX_freelook = InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = currentSeat.activeCamera.transform.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentX += mouseY_freelook;
        currentY += mouseX_freelook;

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
    #endregion

}