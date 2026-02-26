using System.Collections;
using UnityEngine;

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

    #endregion

    #region Unity Lifecycle 

    public override void Spawn()
    {
        //print("C: " + collisionLayers);
        throttle = 0;
        SetHpProperties(heliProperties.hp, heliProperties.resistance);
        base.Spawn();
        helicopterHudManager.helicopterPilotHUD.SetImages(missileManager.main_missile.image_hud, missileManager.secondary_missile.image_hud, countermeasures.image_icon_hud);


    }

    Vector3 shakeOffset;

    protected override void FixedUpdate()
    {
        if (start_engine && is_in_vehicle && !settings.is_menu_settings_active && !vehicle_destroyed && is_pilot_seat_occupied)
        {
            Move();
            Rotate();
            rb.AddForce(liftDirection * throttle, ForceMode.Acceleration);
        }
        else
        {
            if (vehicle_destroyed)
            {
                DestroyAnimation();
            }
            throttle = 0;
            rb.AddForce(Vector3.down * 50, ForceMode.Acceleration);
        }

    }

    protected override void Update()
    {
        //minFov = video.helicopter_fov;

        if (!canShootGunnerGun) HandleCooldown(Time.deltaTime);

        if (playerProperties != null)
        {
            if (playerProperties.is_dead)
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

            SwitchWeapon();
            if (Input.GetKeyDown(KeyCode.P))
            {
                Damage(100);
            }

            minFov = video.helicopter_fov;

            if (!settings.is_menu_settings_active) HandleVehicleInput();

        }

    }


    #endregion


    #region Input Handling 

    private void HandleVehicleInput()
    {
        if (is_pilot && !vehicle_destroyed) Start_Stop_Engine();
        CameraController();
        FreeLook();
        if (!vehicle_destroyed) Shoot();

        if (Input.GetKeyDown(keyBinds.VEHICLE_switchSeatKey))
        {
            SwitchSeats();
        }

        if (!is_pilot)
        {
            RotateGunnerGun();
        }

        if (start_engine)
        {
            PropellerAudioController();
            if (!vehicle_destroyed) HandleThrottleControls();
        }

        HandleExitVehicle();
    }

    private void HandleExitVehicle()
    {
        exit_cooldown += Time.deltaTime;

        if (Input.GetKeyDown(keyBinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
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
        if (Input.GetKeyDown(keyBinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            if (start_engine)
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
        if (Input.GetKeyDown(keyBinds.VEHICLE_weapon1))
        {
            is_using_primary = true;
        }

        if (Input.GetKeyDown(keyBinds.VEHICLE_weapon2))
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
        mouseX = Input.GetAxis("Mouse X") * controls.helicopter_sensibility;
        mouseY = Input.GetAxis("Mouse Y") * controls.helicopter_sensibility;

        Vector3 currentEuler = gunner_gun.transform.localEulerAngles;

        float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
        float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

        currentX -= mouseY;
        currentY += mouseX;

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
                missileManager.main_missile.Shoot(keyBinds.HELICOPTER_shoot_key);
                //missileManager.main_missile.SetPlayerVehicle(this);

            }
            else
            {
                missileManager.secondary_missile.SetActive(true);
                missileManager.main_missile.SetActive(false);
                missileManager.secondary_missile.SetActive(true);
                missileManager.secondary_missile.Shoot(keyBinds.HELICOPTER_shoot_key);
                //missileManager.secondary_missile.SetPlayerVehicle(this);
            }
        }

    }

    private void ShootGunnerGun()
    {
        float dt = Time.deltaTime;
        bool isShooting = Input.GetKey(keyBinds.HELICOPTER_shoot_key);
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



            heliProperties.shootPos.transform.localRotation = new Quaternion(UnityEngine.Random.Range(-current_spread, current_spread) / 1000, UnityEngine.Random.Range(-current_spread, current_spread) / 1000, UnityEngine.Random.Range(-current_spread, current_spread) / 1000, heliProperties.shootPos.transform.localRotation.w);
        }
        else
        {

            current_spread -= Time.deltaTime * 2;
            heliProperties.shootPos.transform.localRotation = Quaternion.Lerp(heliProperties.shootPos.transform.localRotation, original_shoot_pos_rotation, Time.deltaTime * 2);
        }

        next_time_to_fire -= dt;


    }

    private void HandleShooting(float dt)
    {
        if (next_time_to_fire <= 0f)
        {
            FireGunnerGun();
            next_time_to_fire = heliProperties.interval;
        }

        overheat += dt;

        if (overheat >= heliProperties.overheat_time)
            overheated = true;
    }

    private void FireGunnerGun()
    {
        heliProperties.shoot_sound.PlayOneShot(heliProperties.shoot_sound.clip);

        Transform bulletObj = Instantiate(
            heliProperties.bullefPref,
            heliProperties.shootPos.transform.position,
            heliProperties.shootPos.transform.rotation
        );

        bulletObj.GetComponent<Bullet>().CreateBullet(
            heliProperties.shootPos.transform.forward,
            heliProperties.muzzle_velocity,
            heliProperties.bullet_drop,
            heliProperties.damage,
            heliProperties.damage_dropoff,
            heliProperties.damage_dropoff_timer,
            heliProperties.destruction_force,
            heliProperties.minimum_damage,
            2,
            2,
            0,
            true,
            heliProperties.damage,
            heliProperties.bullet_hit_effect,
            vehicle: this
        );

        current_spread += heliProperties.spread;


        Destroy(bulletObj.gameObject, 10f);
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
        if (Input.GetKeyDown(keyBinds.HELICOPTER_switch_camera_key))
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
        if (Input.GetKeyDown(keyBinds.HELICOPTER_switch_camera_key))
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
            if (Input.GetKey(keyBinds.HELICOPTER_zoom_key))
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
            if (Input.GetKey(keyBinds.HELICOPTER_zoom_key))
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
        if (Input.GetKey(keyBinds.VEHICLE_freeLookKey))
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
        float mouseY_freelook = Input.GetAxis("Mouse Y") * -controls.helicopter_sensibility;
        float mouseX_freelook = Input.GetAxis("Mouse X") * controls.helicopter_sensibility;

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
        mouseY = Input.GetAxis("Mouse Y") * controls.helicopter_sensibility;
        mouseX = Input.GetAxis("Mouse X") * controls.helicopter_sensibility;

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

    public override void EnterVehicle(GameObject _player)
    {
        if (is_pilot_seat_occupied && is_gunner_seat_occupied) return;

        base.EnterVehicle(_player);
        vehicle_camera = player.GetComponent<PlayerController>().playerCamera;
        player_camera = vehicle_camera.transform;

        missileManager.SetCamera(vehicle_camera);

        InitializeVehicleEntry();

        if ((!is_pilot_seat_occupied && !is_gunner_seat_occupied) ||
            (!is_pilot_seat_occupied && is_gunner_seat_occupied))
        {
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(true);
            EnterPilotSeat();
        }
        else if (is_pilot_seat_occupied && !is_gunner_seat_occupied)
        {
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(true);
            EnterGunnerSeat();
        }


    }

    private void InitializeVehicleEntry()
    {
        current_camera = 1;
        exit_cooldown = 0f;
    }

    private void EnterPilotSeat()
    {
        is_pilot = true;
        is_pilot_seat_occupied = true;
        SnapPlayerToSeat(pilot_position);
        SetPlayerAndHUDActive(false, true);
    }

    private void EnterGunnerSeat()
    {
        is_pilot = false;
        is_gunner_seat_occupied = true;
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
        if (!is_gunner_seat_occupied)
        {
            SnapPlayerToSeat(gunner_position);
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(true);
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(false);
            is_gunner_seat_occupied = true;
            is_pilot_seat_occupied = false;
            is_pilot = false;
            current_camera = 1;
        }
    }

    private void SwitchToPilotSeat()
    {
        if (!is_pilot_seat_occupied)
        {
            SnapPlayerToSeat(pilot_position);
            helicopterHudManager.helicopterPilotHUD.gameObject.SetActive(true);
            helicopterHudManager.helicopterGunnerHUD.gameObject.SetActive(false);
            gunner_gun_camera.enabled = false;
            vehicle_camera.enabled = true;
            is_gunner_seat_occupied = false;
            is_pilot_seat_occupied = true;
            is_pilot = true;
            current_camera = 1;
        }
    }


    private void ResetVehicleState()
    {
        if (is_pilot)
        {
            is_pilot_seat_occupied = false;
        }
        else
        {
            is_gunner_seat_occupied = false;
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
        float targetSpeed = start_engine ? heliProperties.max_lift_force : 0f;

        float smoothTime = start_engine ? propellerAccelerationTime : propellerDecelerationTime;
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

    protected override void UpdateHUD()
    {
        helicopterHudManager.helicopterPilotHUD.UpdateDamage();
        helicopterHudManager.helicopterPilotHUD.UpdateRotationX(transform.eulerAngles.x);
        helicopterHudManager.helicopterPilotHUD.UpdateRotationY(transform.eulerAngles.y);
        helicopterHudManager.helicopterPilotHUD.UpdateAltitude(transform.position.y / 3);
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
