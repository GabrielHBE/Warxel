using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Jet : Vehicle
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private Transform eject_position;
    [SerializeField] private JetHudManager _hudManager;
    [SerializeField] private Transform _playerPosition;
    [SerializeField] private GameObject _mainCannon;
    [SerializeField] private GameObject _trails;
    [SerializeField] private Transform _core;
    [SerializeField] private Transform _shootPosition;
    //[SerializeField] private Image _blackImage;
    [SerializeField] private GameObject _turbineSmoke;
    [SerializeField] private JetProperties _properties;
    [SerializeField] private JetBombsAndMissiles bombsAndMissiles;


    [Header("Sound")]
    [SerializeField] private AudioSource _stopShooting;
    [SerializeField] private AudioSource _tinnitusAudio;
    [SerializeField] private AudioSource _bulletHitAudio;


    #endregion

    #region Public Fields

    [Header("State")]
    [HideInInspector] public readonly SyncVar<bool> is_pilot_seat_occupied = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ClientUnsynchronized));
    [HideInInspector] public bool is_pilot = true; // Jet sempre tem apenas um piloto
    [HideInInspector] public bool isNearGround;
    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public float moveForward;

    [HideInInspector] public bool retractLandingGear = false;
    [HideInInspector] public bool usingMainCannon = true;
    [HideInInspector] public bool isWheelTouchingGround = true;

    [HideInInspector] public float _overheatAmount;
    [HideInInspector] public float leanValue;

    #endregion

    #region Private Fields

    private float _nextFireTime = 0;

    private bool _isOverheated;
    private float _passoutTimer;
    private bool _isPassingOut;
    private float _diveSpeedModifier;
    private float _afterburnerSpeedModifier;
    private float _totalThrottle;
    private float _exitCooldown;
    private Volume _volume;
    private float _currentGravity = 0;
    private float _downwardComponent;
    private float _gForce;
    private float _maxSpeed = 700;
    private float _mainCannonRotationValue = 0;
    private float _shootDelayTimer = 0;
    private float current_spread;

    #endregion

    #region Unity Lifecycle

    public override void Initialize()
    {
        base.Initialize();

        if (bombsAndMissiles.missile != null) _hudManager.SetImages(_properties.hud_icon, bombsAndMissiles.missile.image_hud, countermeasures.image_icon_hud);
        if (bombsAndMissiles.bombs != null) _hudManager.SetImages(_properties.hud_icon, bombsAndMissiles.bombs.image_hud, countermeasures.image_icon_hud);

        rb.mass = _properties.mass;
        SetHpProperties(_properties.hp, _properties.resistance);

        _turbineSmoke.SetActive(false);
        _volume = GetGlobalVolume();
    }

    protected override void FixedUpdate()
    {
        if (!IsOwner) return;

        if (!vehicle_destroyed.Value)
        {
            // Só aplica movimento se o veículo estiver ocupado e o motor ligado
            if (start_engine && is_in_vehicle && !SettingsHUD.Instance.is_menu_settings_active && is_pilot_seat_occupied.Value == true)
            {
                Lean();
                Move();
                Rotate();
                ApplyDiveSpeedBoost();
            }

            ApplyGravityModifier();
            rb.AddForce(Physics.gravity * _currentGravity, ForceMode.Acceleration);

            if (speed < _maxSpeed)
            {
                _totalThrottle = throttle + _diveSpeedModifier + _afterburnerSpeedModifier;
                rb.AddForce(transform.forward * _totalThrottle * _properties.max_throttle);
            }
        }
        else
        {
            DestroyAnimation();
        }
    }

    protected override void Update()
    {
        if (!IsOwner) return;

        speed = rb.linearVelocity.magnitude;

        if (is_in_vehicle)
        {
            player.transform.position = _playerPosition.position;
            player.transform.rotation = _playerPosition.rotation;

            if (!SettingsHUD.Instance.is_menu_settings_active)
            {
                minFov = Settings.Instance._video.jet_fov;

                if (Input.GetKeyDown(KeyCode.P))
                {
                    RequestDamage(100);
                }

                UpdateHUD();
                PilotBehaviour();
            }


        }
        else
        {
            SlowDownEngine();
        }

        current_spread = Math.Clamp(current_spread, 0, _properties.max_spread);

        UpdateEngineSound();


    }

    protected void OnCollisionStay(Collision collision)
    {
        if (vehicle_destroyed.Value && IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            ContactPoint contact = collision.contacts[0]; // Primeiro ponto de contato
            Vector3 contactPoint = contact.point; // Ponto da colisão
            Vector3 contactNormal = contact.normal; // Normal da colisão

            Explode(contactPoint, contactNormal, collision.gameObject.layer, 12);
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {

        if (isWheelTouchingGround || collision.gameObject.GetComponent<Missiles>() != null) return;

        if (vehicle_destroyed.Value || hp.Value <= 0)
        {
            if (playerController != null) playerController.RequestDamage(1000);
            ExitVehicle();
        }

        if (speed < 300)
        {
            base.OnCollisionEnter(collision);
        }
        else
        {
            HandleCollision(collision, 50);
            Explode(collision.contacts[0].point, transform.localEulerAngles.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }



    }

    #endregion

    #region Engine & Movement

    protected void PilotBehaviour()
    {

        ThrottleInput();
        _exitCooldown += Time.deltaTime;

        CameraController();
        FreeLook();
        CalculateGForce();

        if (_properties.can_afterburner)
            AfterBurner();

        HandleExitInput();

        if (!vehicle_destroyed.Value)
        {
            Start_Stop_Engine();
            Shoot();
            SwitchWeapon();

        }

        if (start_engine == true)
        {
            HandleLeanInput();
            HandlePassout();
            _turbineSmoke.SetActive(true);
            UpdateLandingGear();
        }
        else
        {
            _turbineSmoke.SetActive(false);
            SlowDownEngine();
        }
    }

    private void ThrottleInput()
    {
        moveForward = 0;

        if (Input.GetKey(Settings.Instance._keybinds.JET_speedUpKey) && Input.GetKey(Settings.Instance._keybinds.JET_speedDownKey))
        {
            moveForward = 0;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.JET_speedUpKey))
        {
            moveForward = 1;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.JET_speedDownKey))
        {
            moveForward = -1;
        }
    }

    protected void SlowDownEngine()
    {
        ResetSpeedModifiers();
        //_properties.interior_turbine.pitch = Math.Clamp(_properties.interior_turbine.pitch, 0.01f, 2);
        throttle = Mathf.Lerp(throttle, 0, Time.deltaTime / 2);
    }


    protected override void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (isNearGround && mouseY > 0 && speed > 50)
        {
            rb.AddForce(Vector3.up * rb.mass * 20);
        }

        if (transform.position.y < MapSettings.Instance.max_altitude)
        {
            HandleThrottleControl(deltaTime);
            return;
        }

        SlowDownEngine();


    }

    protected void HandleThrottleControl(float deltaTime)
    {

        if (moveForward > 0 && !_isPassingOut)
        {
            IncreaseThrottle(deltaTime);
        }
        else if (moveForward < 0 && !_isPassingOut)
        {
            DecreaseThrottle(deltaTime);
        }
        else
        {
            Decelerate(deltaTime);
        }
    }

    protected void IncreaseThrottle(float deltaTime)
    {
        //_properties.interior_turbine.pitch = Mathf.MoveTowards( _properties.interior_turbine.pitch, 2f, 0.1f * deltaTime);

        throttle += _properties.aceleration * deltaTime;
        throttle = Mathf.Min(throttle, _properties.max_throttle);
    }

    protected void DecreaseThrottle(float deltaTime)
    {
        if (isNearGround)
        {
            if (throttle > -50)
            {
                //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.1f * deltaTime);
                throttle -= _properties.aceleration * deltaTime * 2;
            }
        }
        else
        {
            if (throttle > 100)
            {
                //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.1f * deltaTime);
                throttle -= _properties.aceleration * deltaTime;
            }
        }
    }

    protected void Decelerate(float deltaTime)
    {
        if (isNearGround)
        {
            float decelerationRate = _properties.aceleration * 0.8f;
            throttle = Mathf.MoveTowards(throttle, 0, decelerationRate * deltaTime);

            //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.1f * deltaTime);
        }
        else
        {
            throttle = Mathf.MoveTowards(throttle, 0, deltaTime);

            //_properties.interior_turbine.pitch = Mathf.MoveTowards(_properties.interior_turbine.pitch, 0.15f, 0.005f * deltaTime);
        }

        //_properties.interior_turbine.pitch = Mathf.Clamp(_properties.interior_turbine.pitch, 0.15f, 2f);
    }

    protected void Rotate()
    {
        if (_isPassingOut) return;

        float deltaTime = Time.fixedDeltaTime;

        mouseX = Math.Clamp(Input.GetAxis("Mouse X") * Settings.Instance._controls.jet_sensibility,
                           -_properties.max_rotation_value, _properties.max_rotation_value);
        mouseY = Math.Clamp(Input.GetAxis("Mouse Y") * Settings.Instance._controls.jet_sensibility,
                           -_properties.max_pitch_value, _properties.max_pitch_value);

        HandlePitchKeys();

        if (_properties.invertY)
            mouseY *= -1;

        if (Math.Abs(mouseY) > 1 && throttle > 0 && !isNearGround)
        {
            throttle -= Math.Abs(mouseY) * deltaTime * 10;
        }

        UpdateTrails();
        ApplyRotationTorque();
    }

    protected void HandlePitchKeys()
    {
        if (Input.GetKey(Settings.Instance._keybinds.JET_pitchUpKey))
            mouseY = _properties.max_pitch_value;
        if (Input.GetKey(Settings.Instance._keybinds.JET_pitchDownKey))
            mouseY = -_properties.max_pitch_value;
    }

    protected void UpdateTrails()
    {
        foreach (TrailRenderer trail in _trails.GetComponentsInChildren<TrailRenderer>())
        {
            bool shouldEmit = (mouseX > 10 || mouseX < 10 || mouseY > 10 || mouseY < 10)
                           && (mouseX != 0 || mouseY != 0) && speed > 100;

            if (shouldEmit && !trail.emitting)
            {
                trail.Clear();
                trail.emitting = true;
            }
            else if (!shouldEmit)
            {
                trail.emitting = false;
            }
        }
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(-transform.forward * mouseX * speed * _properties.rotation_value * 20);
        rb.AddTorque(-transform.right * mouseY * speed * _properties.pitch_value * 7);
    }

    protected void Lean()
    {
        if (_isPassingOut) return;

        float speedFactor = Mathf.Clamp01(speed / _maxSpeed);

        if (isNearGround && (throttle >= 20 || throttle < -10) && throttle <= 50)
        {
            if (Mathf.Abs(rb.angularVelocity.y) < _properties.max_lean_speed)
            {
                float turnForce = leanValue * _properties.lean_value * rb.mass * 70;
                rb.AddTorque(transform.up * turnForce, ForceMode.Force);
            }
        }
        else
        {
            if (Mathf.Abs(rb.angularVelocity.y) < _properties.max_lean_speed)
            {
                float turnForce = leanValue * _properties.lean_value * rb.mass * 5 * speedFactor;
                rb.AddTorque(transform.up * turnForce, ForceMode.Force);
            }
        }


    }

    protected void HandleLeanInput()
    {
        if (Input.GetKey(Settings.Instance._keybinds.JET_yawLeftKey))
        {
            leanValue = -1;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.JET_yawRightKey))
        {
            leanValue = 1;
        }
        else
        {
            leanValue = 0;
        }
    }

    #endregion

    #region Speed & Physics Modifiers

    protected void ApplyDiveSpeedBoost()
    {
        _downwardComponent = transform.forward.y;

        if (_downwardComponent > 0.3f) // Down
        {
            ApplyDiveBoost();
        }
        else if (_downwardComponent < -0.3f) // Up
        {
            ApplyClimbPenalty();
        }
        else
        {
            _diveSpeedModifier = Mathf.Lerp(_diveSpeedModifier, 0, 2 * Time.fixedDeltaTime);
        }
    }

    protected void ApplyDiveBoost()
    {
        float gravityBoost = _downwardComponent * Physics.gravity.magnitude * 0.5f;
        float aerodynamicBoost = _downwardComponent * _properties.dive_speed_boost;
        float totalBoost = (gravityBoost + aerodynamicBoost) * Time.fixedDeltaTime;

        totalBoost = Mathf.Clamp(totalBoost, 0, _properties.max_throttle * 1.2f);
        _diveSpeedModifier = totalBoost * 400 * Time.fixedDeltaTime;
    }

    protected void ApplyClimbPenalty()
    {
        float upwardIntensity = -_downwardComponent;
        float airResistance = upwardIntensity * Physics.gravity.magnitude * 0.3f;
        float gravityPenalty = upwardIntensity * _properties.dive_speed_boost * 0.5f;
        float totalPenalty = (airResistance + gravityPenalty) * Time.fixedDeltaTime;

        totalPenalty = Mathf.Clamp(totalPenalty, 0, _properties.max_throttle * 0.7f);
        _diveSpeedModifier = -totalPenalty * 400 * Time.fixedDeltaTime;
    }

    protected void ApplyGravityModifier()
    {
        if (isNearGround)
        {
            _currentGravity = 0;
            return;
        }

        float targetGravity = 1f;

        if (_downwardComponent > 0.3f) // Down
        {
            targetGravity = moveForward > 0
                ? (_properties.max_throttle / (speed * 2)) * -_downwardComponent
                : (_properties.max_throttle / speed) * -_downwardComponent;
        }
        else if (_downwardComponent < -0.3f) // Up
        {
            targetGravity = moveForward > 0
                ? 1.5f * -_downwardComponent
                : (_properties.max_throttle / speed) * -_downwardComponent;
        }
        else
        {
            if (moveForward > 0)
            {
                targetGravity = 0;
            }
            else if (throttle < 100)
            {
                targetGravity = _properties.max_throttle / (speed * 10);
            }
        }

        _currentGravity = Mathf.Lerp(_currentGravity, targetGravity, Time.fixedDeltaTime);
        _currentGravity = Mathf.Clamp(_currentGravity, 0f, 5f);
    }

    protected void CalculateGForce()
    {
        float deltaTime = Time.deltaTime;

        if (mouseY != 0)
        {
            _gForce = (deltaTime / 3) * speed * mouseY;
        }
        else
        {
            _gForce = Mathf.MoveTowards(_gForce, 0f, deltaTime * 5);
        }

        _gForce = Math.Clamp(_gForce, -10, 10);
    }

    #endregion

    #region Combat

    private bool _hasPlayedShootSound = false;

    protected virtual void Shoot()
    {
        float delayToShoot = 0.05f;
        float deltaTime = Time.deltaTime;

        bool isShooting = Input.GetKey(Settings.Instance._keybinds.JET_shootVehicleKey);
        bool canShootMain = usingMainCannon && !_isOverheated && isShooting;

        HandleShootDelay(canShootMain, deltaTime);

        bool readyToShoot = canShootMain && _shootDelayTimer >= delayToShoot;

        if (readyToShoot)
        {
            // 1. ÁUDIO: Toca o som localmente no PC do piloto usando .Play() para poder ser interrompido
            if (!_hasPlayedShootSound)
            {
                CmdPlayShootSound();
                _properties.shoot_sound.Play();
                _hasPlayedShootSound = true;
            }

            // 2. TEMPO DE TIRO: Calculado perfeitamente no Client
            if (_nextFireTime <= 0f)
            {
                // Calcula o spread da bala localmente
                float spreadX = UnityEngine.Random.Range(-current_spread, current_spread);
                float spreadY = UnityEngine.Random.Range(-current_spread, current_spread);
                float spreadZ = UnityEngine.Random.Range(-current_spread, current_spread);
                Quaternion spreadRotation = Quaternion.Euler(spreadX / 10, spreadY / 10, spreadZ / 10);

                // Soma a rotação da arma com o spread
                Quaternion finalRotation = _shootPosition.rotation * spreadRotation;

                // 3. Pede para o servidor criar a bala na rede com a rotação final
                CmdFireMainCannon(_shootPosition.position, finalRotation);

                current_spread += _properties.spread;
                _nextFireTime = _properties.interval;
            }

            _overheatAmount += deltaTime;
            _mainCannonRotationValue = _properties.fire_rate;

            if (_overheatAmount >= _properties.overheat_time)
                _isOverheated = true;
        }
        else
        {
            current_spread = 0;
            CoolDownCannon(deltaTime);

            // 4. Parar o som se o jogador soltou o botão ou a arma superaqueceu
            if (_hasPlayedShootSound)
            {
                CmdPlayStopShootSound();
                _properties.shoot_sound.Stop(); // Interrompe o loop da metralhadora
                _stopShooting.PlayOneShot(_stopShooting.clip); // Toca o som de "fim de tiro"
                _hasPlayedShootSound = false;
            }
        }

        _nextFireTime -= deltaTime;
        HandleSecondaryWeapon();
        RotateMainCannon(deltaTime);
    }

    [ServerRpc]
    protected void CmdPlayShootSound()
    {
        PlayShootSound();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void PlayShootSound()
    {
        _properties.shoot_sound.Play();
    }

    [ServerRpc]
    protected void CmdPlayStopShootSound()
    {
        PlayStopShootSound();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void PlayStopShootSound()
    {
        _properties.shoot_sound.Stop();
        _stopShooting.PlayOneShot(_stopShooting.clip);
    }

    // Agora o Servidor APENAS spawna a bala, tirando o peso dos cálculos
    [ServerRpc]
    protected void CmdFireMainCannon(Vector3 position, Quaternion rotation)
    {
        Transform bulletObj = Instantiate(
            _properties.bullefPref,
            position,
            rotation
        );

        Bullet.BulletData data = new Bullet.BulletData
        {
            position = position,
            rotation = rotation,
            direction = rotation * Vector3.forward,
            speed = _properties.muzzle_velocity,
            dropMultiplier = _properties.bullet_drop,
            infantaryDamage = _properties.infantary_damage,
            damageDropoff = _properties.damage_dropoff,
            damageDropoffTimer = _properties.damage_dropoff_timer,
            destructionForce = _properties.destruction_force,
            minimumDamage = _properties.minimum_damage,
            hsMultiplier = 2,
            size = 1,
            canDamageVehicles = true,
            vehicleDamage = _properties.vehicle_damage
        };

        Spawn(bulletObj.gameObject, Owner);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // Passa o GameObject do Jato (gameObject) para que a bala ignore a colisão com ele mesmo
            bullet.CreateBullet(data, transform);
        }
    }

    protected void HandleShootDelay(bool canShoot, float deltaTime)
    {
        if (canShoot)
        {
            _shootDelayTimer += deltaTime;
        }
        else
        {
            _shootDelayTimer = 0;
        }
    }

    protected void CoolDownCannon(float deltaTime)
    {
        _mainCannonRotationValue = Mathf.Lerp(_mainCannonRotationValue, 0f, deltaTime * 3f);
        float coolSpeed = _isOverheated ? (deltaTime / 2f) : deltaTime;

        _overheatAmount = Mathf.MoveTowards(_overheatAmount, 0f, coolSpeed);

        if (_overheatAmount <= 0)
        {
            _isOverheated = false;
        }
    }

    protected void HandleSecondaryWeapon()
    {

        if (!usingMainCannon && bombsAndMissiles != null)
        {

            if (bombsAndMissiles.missile != null) bombsAndMissiles.missile.Shoot(Settings.Instance._keybinds.JET_shootVehicleKey);
            if (bombsAndMissiles.bombs != null) bombsAndMissiles.bombs.Shoot(Settings.Instance._keybinds.JET_shootVehicleKey);

        }

    }

    protected void RotateMainCannon(float deltaTime)
    {
        _mainCannon.transform.Rotate(Vector3.left * _mainCannonRotationValue * deltaTime);
    }

    protected override void SwitchWeapon()
    {
        if (Mouse.current.scroll.ReadValue().y != 0)
        {
            usingMainCannon = !usingMainCannon;
        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon1))
        {
            usingMainCannon = true;

            if (bombsAndMissiles.missile != null) bombsAndMissiles.missile.SetActive(false);
            if (bombsAndMissiles.bombs != null) bombsAndMissiles.bombs.SetActive(false);

        }

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_weapon2))
        {

            usingMainCannon = false;
            if (bombsAndMissiles.missile != null) bombsAndMissiles.missile.SetActive(true);
            if (bombsAndMissiles.bombs != null) bombsAndMissiles.bombs.SetActive(true);
        }

    }

    #endregion

    #region Player Interaction
    [TargetRpc]
    public override void EnterVehicle(NetworkConnection conn, GameObject _player)
    {
        // Verifica se o assento já está ocupado
        if (is_pilot_seat_occupied.Value == true) return;

        CallBaseEnterVeicle(conn, _player);
        _exitCooldown = 0;

        is_pilot = true;
        is_pilot_seat_occupied.Value = true;

        player.transform.position = _playerPosition.position;
        player.transform.rotation = _playerPosition.rotation;

        _turbineSmoke.SetActive(start_engine);
    }

    private void CallBaseEnterVeicle(NetworkConnection conn, GameObject _player)
    {
        base.EnterVehicle(conn, _player);
    }

    protected void HandleExitInput()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && _exitCooldown > 0.1f)
        {
            _turbineSmoke.SetActive(false);

            if (throttle > 10)
            {
                EjectPlayer();
            }
            else
            {
                ExitVehicle();
            }
        }
    }

    protected override void ExitVehicle()
    {
        if (!is_in_vehicle) return;

        base.ExitVehicle();

        // Reseta os estados
        is_pilot_seat_occupied.Value = false;
        is_in_vehicle = false;
    }

    protected void EjectPlayer()
    {

        if (!is_in_vehicle) return;

        if (!player.activeSelf) player.SetActive(true);

        if (vehicleHudManager != null) vehicleHudManager.gameObject.SetActive(false);

        if (playerProperties != null)
        {
            playerProperties.is_in_vehicle = false;
            playerProperties = null;
        }

        if (playerController != null)
        {

            playerController.HideOwnerItems(true);
            playerController = null;
        }

        if (player != null)
        {
            player.transform.position = eject_position.position;
            player.transform.SetParent(null);
            player = null;
        }


        if (player_rb != null)
        {

            player_rb.isKinematic = false;
            player_rb.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 ejectDirection = transform.up;

            player_rb.AddForce(ejectDirection * 2 * player_rb.mass, ForceMode.Impulse);
            player_rb.AddForce(transform.forward * player_rb.mass * speed, ForceMode.Impulse);

        }

        is_in_vehicle = false;

    }

    #endregion

    #region Systems

    protected void AfterBurner()
    {
        float maxAfterburnerSpeed = 100;

        if (Input.GetKey(Settings.Instance._keybinds.JET_boostKey) && moveForward > 0)
        {
            _afterburnerSpeedModifier += Time.deltaTime * 50;
        }
        else
        {
            _afterburnerSpeedModifier -= Time.deltaTime * 50;
        }

        _afterburnerSpeedModifier = Math.Clamp(_afterburnerSpeedModifier, 0, maxAfterburnerSpeed);
    }

    protected void FreeLook()
    {
        if (Input.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey))
        {
            float mouseYFreeLook = Input.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
            float mouseXFreeLook = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

            Vector3 currentEuler = vehicle_camera.transform.localEulerAngles;

            float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
            float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

            currentX += mouseYFreeLook;
            currentY += mouseXFreeLook;

            currentX = Mathf.Clamp(currentX, -80f, 20f);
            currentY = Mathf.Clamp(currentY, -90f, 90f);

            vehicle_camera.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
        else
        {
            vehicle_camera.transform.localRotation = Quaternion.Lerp(
                vehicle_camera.transform.localRotation,
                Quaternion.identity,
                Time.deltaTime * 3
            );
        }
    }

    protected void Zoom()
    {
        if (!vehicle_camera.enabled) return;

        if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key))
        {

            float targetFov = minFov / _properties.zoom;
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

    protected void UpdateLandingGear()
    {
        retractLandingGear = !Physics.Raycast(_core.position, Vector3.down, 10,
            LayerMask.GetMask("Ground") | LayerMask.GetMask("Voxel"));
    }

    protected void ResetSpeedModifiers()
    {
        _diveSpeedModifier = 0;
        _afterburnerSpeedModifier = 0;
    }

    #endregion

    #region Visual & Audio


    protected override void Start_Stop_Engine()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;

            if (start_engine)
            {
                _properties.interior_turbine.Play();
            }

        }
    }

    private bool _wasEnginePlaying = false;

    private void UpdateEngineSound()
    {
        if (vehicle_destroyed.Value) return;

        // Mapeia o throttle (0 a _properties.max_throttle) para o pitch (0.01 a 2)
        float t = throttle / _properties.max_throttle;

        float targetPitch;

        if (start_engine == true)
        {
            targetPitch = Mathf.Lerp(0.4f, 2f, t);
        }
        else
        {
            targetPitch = Mathf.Lerp(0.01f, 2f, t);
        }

        // Suaviza a transição do pitch
        _properties.interior_turbine.pitch = Mathf.Lerp(
            _properties.interior_turbine.pitch,
            targetPitch,
            Time.deltaTime * 2
        );

        // Verifica se deve parar o som
        bool shouldBePlaying = _properties.interior_turbine.pitch > 0.01f;

        // Só chama Stop() se estava tocando e agora não deve mais tocar
        if (_wasEnginePlaying && !shouldBePlaying)
        {
            _properties.interior_turbine.Stop();
        }
        // Só chama Play() se não estava tocando e agora deve tocar
        else if (!_wasEnginePlaying && shouldBePlaying)
        {
            _properties.interior_turbine.Play();
        }

        _wasEnginePlaying = shouldBePlaying;
    }

    #endregion

    #region HUD & UI

    protected override void UpdateHUD()
    {
        _hudManager.UpdateSpeed(speed);
        _hudManager.UpdateAltitude(transform.position.y);

        _hudManager.UpdateGravity(_currentGravity);
        _hudManager.UpdateGforce(_gForce);
        _hudManager.UpdateHeat(_overheatAmount);
        _hudManager.ChangeHeatIndicatorActive(usingMainCannon);
        if (countermeasures != null)
        {
            if (countermeasures.is_active)
            {
                _hudManager.UpdateCountermeasuresStatus("Active");
            }
            else if (!countermeasures.is_active && countermeasures.is_reloading)
            {
                _hudManager.UpdateCountermeasuresStatus("Reloading... [" + countermeasures.reload_countermeasures_duration.ToString("F0") + "]");
            }
            else if (!countermeasures.is_active && !countermeasures.is_reloading)
            {
                _hudManager.UpdateCountermeasuresStatus("Ready");
            }
        }
    }

    #endregion

    #region Helper Methods

    private Volume GetGlobalVolume()
    {
        GameObject globalVolumeObj = GameObject.FindGameObjectWithTag("GlobalVolume");
        if (globalVolumeObj != null)
        {
            return globalVolumeObj.GetComponent<Volume>();
        }
        return null;
    }

    private void HandlePassout()
    {
        // Implementação comentada para revisão futura
    }

    protected override void CameraController()
    {
        Zoom();
        // Implementação de troca de câmera comentada
    }


    bool DestroyAnimation_do_once = true;
    float deltaTime = 0;
    protected override void DestroyAnimation()
    {
        if (DestroyAnimation_do_once)
        {
            CmdRequestEnableFireEffects();
            fire_effects_parent.SetActive(true);
            DestroyAnimation_do_once = false;
        }

        deltaTime += Time.fixedDeltaTime;

        if (playerController != null) playerController.RequestDamage(deltaTime);

        if (deltaTime >= 5)
        {
            Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }

        rb.AddForce(transform.forward * _totalThrottle * _properties.max_throttle);
        rb.AddTorque(transform.right * 400 * rb.mass);
    }

    [ServerRpc]
    private void CmdRequestEnableFireEffects()
    {
        RequestEnableFireEffects();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RequestEnableFireEffects()
    {
        fire_effects_parent.SetActive(true);
    }

    protected override void GetVehicleCustomization()
    {
        throw new NotImplementedException();
    }


    #endregion
}