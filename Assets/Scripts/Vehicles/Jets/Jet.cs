using System;
using UnityEngine;

public class Jet : Vehicle
{
    [Header("--------------------------JET VEHICLE SETTINGS--------------------------")]
    [Space(5)]

    [Header("Jet References")]
    [SerializeField] private Transform eject_position;
    [SerializeField] private GameObject _mainCannon;
    [SerializeField] private GameObject _trails;
    [SerializeField] private Transform _core;
    [SerializeField] private Transform _shootPosition;
    [SerializeField] private GameObject _turbineSmoke;
    [SerializeField] private JetProperties _properties;

    [HideInInspector] public bool isNearGround;
    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public float moveForward;
    [HideInInspector] public bool retractLandingGear = false;
    [HideInInspector] public bool isWheelTouchingGround = true;
    [HideInInspector] public float _overheatAmount;
    [HideInInspector] public float leanValue;

    private float _diveSpeedModifier;
    private float _afterburnerSpeedModifier;
    private float _totalThrottle;
    private float _currentGravity = 0;
    private float _downwardComponent;
    private float _gForce;
    public static float maxSpeed = 700;

    private bool _wasEnginePlaying = false;
    private float _currentPitch = 0f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        SetHpProperties(_properties.hp, _properties.resistance);
        _turbineSmoke.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
        UpdateEngineSound();
    }

    #region State Implementations
    protected override void HandleEngineOn()
    {
        _turbineSmoke.SetActive(true);
        UpdateLandingGear();
        
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            HandleFlightInputs();
            Lean();
            Move();
            Rotate();
            ApplyDiveSpeedBoost();
        }

        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    protected override void HandleEngineOff()
    {
        base.HandleEngineOff();
        _turbineSmoke.SetActive(false);
        SlowDownEngine();
        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    protected override void HandleEmptyVehicle()
    {
        base.HandleEmptyVehicle();
        _turbineSmoke.SetActive(false);
        SlowDownEngine();
        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    protected override void OnDestructionPhysicsTick(float timer)
    {
        rb.AddTorque(transform.forward * 400 * rb.mass);
    }
    #endregion

    #region Flight Input & Physics
    private void HandleFlightInputs()
    {
        moveForward = 0;
        if (InputManager.GetKey(Settings.Instance._keybinds.JET_speedUpKey)) moveForward = 1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.JET_speedDownKey)) moveForward = -1;

        if (InputManager.GetKey(Settings.Instance._keybinds.JET_yawLeftKey)) leanValue = -1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.JET_yawRightKey)) leanValue = 1;
        else leanValue = 0;

        if (_properties.can_afterburner) AfterBurner();
        CalculateGForce();
    }

    private void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (isNearGround && mouseY > 0 && speed > 50)
            rb.AddForce(Vector3.up * rb.mass * 20);

        if (transform.position.y < MapSettings.Instance.max_altitude)
        {
            if (moveForward > 0)
            {
                throttle += _properties.aceleration * deltaTime;
                throttle = Mathf.Min(throttle, _properties.max_throttle);
            }
            else if (moveForward < 0)
            {
                float limit = isNearGround ? -50f : 100f;
                if (throttle > limit) throttle -= _properties.aceleration * deltaTime * (isNearGround ? 2f : 1f);
            }
            else
            {
                throttle = Mathf.MoveTowards(throttle, 0, (isNearGround ? _properties.aceleration * 0.8f : 1f) * deltaTime);
            }
        }
        else
        {
            SlowDownEngine();
        }
    }

    private void Rotate()
    {
        mouseX = Math.Clamp(InputManager.GetAxis("Mouse X") * Settings.Instance._controls.jet_sensibility, -_properties.max_rotation_value, _properties.max_rotation_value);
        mouseY = Math.Clamp(InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.jet_sensibility, -_properties.max_pitch_value, _properties.max_pitch_value);

        if (InputManager.GetKey(Settings.Instance._keybinds.JET_pitchUpKey)) mouseY = _properties.max_pitch_value;
        if (InputManager.GetKey(Settings.Instance._keybinds.JET_pitchDownKey)) mouseY = -_properties.max_pitch_value;

        if (Settings.Instance._controls.invert_vertical_jet_mouse) mouseY *= -1;

        if (Math.Abs(mouseY) > 1 && throttle > 0 && !isNearGround)
            throttle -= Math.Abs(mouseY) * Time.fixedDeltaTime * 10;

        UpdateTrails();
        rb.AddTorque(-transform.forward * mouseX * speed * _properties.rotation_value * (rb.mass / 100));
        rb.AddTorque(-transform.right * mouseY * speed * _properties.pitch_value * (rb.mass / 200));
    }

    private void Lean()
    {
        float speedFactor = Mathf.Clamp01(speed / maxSpeed);
        if (Mathf.Abs(rb.angularVelocity.y) >= _properties.max_lean_speed) return;

        float forceMultiplier = (isNearGround && (throttle >= 20 || throttle < -10) && throttle <= 50) ? 70 : speedFactor;
        rb.AddTorque(transform.up * leanValue * _properties.lean_value * rb.mass * forceMultiplier);
    }

    private void ApplyForwardPropulsion()
    {
        rb.AddForce(Physics.gravity * _currentGravity * rb.mass);
        if (speed < maxSpeed)
        {
            _totalThrottle = throttle + _diveSpeedModifier + _afterburnerSpeedModifier;
            rb.AddForce(transform.forward * _totalThrottle * _properties.max_throttle);
        }
    }
    #endregion

    #region Dynamics Math
    private void ApplyDiveSpeedBoost()
    {
        _downwardComponent = transform.forward.y;
        if (_downwardComponent > 0.3f)
        {
            float totalBoost = Mathf.Clamp((_downwardComponent * Physics.gravity.magnitude * 0.5f + _downwardComponent * _properties.dive_speed_boost) * Time.fixedDeltaTime, 0, _properties.max_throttle * 1.2f);
            _diveSpeedModifier = totalBoost * 400 * Time.fixedDeltaTime;
        }
        else if (_downwardComponent < -0.3f)
        {
            float upwardIntensity = -_downwardComponent;
            float totalPenalty = Mathf.Clamp((upwardIntensity * Physics.gravity.magnitude * 0.3f + upwardIntensity * _properties.dive_speed_boost * 0.5f) * Time.fixedDeltaTime, 0, _properties.max_throttle * 0.7f);
            _diveSpeedModifier = -totalPenalty * 400 * Time.fixedDeltaTime;
        }
        else
        {
            _diveSpeedModifier = Mathf.Lerp(_diveSpeedModifier, 0, 2 * Time.fixedDeltaTime);
        }
    }

    private void ApplyGravityModifier()
    {
        if (isNearGround) { _currentGravity = 0; return; }

        float targetGravity = 1f;
        if (_downwardComponent > 0.3f) targetGravity = (moveForward > 0 ? (_properties.max_throttle / (speed * 2)) : (_properties.max_throttle / speed)) * -_downwardComponent;
        else if (_downwardComponent < -0.3f) targetGravity = (moveForward > 0 ? 1.5f : (_properties.max_throttle / speed)) * -_downwardComponent;
        else if (moveForward > 0) targetGravity = 0;
        else if (throttle < 100) targetGravity = _properties.max_throttle / (speed * 10);

        _currentGravity = Mathf.Clamp(Mathf.Lerp(_currentGravity, targetGravity, Time.fixedDeltaTime), 0f, 5f);
    }

    private void CalculateGForce()
    {
        _gForce = mouseY != 0 ? (Time.fixedDeltaTime / 3) * speed * mouseY : Mathf.MoveTowards(_gForce, 0f, Time.fixedDeltaTime * 5);
        _gForce = Math.Clamp(_gForce, -10, 10);
    }

    protected void AfterBurner()
    {
        if (InputManager.GetKey(Settings.Instance._keybinds.JET_boostKey) && moveForward > 0) _afterburnerSpeedModifier += Time.fixedDeltaTime * 50;
        else _afterburnerSpeedModifier -= Time.fixedDeltaTime * 50;
        _afterburnerSpeedModifier = Math.Clamp(_afterburnerSpeedModifier, 0, 100);
    }

    private void SlowDownEngine()
    {
        _diveSpeedModifier = 0;
        _afterburnerSpeedModifier = 0;
        throttle = Mathf.Lerp(throttle, 0, Time.fixedDeltaTime / 2);
    }

    protected void UpdateLandingGear()
    {
        retractLandingGear = !Physics.Raycast(_core.position, Vector3.down, 10, LayerMask.GetMask("Ground", "Voxel"));
    }
    #endregion

    #region Ejection & Sounds
    protected override void HandleVehicleInput()
    {
        base.HandleVehicleInput();
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f)
        {
            if (throttle > 10) EjectPlayer();
        }
    }

    protected void EjectPlayer()
    {
        Rigidbody playerRb = currentSeat.playerRigidbody;
        GameObject player = currentSeat.playerGameObject;
        currentSeat.ExitSeat();

        if (player != null) player.transform.position = eject_position.position;

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.interpolation = RigidbodyInterpolation.Interpolate;
            playerRb.AddForce(transform.up * 2 * playerRb.mass, ForceMode.Impulse);
            playerRb.AddForce(transform.forward * playerRb.mass * speed, ForceMode.Impulse);
        }
    }

    protected override void StartStopEngine()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            if (start_engine) SoundManager.Instance.RequestPlay3dLoopSound(_properties.interiorTurbineSound.name, _properties.interiorTurbineSoundProperties, transform, true);
        }
    }

    private void UpdateEngineSound()
    {
        if (vehicle_destroyed.Value) return;
        float targetPitch = start_engine ? Mathf.Lerp(0.4f, 2f, throttle / _properties.max_throttle) : 0f;
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * 2);
        
        bool shouldBePlaying = _currentPitch > 0.01f;
        if (shouldBePlaying) SoundManager.SetLoopSoundPitchLocal(_properties.interiorTurbineSound, transform, _currentPitch);

        if (_wasEnginePlaying && !shouldBePlaying)
        {
            SoundManager.Instance.RequestStop3dLoopSound(_properties.interiorTurbineSound.name, transform);
            _wasEnginePlaying = false;
        }
        else if (!_wasEnginePlaying && shouldBePlaying)
        {
            SoundManager.Instance.RequestPlay3dLoopSound(_properties.interiorTurbineSound.name, _properties.interiorTurbineSoundProperties, transform, true);
            _wasEnginePlaying = true;
        }
    }

    protected void UpdateTrails()
    {
        bool shouldEmit = (mouseX > 10 || mouseX < -10 || mouseY > 10 || mouseY < -10) && speed > 100;
        foreach (TrailRenderer trail in _trails.GetComponentsInChildren<TrailRenderer>())
        {
            if (shouldEmit && !trail.emitting) { trail.Clear(); trail.emitting = true; }
            else if (!shouldEmit && trail.emitting) trail.emitting = false;
        }
    }

    public override float GetMinFov() => Settings.Instance._video.jet_fov;
    public override float GetMaxSpeed() => maxSpeed;
    public override float GetMaxThrottle() => _properties.max_throttle;
    protected override float GetCameraSensitivity() => Settings.Instance._controls.jet_sensibility;
    #endregion
}