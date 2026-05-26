using System;
using FishNet.Object;
using UnityEngine;

public class Jet : Vehicle
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private Transform eject_position;
    [SerializeField] private GameObject _mainCannon;
    [SerializeField] private GameObject _trails;
    [SerializeField] private Transform _core;
    [SerializeField] private Transform _shootPosition;
    [SerializeField] private GameObject _turbineSmoke;
    [SerializeField] private JetProperties _properties;

    [Header("Sound")]
    [SerializeField] private AudioSource _stopShooting;
    [SerializeField] private AudioSource _tinnitusAudio;
    [SerializeField] private AudioSource _bulletHitAudio;
    #endregion

    #region Public Fields
    [Header("State")]
    [HideInInspector] public bool isNearGround;
    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public float moveForward;
    [HideInInspector] public bool retractLandingGear = false;
    [HideInInspector] public bool isWheelTouchingGround = true;
    [HideInInspector] public float _overheatAmount;
    [HideInInspector] public float leanValue;
    #endregion

    #region Private Fields
    private JetMainCannon mainCannon;
    private bool _isPassingOut;
    private float _diveSpeedModifier;
    private float _afterburnerSpeedModifier;
    private float _totalThrottle;
    private float _exitCooldown;
    private float _currentGravity = 0;
    private float _downwardComponent;
    private float _gForce;
    public static float maxSpeed = 700;

    // Flags de controle
    private bool _wasEnginePlaying = false;
    private bool _destroyAnimationDoOnce = true;
    private float _destructionDeltaTime = 0;
    #endregion

    #region Unity Lifecycle
    public override void OnStartClient()
    {
        base.OnStartClient();
        SetHpProperties(_properties.hp, _properties.resistance);
        _turbineSmoke.SetActive(false);
        mainCannon = vehicleSeats[0].vehicleArmory[0].GetComponent<JetMainCannon>();
    }

    protected override void FixedUpdate()
    {
        //if (!IsOwner) return;

        if (!vehicle_destroyed.Value)
        {
            HandleFlightPhysics();
        }
        else
        {
            DestroyAnimation();
        }
    }

    protected override void Update()
    {
        if (!IsOwner) return;

        UpdateBasicState();

        if (is_in_vehicle)
        {
            //SyncPlayerPosition();
            HandleInVehicleLogic();
        }
        else
        {
            SlowDownEngine();
        }

        UpdateWeaponSpread();
        UpdateEngineSound();
    }

    void LateUpdate()
    {
        if(is_in_vehicle) SyncPlayerPosition();
    }

    protected void OnCollisionStay(Collision collision)
    {
        if (vehicle_destroyed.Value && IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            ContactPoint contact = collision.contacts[0];
            Explode(contact.point, contact.normal, collision.gameObject.layer, 12);
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (isWheelTouchingGround || collision.gameObject.GetComponent<Missiles>() != null) return;

        if (vehicle_destroyed.Value || hp.Value <= 0)
        {
            if (currentSeat.playerController != null) currentSeat.playerController.RequestDamage(1000);
            ExitVehicle();
            HandleCollision(collision, 50);
            Explode(collision.contacts[0].point, transform.localEulerAngles.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }

        if (speed < 300)
        {
            base.OnCollisionEnter(collision);
        }
        else
        {
            print(collision.gameObject.name);
            HandleCollision(collision, 50);
            Explode(collision.contacts[0].point, transform.localEulerAngles.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }
    }
    #endregion

    #region Main Update & Physics Loops
    protected void HandleFlightPhysics()
    {
        bool canFly = start_engine && is_in_vehicle && !SettingsHUD.Instance.is_menu_settings_active && currentSeat.seatType == VehicleSeats.SeatType.Pilot;

        if (canFly)
        {
            Lean();
            Move();
            Rotate();
            ApplyDiveSpeedBoost();
        }

        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    private void ApplyForwardPropulsion()
    {
        rb.AddForce(Physics.gravity * _currentGravity, ForceMode.Acceleration);

        if (speed < maxSpeed)
        {
            _totalThrottle = throttle + _diveSpeedModifier + _afterburnerSpeedModifier;
            rb.AddForce(transform.forward * _totalThrottle * _properties.max_throttle);
        }
    }

    private void UpdateBasicState()
    {
        speed = rb.linearVelocity.magnitude;
    }
    private void HandleInVehicleLogic()
    {
        if (SettingsHUD.Instance.is_menu_settings_active) return;

        minFov = Settings.Instance._video.jet_fov;

        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_switchSeatKey)) SwitchSeats();

        HandleDebugInput();
        PilotBehaviour();
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            RequestDamage(100);
        }
    }

    private void UpdateWeaponSpread()
    {

    }
    #endregion

    #region Engine & Movement
    protected void PilotBehaviour()
    {
        _exitCooldown += Time.deltaTime;

        HandleFlightInputs();

        if (!vehicle_destroyed.Value)
        {
            StartStopEngine();
            Shoot();
            SwitchWeapon();
        }

        ManageEngineState();
    }

    private void HandleFlightInputs()
    {
        ThrottleInput();
        CameraController();
        FreeLook();
        CalculateGForce();

        if (_properties.can_afterburner) AfterBurner();

        HandleExitInput();
    }

    private void ManageEngineState()
    {
        if (start_engine)
        {
            HandleLeanInput();
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
        bool speedUp = Input.GetKey(Settings.Instance._keybinds.JET_speedUpKey);
        bool speedDown = Input.GetKey(Settings.Instance._keybinds.JET_speedDownKey);

        if (speedUp && !speedDown) moveForward = 1;
        else if (speedDown && !speedUp) moveForward = -1;
    }

    protected void SlowDownEngine()
    {
        ResetSpeedModifiers();
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
        }
        else
        {
            SlowDownEngine();
        }
    }

    protected void HandleThrottleControl(float deltaTime)
    {
        if (moveForward > 0 && !_isPassingOut) IncreaseThrottle(deltaTime);
        else if (moveForward < 0 && !_isPassingOut) DecreaseThrottle(deltaTime);
        else Decelerate(deltaTime);
    }

    protected void IncreaseThrottle(float deltaTime)
    {
        throttle += _properties.aceleration * deltaTime;
        throttle = Mathf.Min(throttle, _properties.max_throttle);
    }

    protected void DecreaseThrottle(float deltaTime)
    {
        float multiplier = isNearGround ? 2f : 1f;
        float limit = isNearGround ? -50f : 100f;

        if (throttle > limit)
        {
            throttle -= _properties.aceleration * deltaTime * multiplier;
        }
    }

    protected void Decelerate(float deltaTime)
    {
        float decelerationRate = isNearGround ? _properties.aceleration * 0.8f : 1f;
        throttle = Mathf.MoveTowards(throttle, 0, decelerationRate * deltaTime);
    }

    protected void Rotate()
    {
        if (_isPassingOut) return;

        ProcessMouseRotationInput();
        HandlePitchKeys();

        if (_properties.invertY) mouseY *= -1;

        ApplyDragFromPitch();
        UpdateTrails();
        ApplyRotationTorque();
    }

    private void ProcessMouseRotationInput()
    {
        mouseX = Math.Clamp(Input.GetAxis("Mouse X") * Settings.Instance._controls.jet_sensibility, -_properties.max_rotation_value, _properties.max_rotation_value);
        mouseY = Math.Clamp(Input.GetAxis("Mouse Y") * Settings.Instance._controls.jet_sensibility, -_properties.max_pitch_value, _properties.max_pitch_value);
    }

    private void ApplyDragFromPitch()
    {
        if (Math.Abs(mouseY) > 1 && throttle > 0 && !isNearGround)
        {
            throttle -= Math.Abs(mouseY) * Time.fixedDeltaTime * 10;
        }
    }

    protected void HandlePitchKeys()
    {
        if (Input.GetKey(Settings.Instance._keybinds.JET_pitchUpKey)) mouseY = _properties.max_pitch_value;
        if (Input.GetKey(Settings.Instance._keybinds.JET_pitchDownKey)) mouseY = -_properties.max_pitch_value;
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(-transform.forward * mouseX * speed * _properties.rotation_value * (rb.mass / 100));
        rb.AddTorque(-transform.right * mouseY * speed * _properties.pitch_value * (rb.mass / 200));
    }

    protected void Lean()
    {
        if (_isPassingOut) return;

        float speedFactor = Mathf.Clamp01(speed / maxSpeed);
        bool canLean = Mathf.Abs(rb.angularVelocity.y) < _properties.max_lean_speed;

        if (!canLean) return;

        float forceMultiplier = (isNearGround && (throttle >= 20 || throttle < -10) && throttle <= 50) ? 70f : 5f * speedFactor;
        float turnForce = leanValue * _properties.lean_value * rb.mass * forceMultiplier;

        rb.AddTorque(transform.up * turnForce, ForceMode.Force);
    }

    protected void HandleLeanInput()
    {
        if (Input.GetKey(Settings.Instance._keybinds.JET_yawLeftKey)) leanValue = -1;
        else if (Input.GetKey(Settings.Instance._keybinds.JET_yawRightKey)) leanValue = 1;
        else leanValue = 0;
    }
    #endregion

    #region Combat
    protected virtual void Shoot()
    {
        if (currentSeat.currentArmory != null) currentSeat.currentArmory.Shoot();
    }
    #endregion

    #region Player Interaction
    // SOBRESCREVE APENAS A LÓGICA LOCAL (Sem tags de rede)
    protected override void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        // 1. Roda a base (Agora vai funcionar 100% sem o FishNet interferir!)
        base.OnVehicleEntered(seatIndex, _player);

        // 2. Travas de segurança para garantir que a base rodou certinho
        if (currentSeat == null || currentSeat.playerController == null)
        {
            Debug.LogError("Erro: Referências do Player não foram preenchidas pela classe base no Jet.");
            return;
        }

        // 3. Continua a lógica exclusiva do Jato
        _exitCooldown = 0;
        SyncPlayerPosition();
        _turbineSmoke.SetActive(start_engine);
    }

    protected void HandleExitInput()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && _exitCooldown > 0.1f)
        {
            _turbineSmoke.SetActive(false);

            if (throttle > 10) EjectPlayer();
            else ExitVehicle();
        }
    }

    protected void EjectPlayer()
    {
        if (!is_in_vehicle) return;

        Rigidbody playerRb = currentSeat.playerRigidbody;
        GameObject player = currentSeat.playerGameObject;
        currentSeat.ExitSeat();

        if (player != null)
        {
            player.transform.position = eject_position.position;

        }

        ApplyEjectForce(playerRb);
  
    }

    private void ApplyEjectForce(Rigidbody player_rb)
    {
        if (player_rb != null)
        {
            player_rb.isKinematic = false;
            player_rb.interpolation = RigidbodyInterpolation.Interpolate;

            player_rb.AddForce(transform.up * 2 * player_rb.mass, ForceMode.Impulse);
            player_rb.AddForce(transform.forward * player_rb.mass * speed, ForceMode.Impulse);
        }
    }
    #endregion

    #region Flight Physics Calculations
    protected void ApplyDiveSpeedBoost()
    {
        _downwardComponent = transform.forward.y;

        if (_downwardComponent > 0.3f) ApplyDiveBoost();
        else if (_downwardComponent < -0.3f) ApplyClimbPenalty();
        else _diveSpeedModifier = Mathf.Lerp(_diveSpeedModifier, 0, 2 * Time.fixedDeltaTime);
    }

    protected void ApplyDiveBoost()
    {
        float gravityBoost = _downwardComponent * Physics.gravity.magnitude * 0.5f;
        float aerodynamicBoost = _downwardComponent * _properties.dive_speed_boost;
        float totalBoost = Mathf.Clamp((gravityBoost + aerodynamicBoost) * Time.fixedDeltaTime, 0, _properties.max_throttle * 1.2f);

        _diveSpeedModifier = totalBoost * 400 * Time.fixedDeltaTime;
    }

    protected void ApplyClimbPenalty()
    {
        float upwardIntensity = -_downwardComponent;
        float airResistance = upwardIntensity * Physics.gravity.magnitude * 0.3f;
        float gravityPenalty = upwardIntensity * _properties.dive_speed_boost * 0.5f;
        float totalPenalty = Mathf.Clamp((airResistance + gravityPenalty) * Time.fixedDeltaTime, 0, _properties.max_throttle * 0.7f);

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

        if (_downwardComponent > 0.3f)
        {
            targetGravity = (moveForward > 0 ? (_properties.max_throttle / (speed * 2)) : (_properties.max_throttle / speed)) * -_downwardComponent;
        }
        else if (_downwardComponent < -0.3f)
        {
            targetGravity = (moveForward > 0 ? 1.5f : (_properties.max_throttle / speed)) * -_downwardComponent;
        }
        else
        {
            if (moveForward > 0) targetGravity = 0;
            else if (throttle < 100) targetGravity = _properties.max_throttle / (speed * 10);
        }

        _currentGravity = Mathf.Clamp(Mathf.Lerp(_currentGravity, targetGravity, Time.fixedDeltaTime), 0f, 5f);
    }

    protected void CalculateGForce()
    {
        float deltaTime = Time.deltaTime;

        if (mouseY != 0) _gForce = (deltaTime / 3) * speed * mouseY;
        else _gForce = Mathf.MoveTowards(_gForce, 0f, deltaTime * 5);

        _gForce = Math.Clamp(_gForce, -10, 10);
    }
    #endregion

    #region Visual, Audio & HUD
    protected void UpdateTrails()
    {
        bool shouldEmit = (mouseX > 10 || mouseX < -10 || mouseY > 10 || mouseY < -10) && speed > 100;

        foreach (TrailRenderer trail in _trails.GetComponentsInChildren<TrailRenderer>())
        {
            if (shouldEmit && !trail.emitting)
            {
                trail.Clear();
                trail.emitting = true;
            }
            else if (!shouldEmit && trail.emitting)
            {
                trail.emitting = false;
            }
        }
    }

    protected override void StartStopEngine()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            if (start_engine) _properties.interior_turbine.Play();
        }
    }

    private void UpdateEngineSound()
    {
        if (vehicle_destroyed.Value) return;

        float t = throttle / _properties.max_throttle;
        float targetPitch = start_engine ? Mathf.Lerp(0.4f, 2f, t) : Mathf.Lerp(0.01f, 2f, t);

        _properties.interior_turbine.pitch = Mathf.Lerp(_properties.interior_turbine.pitch, targetPitch, Time.deltaTime * 2);

        bool shouldBePlaying = _properties.interior_turbine.pitch > 0.01f;

        if (_wasEnginePlaying && !shouldBePlaying) _properties.interior_turbine.Stop();
        else if (!_wasEnginePlaying && shouldBePlaying) _properties.interior_turbine.Play();

        _wasEnginePlaying = shouldBePlaying;
    }
    #endregion

    #region Camera & Systems
    protected void FreeLook()
    {
        if (Input.GetKey(Settings.Instance._keybinds.VEHICLE_freeLookKey))
        {
            float mouseYFreeLook = Input.GetAxis("Mouse Y") * -Settings.Instance._controls.helicopter_sensibility;
            float mouseXFreeLook = Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility;

            Vector3 currentEuler = currentSeat.playerCamera.transform.localEulerAngles;
            float currentX = (currentEuler.x > 180) ? currentEuler.x - 360 : currentEuler.x;
            float currentY = (currentEuler.y > 180) ? currentEuler.y - 360 : currentEuler.y;

            currentX = Mathf.Clamp(currentX + mouseYFreeLook, -80f, 20f);
            currentY = Mathf.Clamp(currentY + mouseXFreeLook, -90f, 90f);

            currentSeat.playerCamera.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
        else
        {
            currentSeat.playerCamera.transform.localRotation = Quaternion.Lerp(currentSeat.playerCamera.transform.localRotation, Quaternion.identity, Time.deltaTime * 3);
        }
    }

    protected void Zoom()
    {
        /*
        if (!currentSeat.playerCamera.enabled) return;

        float targetFov = Input.GetKey(Settings.Instance._keybinds.HELICOPTER_zoom_key) ? (minFov / mainCannon.zoom) : minFov;
        currentSeat.playerCamera.fieldOfView = Mathf.Lerp(currentSeat.playerCamera.fieldOfView, targetFov, 4 * Time.deltaTime);
        */
    }

    protected override void CameraController()
    {
        if (currentSeat.playerCamera == null) return;
        Zoom();
    }

    protected void AfterBurner()
    {
        if (Input.GetKey(Settings.Instance._keybinds.JET_boostKey) && moveForward > 0)
            _afterburnerSpeedModifier += Time.deltaTime * 50;
        else
            _afterburnerSpeedModifier -= Time.deltaTime * 50;

        _afterburnerSpeedModifier = Math.Clamp(_afterburnerSpeedModifier, 0, 100);
    }

    protected void UpdateLandingGear()
    {
        retractLandingGear = !Physics.Raycast(_core.position, Vector3.down, 10, LayerMask.GetMask("Ground", "Voxel"));
    }

    protected void ResetSpeedModifiers()
    {
        _diveSpeedModifier = 0;
        _afterburnerSpeedModifier = 0;
    }

    #endregion

    #region Destruction & Helpers
    protected override void DestroyAnimation()
    {
        if (_destroyAnimationDoOnce)
        {
            CmdRequestEnableFireEffects();
            fire_effects_parent.SetActive(true);
            _destroyAnimationDoOnce = false;
        }

        _destructionDeltaTime += Time.fixedDeltaTime;

        if (currentSeat.playerController != null) currentSeat.playerController.RequestDamage(_destructionDeltaTime);

        if (_destructionDeltaTime >= 5)
        {
            Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }

        //rb.AddForce(transform.forward * _totalThrottle * _properties.max_throttle);
        rb.AddTorque(transform.forward * 400 * rb.mass);
    }

    [ServerRpc] private void CmdRequestEnableFireEffects() => RequestEnableFireEffects();
    [ObserversRpc(ExcludeOwner = true)] private void RequestEnableFireEffects() => fire_effects_parent.SetActive(true);

    #endregion

    #region Interfaces Overrides
    //ICurrentSpeedUIValues
    public override float GetMaxSpeed() => maxSpeed;
    //ICurrentThrottleUIValues
    public override float GetMaxThrottle() => _properties.max_throttle;
    #endregion

}