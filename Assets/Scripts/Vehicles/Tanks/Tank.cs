using System;
using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class Tank : Vehicle
{
    [Header("Properties")]
    [SerializeField] private TankProperties tankProperties;

    [Header("Instances")]
    [SerializeField] private GameObject turret;
    [SerializeField] protected Light[] lights;
    public WheelCollider[] left_weels;
    public WheelCollider[] right_weels;
    public Transform[] left_weels_transform;
    public Transform[] right_weels_transform;

    [Header("Audio")]
    [SerializeField] private AudioSource engine_sound;
    [SerializeField] private AudioSource secondary_cannon_sound;

    [Header("Guns")]
    [SerializeField] private Transform tankCannon;

    [Header("Public fields")]
    public float maxRotationUp = 45f;
    public float maxRotationDown = 45f;

    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public int moveForward;
    [HideInInspector] public int moveSideways;
    [HideInInspector] public float gunner_gun_overheat_amount;

    #region Private Fields
    private float _exitCooldown;
    private float boost_max_throttle;
    private float boost_max_speed;
    private float current_max_speed;
    private float current_acceletarion;
    private float boost_acceletarion;
    private bool is_boosting;
    private float cannon_rotation_amount;
    #endregion

    #region Unity Lifecycle

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (countermeasures != null && Settings.Instance != null) countermeasures.SetUseCountermeasureKey(Settings.Instance._keybinds.VEHICLE_countermeasureKey);

        InitiaizeClientItens();
        SetHpProperties(tankProperties.hp, tankProperties.resistance);
    }

    private void InitiaizeClientItens()
    {
        current_max_speed = tankProperties.max_speed;
        current_acceletarion = tankProperties.acceleration;

        boost_max_throttle = tankProperties.max_throttle * tankProperties.boost_force;
        boost_max_speed = tankProperties.max_speed * tankProperties.boost_force;
        boost_acceletarion = tankProperties.acceleration * tankProperties.boost_force;
    }

    bool did_play_destroy_animation = false;
    protected override void Update()
    {
        if (!IsOwner) return;

        if (is_in_vehicle)
        {
            _exitCooldown += Time.deltaTime;

            SwitchWeapon();
            HandleShooting();

            if (!vehicle_destroyed.Value)
            {
                StartStopEngine();
                if (start_engine == true)
                {
                    Boost();
                    RotateCannon();
                    RotateTurret();
                }
            }
            if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && _exitCooldown > 0.1f) ExitVehicle();

        }

        if (vehicle_destroyed.Value && !did_play_destroy_animation)
        {
            DestroyAnimation();
            did_play_destroy_animation = true;
        }
    }

    protected override void FixedUpdate()
    {
        if (!IsOwner) return;

        speed = rb.linearVelocity.magnitude;
        if (is_in_vehicle && start_engine == true && !SettingsHUD.Instance.is_menu_settings_active && !vehicle_destroyed.Value)
        {
            Move();
        }
        WheelsController();
        rb.AddForce(Vector3.down * rb.mass * 50);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && rb.linearVelocity.magnitude != 0)
        {
            PlayerController hit_playerController = collision.gameObject.GetComponent<PlayerController>();
            if (hit_playerController != null) hit_playerController.RequestDamage(speed * 10);
        }

        if (!IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            return;
        }

        if (rb.linearVelocity.magnitude < 10) return;

        ContactPoint contact = collision.contacts[0];
        voxCollider.destructionRadius = Math.Clamp(rb.linearVelocity.magnitude, 0, 30);

        voxCollider.SphereExplosion(contact.point, 0, 0);
        ApplyFallUpperVoxels(collision, contact, voxCollider.destructionRadius);
    }

    #endregion

    #region Engine & Movement

    protected override void Move()
    {
        ThrottleInput();
        RotateInput();

        if (moveForward != 0)
        {
            float moveForce = moveForward * current_acceletarion * rb.mass;
            Vector3 forwardForce = transform.forward * moveForce;
            rb.AddForce(forwardForce, ForceMode.Force);

            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (flatVel.magnitude > current_max_speed)
            {
                Vector3 limitedVel = flatVel.normalized * current_max_speed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

        if (moveSideways != 0)
        {
            if (Mathf.Abs(rb.angularVelocity.y) < tankProperties.max_rotation_speed)
            {
                float turnForce = moveSideways * tankProperties.rotation_value * rb.mass * 50;
                rb.AddTorque(transform.up * turnForce, ForceMode.Force);
            }
        }
    }

    private float currentSpeedLerp = 0f;
    private float currentThrottleLerp = 0f;
    private float currentAccelerationLerp = 0f;

    [Header("Boost Transition Settings")]
    [SerializeField] private float boostTransitionSpeed = 5f;
    [SerializeField] private AnimationCurve boostCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Boost()
    {
        if (!tankProperties.can_boost) return;

        float targetSpeed;
        float targetThrottle;
        float targetAcceleration;
        bool shouldBoost;

        if (Settings.Instance._controls.is_vehicle_boost_on_hold)
        {
            shouldBoost = InputManager.GetKey(Settings.Instance._keybinds.TANK_boostKey) && moveForward > 0;
        }
        else
        {
            if (InputManager.GetKeyDown(Settings.Instance._keybinds.TANK_boostKey))
                is_boosting = !is_boosting;

            if (moveForward <= 0)
            {
                is_boosting = false;
            }
            shouldBoost = is_boosting;
        }

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

        float deltaTimeMultiplier = boostTransitionSpeed * Time.deltaTime;
        float boostFactor = shouldBoost ? 1f : 0f;

        currentSpeedLerp = Mathf.MoveTowards(currentSpeedLerp, boostFactor, deltaTimeMultiplier);
        currentThrottleLerp = Mathf.MoveTowards(currentThrottleLerp, boostFactor, deltaTimeMultiplier);
        currentAccelerationLerp = Mathf.MoveTowards(currentAccelerationLerp, boostFactor, deltaTimeMultiplier);

        float curvedSpeedLerp = boostCurve.Evaluate(currentSpeedLerp);
        float curvedAccelerationLerp = boostCurve.Evaluate(currentAccelerationLerp);

        current_max_speed = Mathf.Lerp(tankProperties.max_speed, boost_max_speed, curvedSpeedLerp);
        current_acceletarion = Mathf.Lerp(tankProperties.acceleration, boost_acceletarion, curvedAccelerationLerp);
    }

    private void WheelsController()
    {
        Vector3 pos;
        Quaternion rot;

        if (moveForward == 0 && moveSideways == 0)
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
        else if (moveForward > 0 && moveSideways == 0)
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
        else if (moveForward < 0 && moveSideways == 0)
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
        else if (moveForward == 0 && moveSideways > 0)
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
        else if (moveForward == 0 && moveSideways < 0)
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
        else if (moveForward > 0 && moveSideways < 0)
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
        else if (moveForward > 0 && moveSideways > 0)
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
        else if (moveForward < 0 && moveSideways < 0)
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
        else if (moveForward < 0 && moveSideways > 0)
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

        if (InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_left_key) && InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_right_key))
        {
            moveSideways = 0;
        }
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_right_key))
        {
            moveSideways = 1;
        }
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_left_key))
        {
            moveSideways = -1;
        }
    }

    private bool applied_speed_rotation;
    private bool applied_break_rotation;
    private float currentRotationForce = 0f;
    private float targetRotationForce = 0f;
    [SerializeField] private float rotationForceSmoothing = 10f;

    private int previous_move_foward;
    private void ThrottleInput()
    {
        targetRotationForce = 0f;

        if (InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle))
        {
            applied_break_rotation = false;
            moveForward = 1;
            if (!applied_speed_rotation)
            {
                applied_speed_rotation = true;
                targetRotationForce = -4000;
            }
        }
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle))
        {
            applied_speed_rotation = false;
            moveForward = -1;
            if (!applied_break_rotation)
            {
                applied_break_rotation = true;
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

        currentRotationForce = Mathf.Lerp(currentRotationForce, targetRotationForce,
                                        rotationForceSmoothing * Time.deltaTime);

        if (speed < 2 || (moveForward != previous_move_foward && moveForward != 0))
        {
            rb.AddTorque(transform.right * currentRotationForce * rb.mass);
        }

        if (moveForward != 0) previous_move_foward = moveForward;
    }
    #endregion

    #region Turret & Cannon

    private void RotateTurret()
    {
        mouseX = Math.Clamp(InputManager.GetAxis("Mouse X") * Settings.Instance._controls.tank_sensibility,
                           -tankProperties.turret_max_rotation_value, tankProperties.turret_max_rotation_value);

        turret.transform.Rotate(Vector3.up * mouseX * tankProperties.turret_rotation_value / 20);
    }

    private void RotateCannon()
    {
        float mouseInput = Math.Clamp(InputManager.GetAxisRaw("Mouse Y") * Settings.Instance._controls.tank_sensibility,
                           -tankProperties.turret_max_rotation_value, tankProperties.turret_max_rotation_value);

        float rotationAmount = -mouseInput * tankProperties.turret_rotation_value / 20;

        Vector3 currentRotation = tankCannon.transform.localEulerAngles;

        float currentX = currentRotation.x;
        if (currentX > 180f) currentX -= 360f;

        cannon_rotation_amount = currentX + rotationAmount;
        cannon_rotation_amount = Mathf.Clamp(cannon_rotation_amount, -maxRotationUp, maxRotationDown);

        tankCannon.transform.localEulerAngles = new Vector3(
            cannon_rotation_amount,
            currentRotation.y,
            currentRotation.z
        );
    }
    #endregion

    #region Systems
    public override float GetMinFov()
    {
        return Settings.Instance._video.tank_fov;
    }

    private void UpdateLightState()
    {
        if (start_engine == true)
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

    protected override void StartStopEngine()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            UpdateLightState();
        }
    }
    #endregion

    #region Shoot
    private void HandleShooting()
    {
        currentSeat.currentArmory.Shoot();
    }

    // Método utilitário público para permitir que a arma acesse o sistema de áudio protegido da classe Vehicle
    public void PlayWeaponSound(AudioSource audioSource)
    {
        HandleSound(audioSource);
    }
    #endregion

    #region Entry / Exit

    [TargetRpc]
    public override void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        CallBaseEnterVehicle(conn, _player);
        is_in_vehicle = true;
        _exitCooldown = 0;
        CmdDisablePlayer(_player);
    }

    private void CallBaseEnterVehicle(NetworkConnection conn, GameObject _player)
    {
        base.EnterVehicle(conn, _player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdDisablePlayer(GameObject _player)
    {
        RpcDisablePlayer(_player);
    }

    [ObserversRpc]
    private void RpcDisablePlayer(GameObject _player)
    {
        if (_player != null) _player.SetActive(false);
    }

    protected override void ExitVehicle()
    {
        GameObject player = currentSeat.playerGameObject;
        CmdEnablePlayer(player);
        base.ExitVehicle();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdEnablePlayer(GameObject _player)
    {
        RpcEnablePlayer(_player);
    }

    [ObserversRpc]
    private void RpcEnablePlayer(GameObject _player)
    {
        if (_player != null) _player.SetActive(true);
    }
    #endregion

    #region Destruction
    protected override void DestroyAnimation()
    {
        CmdEnableFire();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdEnableFire()
    {
        EnableFire();
    }

    [ObserversRpc]
    private void EnableFire()
    {
        StartCoroutine(DelayToExplode(5));
        fire_effects_parent.SetActive(true);
    }

    private IEnumerator DelayToExplode(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Ground"), 20);
    }

    protected override void CameraController()
    {
        throw new NotImplementedException();
    }
    #endregion
}