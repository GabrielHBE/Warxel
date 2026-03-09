using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tank : Vehicle
{
    [Header("Properties")]
    [SerializeField] private TankProperties tankProperties;
    [SerializeField] private TankHudManager tankHudManager;

    [Header("Instances")]
    [SerializeField] private GameObject shoot_main_cannon_explosion;
    [SerializeField] private GameObject turret;
    [SerializeField] protected Light[] lights;
    public WheelCollider[] left_weels;
    public WheelCollider[] right_weels;
    public Transform[] left_weels_transform;
    public Transform[] right_weels_transform;

    [Header("Audio")]
    [SerializeField] private AudioSource engine_sound;
    [SerializeField] private AudioSource secondary_cannon_sound;
    [SerializeField] private AudioSource main_cannon_sound;


    [Header("Guns")]
    [SerializeField] private TankMainShell tankMainShell;
    [SerializeField] private Transform tankPilotGun;
    [SerializeField] private Transform pilotGunShootPos;
    [SerializeField] private Transform tankGunnerGun;
    [SerializeField] private Transform tankCannon;
    [SerializeField] private Transform retactableTankCannon;
    [SerializeField] private Transform cannonShootPos;


    [Header("Public fields")]
    public float maxRotationUp = 45f;   // Rotação máxima positiva (para um lado)
    public float maxRotationDown = 45f; // Rotação máxima negativa (para outro lado)

    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public int moveForward;
    [HideInInspector] public int moveSideways;
    [HideInInspector] public float speed;
    [HideInInspector] public bool usingMainCannon = true;
    [HideInInspector] public float pilot_gun_overheat_amount;
    [HideInInspector] public float gunner_gun_overheat_amount;

    #region Private Fields
    private Vector3 gunnerGunOriginalLocalPosition;
    private Quaternion gunnerGunOriginalLocalRotation;
    private bool isGunnerGunRecoiling = false;
    private Coroutine gunnerGunRecoilCoroutine;
    private float _exitCooldown;
    private float cannon_shoot_delay = 0;
    private bool is_pilot_gun_overheated = false;
    private float pilot_gun_next_time_to_fire;
    private float boost_max_throttle;
    private float boost_max_speed;
    private float current_max_speed;
    private float current_acceletarion;
    private float boost_acceletarion;
    private bool is_boosting;
    private bool canShootMainGun;
    private float cannon_rotation_amount;

    #endregion

    #region Unity Lifecycle

    public override void Spawn()
    {
        base.Spawn();
        minFov = Settings.Instance._video.tank_fov;
        SetHpProperties(tankProperties.hp, tankProperties.resistance);
        acceleration = tankProperties.acceleration;

        cannon_shoot_delay = tankMainShell.reload_time;

        gunnerGunOriginalLocalPosition = tankGunnerGun.transform.localPosition;

        current_max_speed = tankProperties.max_speed;
        current_acceletarion = tankProperties.acceleration;

        boost_max_throttle = tankProperties.max_throttle * tankProperties.boost_force;
        boost_max_speed = tankProperties.max_speed * tankProperties.boost_force;
        boost_acceletarion = tankProperties.acceleration * tankProperties.boost_force;

        if (tankMainShell.image_hud != null && tankProperties.pilot_gun_hud_image != null && countermeasures.image_icon_hud != null) tankHudManager.SetImages(tankMainShell.image_hud, tankProperties.pilot_gun_hud_image, countermeasures.image_icon_hud);
    }

    bool did_play_destroy_animation = false;
    protected override void Update()
    {
        if (!canShootMainGun) CoolDownGun();

        if (is_in_vehicle)
        {
            _exitCooldown += Time.deltaTime;

            if (!SettingsHUD.Instance.is_menu_settings_active)
            {
                SwitchGun();
                HandleShooting();
                Zoom();
                UpdateHUD();

                if (!vehicle_destroyed)
                {
                    Start_Stop_Engine();
                    if (start_engine)
                    {
                        Boost();
                        RotateCannon();
                        RotateTurret();
                    }
                }
                if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && _exitCooldown > 0.1f) ExitVehicle();

            }

        }

        if (vehicle_destroyed && !did_play_destroy_animation)
        {
            DestroyAnimation();
            did_play_destroy_animation = true;
        }

    }
    protected override void FixedUpdate()
    {
        speed = rb.linearVelocity.magnitude;
        if (is_in_vehicle && start_engine && !SettingsHUD.Instance.is_menu_settings_active && !vehicle_destroyed)
        {
            Move();
        }
        WheelsController();
        rb.AddForce(Vector3.down * 50);
    }




    protected override void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && rb.linearVelocity.magnitude != 0)
        {
            PlayerController hit_playerController = collision.gameObject.GetComponent<PlayerController>();
            if (hit_playerController != null) hit_playerController.Damage(rb.linearVelocity.magnitude);
        }

        if (!IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            return;
        }

        //Debug.Log(rb.linearVelocity.magnitude);
        if (rb.linearVelocity.magnitude < 10) return;

        ContactPoint contact = collision.contacts[0];
        voxCollider.destructionRadius = Math.Clamp(rb.linearVelocity.magnitude, 0, 30);


        voxCollider.SphereExplosion(contact.point, 1);
        ApplyFallUpperVoxels(collision, contact, voxCollider.destructionRadius);

        
    }

    #endregion

    #region Engine & Movement

    protected override void Move()
    {
        ThrottleInput();
        RotateInput();

        // Aplicar força de movimento
        if (moveForward != 0)
        {
            // Calcular força baseada na direção e aceleração
            float moveForce = moveForward * current_acceletarion * rb.mass;

            // Aplicar força na direção local do tanque
            Vector3 forwardForce = transform.forward * moveForce;
            rb.AddForce(forwardForce, ForceMode.Force);

            // Limitador de velocidade
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (flatVel.magnitude > current_max_speed)
            {
                Vector3 limitedVel = flatVel.normalized * current_max_speed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

        // Aplicar força de rotação
        if (moveSideways != 0)
        {
            // Limitador de velocidade angular
            if (Mathf.Abs(rb.angularVelocity.y) < tankProperties.max_rotation_speed)
            {
                float turnForce = moveSideways * tankProperties.rotation_value * rb.mass;
                rb.AddTorque(transform.up * turnForce, ForceMode.Force);
            }
        }

    }

    private float currentSpeedLerp = 0f;
    private float currentThrottleLerp = 0f;
    private float currentAccelerationLerp = 0f;

    [Header("Boost Transition Settings")]
    [SerializeField] private float boostTransitionSpeed = 5f; // Controla a velocidade da transição
    [SerializeField] private AnimationCurve boostCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Curva para controlar a animação

    private void Boost()
    {
        if (!tankProperties.can_boost) return;

        // Definir alvos baseado no estado do boost
        float targetSpeed;
        float targetThrottle;
        float targetAcceleration;
        bool shouldBoost;

        if (Settings.Instance._controls.is_vehicle_boost_on_hold)
        {
            shouldBoost = Input.GetKey(Settings.Instance._keybinds.TANK_boostKey) && moveForward > 0;
        }
        else
        {
            if (Input.GetKeyDown(Settings.Instance._keybinds.TANK_boostKey))
                is_boosting = !is_boosting;

            if (moveForward <= 0)
            {
                is_boosting = false;

            }
            shouldBoost = is_boosting;
        }

        // Definir valores alvo
        if (shouldBoost)
        {
            targetSpeed = boost_max_speed;
            targetThrottle = boost_max_throttle;
            targetAcceleration = boost_acceletarion;
        }
        else
        {
            targetSpeed = tankProperties.max_speed;
            targetThrottle = tankProperties.max_throttle;
            targetAcceleration = tankProperties.acceleration;
        }

        // Calcular transições suaves
        float deltaTimeMultiplier = boostTransitionSpeed * Time.deltaTime;

        // Usar curvas para interpolação mais controlada
        float boostFactor = shouldBoost ? 1f : 0f;

        currentSpeedLerp = Mathf.MoveTowards(currentSpeedLerp, boostFactor, deltaTimeMultiplier);
        currentThrottleLerp = Mathf.MoveTowards(currentThrottleLerp, boostFactor, deltaTimeMultiplier);
        currentAccelerationLerp = Mathf.MoveTowards(currentAccelerationLerp, boostFactor, deltaTimeMultiplier);

        // Aplicar curva de animação
        float curvedSpeedLerp = boostCurve.Evaluate(currentSpeedLerp);
        float curvedThrottleLerp = boostCurve.Evaluate(currentThrottleLerp);
        float curvedAccelerationLerp = boostCurve.Evaluate(currentAccelerationLerp);

        // Interpolar valores
        current_max_speed = Mathf.Lerp(tankProperties.max_speed, boost_max_speed, curvedSpeedLerp);
        current_acceletarion = Mathf.Lerp(tankProperties.acceleration, boost_acceletarion, curvedAccelerationLerp);

    }

    private void WheelsController()
    {
        Vector3 pos;
        Quaternion rot;

        if (moveForward == 0 && moveSideways == 0) //Stand still
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
        else if (moveForward > 0 && moveSideways == 0) // Move Foward
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
        else if (moveForward < 0 && moveSideways == 0)// Move Backwads
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
        else if (moveForward == 0 && moveSideways > 0) // Rotate Right
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }

        }
        else if (moveForward == 0 && moveSideways < 0) // Rotate Left
        {
            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

        }
        else if (moveForward > 0 && moveSideways < 0) // Move fowards and rotate left
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle) / 2;
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
        else if (moveForward > 0 && moveSideways > 0) // Move fowards and rotate right
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle) / 2;
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
        else if (moveForward < 0 && moveSideways < 0) // Move backward and rotate left
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle) / 2;
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
        else if (moveForward < 0 && moveSideways > 0) // Move backward and rotate right
        {
            for (int i = 0; i < left_weels.Length; i++)
            {
                left_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle);
                left_weels[i].GetWorldPose(out pos, out rot);
                left_weels_transform[i].position = pos;
                left_weels_transform[i].rotation = rot;
            }

            for (int i = 0; i < right_weels.Length; i++)
            {
                right_weels[i].motorTorque = Math.Clamp(-tankProperties.acceleration * Time.deltaTime, -tankProperties.max_throttle, tankProperties.max_throttle) / 2;
                right_weels[i].GetWorldPose(out pos, out rot);
                right_weels_transform[i].position = pos;
                right_weels_transform[i].rotation = rot;
            }
        }
    }


    private void RotateInput()
    {
        moveSideways = 0;


        if (Input.GetKey(Settings.Instance._keybinds.TANK_turn_left_key) && Input.GetKey(Settings.Instance._keybinds.TANK_turn_right_key))
        {
            moveSideways = 0;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.TANK_turn_right_key))
        {
            moveSideways = 1;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.TANK_turn_left_key))
        {
            moveSideways = -1;
        }


    }

    private bool applied_speed_rotation;
    private bool applied_break_rotation;
    private float currentRotationForce = 0f;
    private float targetRotationForce = 0f;
    [SerializeField] private float rotationForceSmoothing = 10f; // Controla a suavidade

    private int previous_move_foward;
    private void ThrottleInput()
    {
        targetRotationForce = 0f;

        if (Input.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle) && !Input.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle))
        {
            applied_break_rotation = false;
            moveForward = 1;
            if (!applied_speed_rotation)
            {
                applied_speed_rotation = true;
                // Definir força alvo gradualmente
                targetRotationForce = -4000;
            }
        }
        else if (Input.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle) && !Input.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle))
        {
            applied_speed_rotation = false;
            moveForward = -1;
            if (!applied_break_rotation)
            {
                applied_break_rotation = true;
                // Definir força alvo gradualmente
                targetRotationForce = 4000;
            }
        }
        else
        {
            moveForward = 0;
            targetRotationForce = 0;
            applied_break_rotation = false;
            applied_speed_rotation = false;
        }

        // Suavizar transição da força
        currentRotationForce = Mathf.Lerp(currentRotationForce, targetRotationForce,
                                        rotationForceSmoothing * Time.deltaTime);

        if (speed < 2 || (moveForward != previous_move_foward && moveForward != 0))
        {
            rb.AddTorque(transform.right * currentRotationForce * rb.mass);
        }

        if (moveForward != 0) previous_move_foward = moveForward;


    }
    #endregion

    #region  Turet & Cannon

    private void RotateTurret()
    {

        mouseX = Math.Clamp(Input.GetAxis("Mouse X") * Settings.Instance._controls.tank_sensibility,
                           -tankProperties.turret_max_rotation_value, tankProperties.turret_max_rotation_value);

        turret.transform.Rotate(Vector3.up * mouseX * tankProperties.turret_rotation_value / 20);
    }

    private void RotateCannon()
    {
        float mouseInput = Math.Clamp(Input.GetAxisRaw("Mouse Y") * Settings.Instance._controls.tank_sensibility,
                           -tankProperties.turret_max_rotation_value, tankProperties.turret_max_rotation_value);

        float rotationAmount = -mouseInput * tankProperties.turret_rotation_value / 20;

        // Obtém a rotação atual
        Vector3 currentRotation = tankCannon.transform.localEulerAngles;

        // Converte Z para range -180 a 180
        float currentX = currentRotation.x;
        if (currentX > 180f) currentX -= 360f;

        // Calcula nova rotação
        cannon_rotation_amount = currentX + rotationAmount;

        // Aplica limites diferentes para cada direção
        cannon_rotation_amount = Mathf.Clamp(cannon_rotation_amount, -maxRotationUp, maxRotationDown);

        // Aplica a rotação
        tankCannon.transform.localEulerAngles = new Vector3(
            cannon_rotation_amount,
            currentRotation.y,
            currentRotation.z
        );
    }


    #endregion

    #region Systems

    private void Zoom()
    {
        if (!vehicle_camera.enabled) return;

        if (Input.GetKey(Settings.Instance._keybinds.TANK_zoom_key))
        {

            float targetFov = minFov / tankProperties.zoom;
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

    private void UpdateLightState()
    {
        if (start_engine)
        {
            foreach (Light light in lights)
            {
                light.enabled = true;
            }
        }
        else
        {
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
        }
    }

    protected override void Start_Stop_Engine()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            UpdateLightState();
        }
    }

    private void SwitchGun()
    {
        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();

        if (scrollDelta.y != 0)
        {
            usingMainCannon = !usingMainCannon;
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon1))
        {
            usingMainCannon = true;
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon2))
        {
            usingMainCannon = false;

        }
    }

    #endregion


    #region Shoot

    private void HandleShooting()
    {

        if (usingMainCannon)
        {
            ShootMainCannon();
        }
        else
        {
            ShootMachineGun();
        }


    }

    private void ShootMainCannon()
    {
        if (cannon_shoot_delay == tankMainShell.reload_time && Input.GetKeyDown(Settings.Instance._keybinds.TANK_shoot_key))
        {
            if (main_cannon_sound != null) HandleSound(main_cannon_sound);
            GameObject current_shell = Instantiate(tankMainShell.gameObject, cannonShootPos.position, cannonShootPos.rotation);
            Instantiate(shoot_main_cannon_explosion, cannonShootPos.position, cannonShootPos.rotation);
            current_shell.GetOrAddComponent<TankMainShell>().Shoot(cannonShootPos.forward);
            ApplyCannonRecoil();
            StartCoroutine(ReloadMainCannon());
        }

    }

    private void ApplyCannonRecoil()
    {
        // Calcular a direção oposta à rotação do canhão
        float currentCannonRotation = turret.transform.eulerAngles.y;

        // Converter para o range -180 a 180
        if (currentCannonRotation > 180f)
            currentCannonRotation -= 360f;

        // Normalizar a rotação para obter uma direção (positiva = para cima, negativa = para baixo)
        float rotationDirection = Mathf.Sign(currentCannonRotation);

        float recoilForce = tankMainShell.recoil_force * rotationDirection;

        // Aplicar torque oposto à direção do canhão
        // Se o canhão está apontando para cima (rotação positiva), o recuo empurra para baixo
        // Se o canhão está apontando para baixo (rotação negativa), o recuo empurra para cima
        Vector3 recoilTorque = turret.transform.right * (-rotationDirection * recoilForce);

        // Aplicar o torque
        rb.AddTorque(recoilTorque * (rb.mass / 2), ForceMode.Impulse);

    }

    private void ShootMachineGun()
    {
        float deltaTime = Time.deltaTime;

        bool isShooting = Input.GetKey(Settings.Instance._keybinds.TANK_shoot_key);
        canShootMainGun = !is_pilot_gun_overheated && isShooting;

        if (canShootMainGun)
        {
            if (pilot_gun_next_time_to_fire <= 0f)
            {
                //tankProperties.shoot_shound.PlayOneShot(tankProperties.shoot_shound.clip);
                HandleSound(tankProperties.shoot_shound);

                Transform bulletObj = Instantiate(
                    tankProperties.bullefPref,
                    pilotGunShootPos.position,
                    pilotGunShootPos.rotation
                );

                bulletObj.GetComponent<Bullet>().CreateBullet(
                    pilotGunShootPos.forward,
                    tankProperties.muzzle_velocity,
                    tankProperties.bullet_dropoff,
                    tankProperties.damage,
                    tankProperties.damage_dropoff,
                    tankProperties.damage_dropoff_timer,
                    tankProperties.destruction_force,
                    tankProperties.minimum_damage,
                    2, 2, 0, true,
                    tankProperties.damage,
                    tankProperties.bullet_hit_effect
                );

                Destroy(bulletObj.gameObject, 10f);
                pilot_gun_next_time_to_fire = tankProperties.interval;

                // Adicionar recuo à metralhadora
                ApplyMachineGunRecoil();
            }

            pilot_gun_overheat_amount += deltaTime;

            if (pilot_gun_overheat_amount >= tankProperties.overheat_time)
                is_pilot_gun_overheated = true;
        }

        pilot_gun_next_time_to_fire -= deltaTime;
    }

    private void ApplyMachineGunRecoil()
    {
        if (tankGunnerGun == null || isGunnerGunRecoiling) return;

        if (gunnerGunRecoilCoroutine != null)
            StopCoroutine(gunnerGunRecoilCoroutine);

        gunnerGunRecoilCoroutine = StartCoroutine(MachineGunRecoilRoutine());
    }

    private IEnumerator MachineGunRecoilRoutine()
    {
        isGunnerGunRecoiling = true;

        // Configurações do recuo da metralhadora (mais rápido e curto que o canhão principal)
        float recoilDistance = 0.4f; // Distância do recuo
        float recoilDuration = tankProperties.interval / 2; // Duração do recuo (mais rápido)
        float returnDuration = tankProperties.interval / 2; // Duração do retorno

        // Direção do recuo (para trás no eixo local Z)
        Vector3 localRecoilDirection = -Vector3.forward;
        Vector3 recoilPosition = gunnerGunOriginalLocalPosition + (localRecoilDirection * recoilDistance);

        // Fase 1: Recuo rápido
        float recoilTimer = 0f;
        while (recoilTimer < recoilDuration)
        {
            recoilTimer += Time.deltaTime;
            float t = recoilTimer / recoilDuration;
            tankGunnerGun.localPosition = Vector3.Lerp(
                gunnerGunOriginalLocalPosition,
                recoilPosition,
                t
            );
            yield return null;
        }

        // Fase 2: Retorno à posição original
        float returnTimer = 0f;
        while (returnTimer < returnDuration)
        {
            returnTimer += Time.deltaTime;
            float t = returnTimer / returnDuration;
            tankGunnerGun.localPosition = Vector3.Lerp(
                recoilPosition,
                gunnerGunOriginalLocalPosition,
                t
            );
            yield return null;
        }

        // Garantir posição exata
        tankGunnerGun.localPosition = gunnerGunOriginalLocalPosition;
        tankGunnerGun.localRotation = gunnerGunOriginalLocalRotation;

        isGunnerGunRecoiling = false;
        gunnerGunRecoilCoroutine = null;
    }

    private void CoolDownGun()
    {
        float deltaTime = Time.deltaTime;

        float coolSpeed = is_pilot_gun_overheated ? (deltaTime / 3f) : deltaTime / 2;
        pilot_gun_overheat_amount = Mathf.MoveTowards(pilot_gun_overheat_amount, 0f, coolSpeed);
        if (pilot_gun_overheat_amount <= 0)
        {
            is_pilot_gun_overheated = false;
        }
    }

    private IEnumerator ReloadMainCannon()
    {
        float reloadTime = tankMainShell.reload_time;
        cannon_shoot_delay = reloadTime;

        // Salva a posição e rotação LOCAL original
        Vector3 originalLocalPosition = retactableTankCannon.localPosition;
        Quaternion originalLocalRotation = retactableTankCannon.localRotation;

        // Usa o eixo Z LOCAL do parent para o recuo
        Transform parentTransform = retactableTankCannon.parent;
        Vector3 localRecoilDirection = -Vector3.forward; // Sempre recua no eixo Z negativo local

        // Converte a direção local para direção local do parent se necessário
        Vector3 recoilPosition = originalLocalPosition + (localRecoilDirection * 5);

        // Fase 1: Recuo rápido (5% do tempo de recarga)
        float recoilDuration = reloadTime * 0.05f;
        float recoilTimer = 0f;

        while (recoilTimer < recoilDuration)
        {
            recoilTimer += Time.deltaTime;
            float t = recoilTimer / recoilDuration;
            retactableTankCannon.localPosition = Vector3.Lerp(originalLocalPosition, recoilPosition, t);
            cannon_shoot_delay = reloadTime - recoilTimer;
            yield return null;
        }

        // Fase 2: Pausa na posição recuada (10% do tempo)
        float holdDuration = reloadTime * 0.1f;
        yield return new WaitForSeconds(holdDuration);
        cannon_shoot_delay = reloadTime - recoilDuration - holdDuration;

        // Fase 3: Retorno à posição original (85% do tempo)
        float returnDuration = reloadTime * 0.85f;
        float returnTimer = 0f;

        while (returnTimer < returnDuration)
        {
            returnTimer += Time.deltaTime;
            float t = returnTimer / returnDuration;
            retactableTankCannon.localPosition = Vector3.Lerp(recoilPosition, originalLocalPosition, t);
            cannon_shoot_delay = reloadTime - recoilDuration - holdDuration - returnTimer;
            yield return null;
        }

        // Garante que volte à posição exata
        retactableTankCannon.localPosition = originalLocalPosition;
        cannon_shoot_delay = reloadTime;
    }

    #endregion


    #region Entry / Exit

    public override void EnterVehicle(GameObject _player)
    {
        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(true);
        player = _player;

        playerProperties = _player.GetComponent<PlayerProperties>();
        playerController = _player.GetComponent<PlayerController>();

        playerProperties.is_in_vehicle = true;
        is_in_vehicle = true;

        _exitCooldown = 0;

        vehicle_camera.enabled = true;
        vehicle_camera.GetComponent<AudioListener>().enabled = true;
        player.transform.SetParent(turret.transform);

        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        player.SetActive(false);
    }

    protected override void ExitVehicle()
    {
        base.ExitVehicle();
        vehicle_camera.enabled = false;
        vehicle_camera.GetComponent<AudioListener>().enabled = false;
    }


    #endregion

    #region HUD

    protected override void UpdateHUD()
    {
        tankHudManager.UpdateSpeed(speed);
        tankHudManager.ChangeHeatIndicatorActive(!usingMainCannon);
        tankHudManager.UpdateHeat(pilot_gun_overheat_amount);
        tankHudManager.UpdateCannonRotation(cannon_rotation_amount);
        if (cannon_shoot_delay != tankMainShell.reload_time)
        {
            tankHudManager.UpdateMainCannonStatus("Reloading... [" + cannon_shoot_delay.ToString("F1") + "]");
        }
        else
        {
            tankHudManager.UpdateMainCannonStatus("Ready!");
        }
    }

    #endregion

    #region Destruction

    protected override void DestroyAnimation()
    {
        base.DestroyAnimation();
        fire_effects_parent.SetActive(true);
        StartCoroutine(DelayToExplode(5));
    }

    IEnumerator DelayToExplode(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Ground"), 20);
    }


    #endregion

}