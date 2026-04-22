using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackHelicopter : Helicopter
{
    #region Inspector Variables

    [Header("Attatck Helicopter variables")]
    [SerializeField] private Transform gunner_position;
    [SerializeField] private HelicopterMissileManager missileManager;
    [SerializeField] private GameObject gunner_gun;
    [HideInInspector] public float overheat;
    [SerializeField] private Camera gunner_gun_camera;

    #endregion


    #region Private Variables

    public readonly SyncVar<bool> is_gunner_seat_occupied = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));

    // Gunner gun
    private float next_time_to_fire = 0;
    private bool overheated;
    private bool is_using_primary = true;
    private float current_spread;
    private Quaternion original_shoot_pos_rotation;

    // Inputs
    private float current_camera;
    private float exit_cooldown;

    // Movement

    // Propeller
    private float currentPropellerSpeed = 0f;
    private float propellerAccelerationTime = 10f;
    private float propellerDecelerationTime = 1f;
    private bool canShootGunnerGun;
    // Destruction


    // Other
    private Coroutine activeShakeCoroutine;
    private Transform current_seat_pos;

    #endregion

    #region Unity Lifecycle 

    public override void Initialize()
    {
        base.Initialize();

        if (!IsSpawned)
        {
            Debug.LogError($"{gameObject.name} : AttackHelicopter not spawned in network yet");
            return;
        }

        //print("C: " + collisionLayers);
        throttle = 0;
        SetHpProperties(heliProperties.hp, heliProperties.resistance);

        helicopterHudManager.helicopterPilotHUD.SetImages(missileManager.main_missile.image_hud, missileManager.secondary_missile.image_hud, countermeasures.image_icon_hud);

    }

    Vector3 shakeOffset;

    protected override void FixedUpdate()
    {

        if (start_engine && is_in_vehicle && !SettingsHUD.Instance.is_menu_settings_active && !vehicle_destroyed.Value && is_pilot_seat_occupied.Value == true)
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
        //minFov = Settings.Instance._video.helicopter_fov;

        if (player == null)
        {
            // Se estiver no veículo mas sem referências válidas, sair
            if (is_in_vehicle)
            {
                Debug.LogWarning("Player reference lost, exiting vehicle");
                ExitVehicle();
                ResetVehicleState();
            }
            return;
        }


        if (!canShootGunnerGun) HandleCooldown(Time.deltaTime);

        if (playerProperties != null)
        {
            if (playerProperties.is_dead.Value)
            {
                ExitVehicle();
                ResetVehicleState();
                return;
            }
        }

        PropellerRotation();


        Vector3 finalRotation = gunner_gun.transform.localEulerAngles + shakeOffset;
        gunner_gun.transform.localEulerAngles = finalRotation;

        if (is_in_vehicle)
        {
            UpdateHUD();

            player.transform.position = current_seat_pos.position;
            player.transform.rotation = current_seat_pos.rotation;

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
        if (is_pilot && !vehicle_destroyed.Value) Start_Stop_Engine();
        CameraController();
        FreeLook();
        if (!vehicle_destroyed.Value) Shoot();

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchSeatKey))
        {
            SwitchSeats();
        }

        if (!is_pilot)
        {
            RotateGunnerGun();
        }

        if (start_engine == true)
        {
            PropellerAudioController();
            if (!vehicle_destroyed.Value) HandleThrottleControls();
        }

        HandleExitVehicle();
    }

    private void HandleExitVehicle()
    {
        exit_cooldown += Time.deltaTime;

        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
        {
            gunner_gun_camera.enabled = false;
            vehicle_camera.enabled = true;
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);

            ExitVehicle();
            //ResetVehicleState();
        }
    }

    protected override void Start_Stop_Engine()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            if (start_engine == true)
            {
                inside_propeller_sound.Play();
            }
            else
            {
                inside_propeller_sound.Stop();
            }
        }
    }

    private void SwitchWeapon()
    {
        if (Mouse.current.scroll.ReadValue().y != 0)
        {
            is_using_primary = !is_using_primary;
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon1))
        {
            is_using_primary = true;
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon2))
        {
            is_using_primary = false;
        }

        if (is_using_primary)
        {
            helicopterHudManager.helicopterPilotHUD.SetPrimaryActive();
        }
        else
        {
            helicopterHudManager.helicopterPilotHUD.SetSecondaryActive();
        }
    }

    #endregion

    #region Gunner Gun 

    private void RotateGunnerGun()
    {
        if (!gunner_gun_camera.enabled) return;

        mouseX = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;
        mouseY = Input.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = gunner_gun.transform.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        // A mesma lógica do ApplyGunnerLookRotation()
        currentY += mouseX;
        currentX -= mouseY;

        currentX = Mathf.Clamp(currentX, -5f, 80f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        gunner_gun.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void Shoot()
    {
        if (is_pilot)
        {
            ShootPilotRockets();
        }
        else
        {
            ShootGunnerGun();
        }
    }


    private void ShootPilotRockets()
    {
        if (missileManager != null)
        {
            if (is_using_primary)
            {
                missileManager.main_missile.SetActive(true);
                missileManager.secondary_missile.SetActive(false);
                missileManager.secondary_missile.SetActive(false);
                missileManager.main_missile.Shoot(Settings.Instance._keybinds.HELICOPTER_shoot_key);
                //missileManager.main_missile.SetPlayerVehicle(this);

            }
            else
            {
                missileManager.secondary_missile.SetActive(true);
                missileManager.main_missile.SetActive(false);
                missileManager.secondary_missile.SetActive(true);
                missileManager.secondary_missile.Shoot(Settings.Instance._keybinds.HELICOPTER_shoot_key);
                //missileManager.secondary_missile.SetPlayerVehicle(this);
            }
        }

    }

    private void ShootGunnerGun()
    {
        float dt = Time.deltaTime;
        bool isShooting = Input.GetKey(Settings.Instance._keybinds.HELICOPTER_shoot_key);
        canShootGunnerGun = !overheated && isShooting;

        current_spread = Mathf.Clamp(current_spread, 0, heliProperties.max_spread);

        if (canShootGunnerGun)
        {
            if (activeShakeCoroutine != null)
            {
                StopCoroutine(activeShakeCoroutine);
            }

            activeShakeCoroutine = StartCoroutine(ShakeRoutine());

            HandleShooting(dt);



            heliProperties.shootPos.transform.localRotation = new Quaternion(Random.Range(-current_spread, current_spread) / 1000, Random.Range(-current_spread, current_spread) / 1000, Random.Range(-current_spread, current_spread) / 1000, heliProperties.shootPos.transform.localRotation.w);
        }
        else
        {

            current_spread = 0;
            heliProperties.shootPos.transform.localRotation = original_shoot_pos_rotation;
        }

        next_time_to_fire -= dt;


    }

    private void HandleShooting(float dt)
    {
        if (next_time_to_fire <= 0f)
        {
            heliProperties.shoot_sound.PlayOneShot(heliProperties.shoot_sound.clip);
            FireGunnerGun();
            next_time_to_fire = heliProperties.interval;
        }

        overheat += dt;

        if (overheat >= heliProperties.overheat_time)
            overheated = true;
    }

    [ServerRpc]
    private void FireGunnerGun()
    {
        // Verificações adicionais de segurança
        if (!IsSpawned)
        {
            Debug.LogError($"NetworkObject not spawned, cannot execute ServerRpc. Object: {gameObject.name}");
            return;
        }

        if (!IsServerInitialized)
        {
            Debug.LogError("Server not initialized, cannot execute ServerRpc");
            return;
        }

        RpcShootMachineGunEffects();

        Bullet.BulletData data = new Bullet.BulletData
        {
            position = heliProperties.shootPos.transform.position,
            rotation = heliProperties.shootPos.transform.rotation,
            direction = heliProperties.shootPos.transform.forward,
            speed = heliProperties.muzzle_velocity,
            dropMultiplier = heliProperties.bullet_drop,
            infantaryDamage = heliProperties.infantary_damage,
            damageDropoff = heliProperties.damage_dropoff,
            damageDropoffTimer = heliProperties.damage_dropoff_timer,
            destructionForce = heliProperties.destruction_force,
            minimumDamage = heliProperties.minimum_damage,
            hsMultiplier = 2,
            size = 1,
            canDamageVehicles = true,
            vehicleDamage = heliProperties.vehicle_damage
        };

        Transform bulletObj = Instantiate(
            heliProperties.bullefPref,
            data.position,
            data.rotation
        );


        Spawn(bulletObj.gameObject, Owner);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.CreateBullet(data, transform);
        }

        current_spread += heliProperties.spread;

        Destroy(bulletObj.gameObject, 10f);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcShootMachineGunEffects()
    {
        HandleSound(heliProperties.shoot_sound);
    }

    private void HandleCooldown(float dt)
    {
        float coolSpeed = overheated ? (dt / 2f) : dt;
        overheat = Mathf.MoveTowards(overheat, 0f, coolSpeed);
        if (overheat == 0)
        {
            overheated = false;
        }
    }

    #endregion

    #region Camera 

    protected override void CameraController()
    {
        if (vehicle_camera == null) return;

        Zoom();

        if (is_pilot)
        {
            HandlePilotCamera();
        }
        else
        {
            HandleGunnerCamera();
        }
    }

    private void HandlePilotCamera()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.HELICOPTER_switch_camera_key))
        {
            current_camera += 1;
            if (current_camera > 3)
            {
                current_camera = 1;
            }
        }
    }


    private void HandleGunnerCamera()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.HELICOPTER_switch_camera_key))
        {
            current_camera += 1;
            if (current_camera > 2)
            {
                current_camera = 1;
            }

            if (current_camera == 1)
            {
                vehicle_camera.enabled = true;
                gunner_gun_camera.enabled = false;
            }
            else
            {
                vehicle_camera.enabled = false;
                gunner_gun_camera.enabled = true;
            }
        }

    }

    void Zoom()
    {
        if (vehicle_camera == null) return;

        if (gunner_gun_camera.enabled)
        {
            if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key))
            {
                float targetFov = minFov / heliProperties.zoom;
                gunner_gun_camera.fieldOfView = Mathf.Lerp(gunner_gun_camera.fieldOfView, targetFov, 4 * Time.deltaTime);
            }
            else
            {
                gunner_gun_camera.fieldOfView = Mathf.Lerp(
                    gunner_gun_camera.fieldOfView,
                    minFov,
                    4 * Time.deltaTime);
            }
        }
        else
        {
            if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key))
            {
                float targetFov = minFov / heliProperties.zoom;
                vehicle_camera.fieldOfView = Mathf.Lerp(vehicle_camera.fieldOfView, targetFov, 4 * Time.deltaTime);
            }
            else
            {
                vehicle_camera.fieldOfView = Mathf.Lerp(
                    vehicle_camera.fieldOfView,
                    minFov,
                    4 * Time.deltaTime);
            }
        }


    }

    private void FreeLook()
    {
        if (is_pilot)
        {

            HandlePilotFreeLook();
        }
        else
        {
            HandleGunnerFreeLook();
        }
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

    private void HandleGunnerFreeLook()
    {
        ApplyGunnerLookRotation();
    }

    private void ApplyFreeLookRotation()
    {
        float mouseY_freelook = Input.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
        float mouseX_freelook = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = player_camera.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentX += mouseY_freelook;
        currentY += mouseX_freelook;

        currentX = Mathf.Clamp(currentX, -80f, 20f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        player_camera.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void ApplyGunnerLookRotation()
    {
        if (gunner_gun_camera.enabled) return;

        mouseY = Input.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility;
        mouseX = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

        Vector3 currentEuler = player_camera.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentY += mouseX;
        currentX -= mouseY;

        currentX = Mathf.Clamp(currentX, -80f, 20f);
        currentY = Mathf.Clamp(currentY, -90f, 90f);

        player_camera.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void ReturnToCenter()
    {
        player_camera.localRotation = Quaternion.Lerp(
            player_camera.localRotation,
            Quaternion.identity,
            Time.deltaTime * 3
        );
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        float intensity = 0.1f;
        float duration = 0.2f;

        shakeOffset = Vector3.zero;
        while (elapsed < duration)
        {
            float time = Time.time * 20f;
            // Calcula o offset do shake
            shakeOffset = new Vector3(
                    // Eixo X: mistura de Perlin noise com seno para movimento orgânico
                    (Mathf.PerlinNoise(time * 1.2f, 0) * 2f - 1f) * intensity * 2f,

                    // Eixo Y: usa combinação de noises para variação
                    ((Mathf.PerlinNoise(0, time * 1.5f) * 2f - 1f) * 0.4f +
                     (Mathf.Sin(time * 3f) * 0.6f)) * intensity * 0.8f,

                    // Eixo Z: noise com offset diferente para variar
                    (Mathf.PerlinNoise(time * 0.8f, time * 0.8f) * 2f - 1f) * intensity * 0.7f
                );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retorna suavemente à posição sem shake
        float returnTime = Mathf.Min(0.1f, duration * 0.5f);
        elapsed = 0f;
        Vector3 startingShake = shakeOffset;

        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            // Interpola suavemente de volta a zero
            shakeOffset = Vector3.Lerp(startingShake, Vector3.zero, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        activeShakeCoroutine = null;
    }

    #endregion

    #region Vehicle Entry/Exit 
    [TargetRpc]
    public override void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        if (is_pilot_seat_occupied.Value == true && is_gunner_seat_occupied.Value == true) return;

        CallBaseEnterVehicle(conn, _player);
        vehicle_camera = player.GetComponent<PlayerController>().playerCamera;
        player_camera = vehicle_camera.transform;

        missileManager.SetCamera(vehicle_camera);

        InitializeVehicleEntry();

        if (is_pilot_seat_occupied.Value == false && is_gunner_seat_occupied.Value == true || (is_pilot_seat_occupied.Value == false && is_gunner_seat_occupied.Value == false))
        {
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(true);
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
            EnterPilotSeat();
        }
        else if (is_pilot_seat_occupied.Value == true && is_gunner_seat_occupied.Value == false)
        {
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(true);
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);
            EnterGunnerSeat();
        }
    }
    private void CallBaseEnterVehicle(NetworkConnection conn, GameObject _player)
    {
        base.EnterVehicle(conn, _player);
    }

    private void InitializeVehicleEntry()
    {
        current_camera = 1;
        exit_cooldown = 0f;
    }

    private void EnterPilotSeat()
    {
        is_pilot = true;
        UpdateServerPilotSeatsStatus(true);
        //is_pilot_seat_occupied.Value = true;
        SnapPlayerToSeat(pilot_position);
        SetPlayerAndHUDActive(false, true);
    }

    private void EnterGunnerSeat()
    {
        is_pilot = false;
        UpdateServerGunnerSeatsStatus(true);
        //is_gunner_seat_occupied.Value = true;
        SnapPlayerToSeat(gunner_position);
        SetPlayerAndHUDActive(false, true);
    }

    protected override void ExitVehicle()
    {

        base.ExitVehicle(); // Chama a implementação da classe base

        // Reseta os estados específicos do helicóptero
        ResetVehicleState();

        // Garante que todas as HUDs estão desativadas
        if (helicopterHudManager.helicopterGunnerHUD.gameObject != null) helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
        if (helicopterHudManager.helicopterPilotHUD.gameObject != null) helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);

        // Reseta as câmeras
        if (gunner_gun_camera != null) gunner_gun_camera.enabled = false;
        if (vehicle_camera != null) vehicle_camera.enabled = true;
    }


    private void SwitchSeats()
    {
        if (is_pilot)
        {
            SwitchToGunnerSeat();
        }
        else
        {
            SwitchToPilotSeat();
        }
    }

    private void SwitchToGunnerSeat()
    {
        if (is_gunner_seat_occupied.Value == false)
        {
            SnapPlayerToSeat(gunner_position);
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(true);
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);
            UpdateServerSwitchSeatsStatus(is_gunner_seat_occupied: true, is_pilot_seat_occupied: false);
            //is_gunner_seat_occupied.Value = true;
            //is_pilot_seat_occupied.Value = false;
            is_pilot = false;
            current_camera = 1;
        }
    }

    private void SwitchToPilotSeat()
    {
        if (is_pilot_seat_occupied.Value == false)
        {
            SnapPlayerToSeat(pilot_position);
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(true);
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
            gunner_gun_camera.enabled = false;
            vehicle_camera.enabled = true;
            UpdateServerSwitchSeatsStatus(is_gunner_seat_occupied: false, is_pilot_seat_occupied: true);
            //is_gunner_seat_occupied.Value = false;
            //is_pilot_seat_occupied.Value = true;
            is_pilot = true;
            current_camera = 1;
        }
    }

    [ServerRpc]
    private void UpdateServerPilotSeatsStatus(bool status)
    {
        this.is_pilot_seat_occupied.Value = status;
    }

    [ServerRpc]
    private void UpdateServerGunnerSeatsStatus(bool status)
    {
        this.is_gunner_seat_occupied.Value = status;
    }

    [ServerRpc]
    private void UpdateServerSwitchSeatsStatus(bool is_gunner_seat_occupied, bool is_pilot_seat_occupied)
    {
        this.is_gunner_seat_occupied.Value = is_gunner_seat_occupied;
        this.is_pilot_seat_occupied.Value = is_pilot_seat_occupied;

    }


    private void ResetVehicleState()
    {
        if (is_pilot)
        {
            UpdateServerPilotSeatsStatus(false);
        }
        else
        {
            UpdateServerGunnerSeatsStatus(false);
        }

        is_in_vehicle = false;
    }

    private void SetPlayerAndHUDActive(bool playerActive, bool hudActive)
    {
        //if (player != null) player.SetActive(playerActive);
        //if (hud != null) hud.SetActive(hudActive);
    }

    #endregion

    #region Audio & Visual Effects 

    private void PropellerAudioController()
    {

    }


    private void PropellerRotation()
    {
        float deltaTime = Time.deltaTime;
        float targetSpeed = start_engine == true ? heliProperties.max_lift_force : 0f;

        float smoothTime = start_engine == true ? propellerAccelerationTime : propellerDecelerationTime;
        float t = Mathf.Clamp01(deltaTime / smoothTime);

        currentPropellerSpeed = Mathf.Lerp(currentPropellerSpeed, targetSpeed, t);

        float rotationAmount = currentPropellerSpeed * deltaTime * 20;

        if (main_propeller != null)
            main_propeller.transform.Rotate(0, rotationAmount * 4, 0, Space.Self);
        if (back_propeller != null)
            back_propeller.transform.Rotate(0, rotationAmount * 4, 0);
    }

    #endregion


    #region Utility 

    protected void SnapPlayerToSeat(Transform seat)
    {
        if (player == null) return;

        current_seat_pos = seat;

        // Fazer o parenting
        //player.transform.SetParent(seat, false);
        //player.transform.localPosition = Vector3.zero;
        //player.transform.localRotation = Quaternion.identity;
    }

    protected override void UpdateHUD()
    {
        helicopterHudManager.helicopterPilotHUD.UpdateDamage();
        helicopterHudManager.helicopterPilotHUD.UpdateRotationX(transform.eulerAngles.x);
        helicopterHudManager.helicopterPilotHUD.UpdateRotationY(transform.eulerAngles.y);
        helicopterHudManager.helicopterPilotHUD.UpdateAltitude(transform.position.y);
        helicopterHudManager.helicopterPilotHUD.UpdateSpeed(rb.linearVelocity.magnitude);
        helicopterHudManager.helicopterPilotHUD.UpdateThrottle(throttle);

        if (countermeasures != null)
        {
            if (countermeasures.is_active)
            {
                helicopterHudManager.UpdateCountermeasuresStatus("Active");
            }
            else if (!countermeasures.is_active && countermeasures.is_reloading)
            {
                helicopterHudManager.UpdateCountermeasuresStatus("Reloading... [" + countermeasures.reload_countermeasures_duration.ToString("F0") + "]");
            }
            else if (!countermeasures.is_active && !countermeasures.is_reloading)
            {
                helicopterHudManager.UpdateCountermeasuresStatus("Ready");
            }
        }

    }

    #endregion
}
