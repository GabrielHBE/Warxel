using System;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(JetProperties))]
public class Jet : Vehicle
{
    [Header("--------------------------JET VEHICLE SETTINGS--------------------------")]
    [Space(5)]

    [Header("Jet References")]
    [SerializeField] private Transform eject_position;
    [SerializeField] private GameObject _trails;
    [SerializeField] private GameObject _turbineSmoke;
    [SerializeField] private JetProperties _properties;
    [SerializeField] protected PropellerData[] propellerDatas;


    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public float moveForward;
    [HideInInspector] public float leanValue;

    private float groundCheckDistance;
    private bool isNearGround = true;
    private float _diveSpeedModifier;
    private float _totalThrottle;
    private float _currentGravity = 0;
    private float _downwardComponent;
    private float _gForce;
    private const float MAX_SPEED = 2000;
    private const float PROPELLER_ACCELARATION = 2f;
    private const float PROPELLER_DESELERATION = 2f;
    private const int SONIC_BOON_SPEED = 700;
    private float currentPropellerSpeed = 0f;
    private bool _wasEnginePlaying = false;
    private float _currentPitch = 0f;
    private bool _sonicBoomArmed = true;
    private bool _isThrustVectoring;
    private float _thrustVectorTimeRemaining;
    private float _thrustVectorCooldownRemaining;
    private bool _thrustVectorNeedsRelease;

    private bool IsThrustVectorInputHeld => _properties != null && _properties.canThrustVector &&
        InputManager.GetKey(Settings.Instance._keybinds.JET_speedDownKey) &&
        InputManager.GetKey(Settings.Instance._keybinds.VEHICLE_boost_key);

    protected override void Awake()
    {
        base.Awake();
        SetGroundCheckDistance();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        SetHpProperties(_properties.hp, _properties.resistance);
        if (_turbineSmoke != null) _turbineSmoke.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
        UpdateEngineSound();
    }

    protected override void FixedUpdate()
    {
        if (IsController) UpdateThrustVectoringState();
        base.FixedUpdate();
        if (IsController) CheckSonicBoom();
        PropellerRotation();
    }

    private void UpdateThrustVectoringState()
    {
        _thrustVectorCooldownRemaining = Mathf.Max(0f, _thrustVectorCooldownRemaining - Time.fixedDeltaTime);

        bool inputHeld = IsThrustVectorInputHeld;
        if (!inputHeld) _thrustVectorNeedsRelease = false;

        bool canFly = !vehicle_destroyed.Value && Owner.IsValid && startEngine.Value &&
                      currentSeat != null && currentSeat.seatType == VehicleSeats.SeatType.Pilot;
        float minSpeed = Mathf.Max(0f, _properties.thrustVectorMinSpeed);
        bool hasEnoughSpeed = rb.linearVelocity.magnitude >= minSpeed;

        if (_isThrustVectoring)
        {
            if (!canFly || !inputHeld || !hasEnoughSpeed)
            {
                StopThrustVectoring();
                return;
            }

            _thrustVectorTimeRemaining -= Time.fixedDeltaTime;
            if (_thrustVectorTimeRemaining <= 0f) StopThrustVectoring();
            return;
        }

        if (!canFly || !inputHeld || !hasEnoughSpeed || _thrustVectorNeedsRelease ||
            _thrustVectorCooldownRemaining > 0f) return;

        _isThrustVectoring = true;
        _thrustVectorTimeRemaining = Mathf.Max(0.1f, _properties.thrustVectorMaxDuration);
    }

    private void StopThrustVectoring()
    {
        if (!_isThrustVectoring) return;

        _isThrustVectoring = false;
        _thrustVectorTimeRemaining = 0f;
        _thrustVectorCooldownRemaining = Mathf.Max(0f, _properties.thrustVectorCooldown);
        _thrustVectorNeedsRelease = true;
    }

    private void CheckSonicBoom()
    {
        if (vehicle_destroyed.Value || SONIC_BOON_SPEED <= 0f) return;

        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed < SONIC_BOON_SPEED * 0.95f)
            _sonicBoomArmed = true;

        if (!_sonicBoomArmed || currentSpeed <= SONIC_BOON_SPEED) return;

        _sonicBoomArmed = false;
        Quaternion effectRotation = Quaternion.LookRotation(rb.linearVelocity.normalized, transform.up);
        if (IsServerInitialized) RpcPlaySonicBoom(transform.position, effectRotation);
        else CmdPlaySonicBoom(transform.position, effectRotation);
    }

    [ServerRpc]
    private void CmdPlaySonicBoom(Vector3 position, Quaternion rotation) => RpcPlaySonicBoom(position, rotation);

    [ObserversRpc]
    private void RpcPlaySonicBoom(Vector3 position, Quaternion rotation)
    {
        if (_properties.sonicBoomSound?.clip != null)
        {
            SoundManager.SoundProperties soundProperties = _properties.sonicBoomSound.properties;
            soundProperties.spatialBlend = 1f;
            SoundManager.Play3dSoundLocal(_properties.sonicBoomSound.clip, soundProperties, position);
        }

        if (_properties.sonicBoomEffectPrefab != null)
        {
            GameObject effect = Instantiate(_properties.sonicBoomEffectPrefab, position, rotation);
            Destroy(effect, Mathf.Max(0.1f, _properties.sonicBoomEffectDuration));
        }
    }

    #region State Implementations
    private void SetGroundCheckDistance()
    {
        int groundMask = LayerMask.GetMask("Voxel", "Ground");
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore)) groundCheckDistance = hit.distance * 2;
    }
    protected override void HandleEngineOn()
    {
        if (_turbineSmoke != null) _turbineSmoke.SetActive(true);
        UpdateIsNearGround();
        UpdateAnimator();
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            HandleFlightInputs();
            Lean();
            Move();
            Rotate();
            if (_isThrustVectoring)
            {
                _diveSpeedModifier = 0f;
                ApplyThrustVectoring();
            }
            else ApplyDiveSpeedBoost();
        }

        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    protected override void HandleEngineOff()
    {
        StopThrustVectoring();
        base.HandleEngineOff();
        if (_turbineSmoke != null) _turbineSmoke.SetActive(false);
        SlowDownEngine();
        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    protected override void HandleEmptyVehicle()
    {
        StopThrustVectoring();
        base.HandleEmptyVehicle();
        if (_turbineSmoke != null) _turbineSmoke.SetActive(false);
        SlowDownEngine();
        ApplyGravityModifier();
        ApplyForwardPropulsion();
    }

    protected override void OnDestructionPhysicsTick(float timer) => rb.AddTorque(transform.forward * 400 * rb.mass);

    #endregion

    #region Flight Input & Physics
    private void HandleFlightInputs()
    {
        moveForward = 0;
        if (_isThrustVectoring) moveForward = -1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.JET_speedUpKey)) moveForward = 1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.JET_speedDownKey)) moveForward = -1;

        if (InputManager.GetKey(Settings.Instance._keybinds.JET_yawLeftKey)) leanValue = -1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.JET_yawRightKey)) leanValue = 1;
        else leanValue = 0;

        CalculateGForce();
    }

    private void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (isNearGround && mouseY > 0 && speed > 50) rb.AddForce(Vector3.up * rb.mass * 20);

        if (transform.position.y < MapSettings.Instance.max_altitude)
        {
            if (moveForward > 0)
            {
                SetThrottle(Mathf.Min(BaseThrottle + _properties.aceleration * deltaTime, _properties.max_throttle));
            }
            else if (moveForward < 0)
            {
                float limit = isNearGround ? -50f : 100f;
                if (BaseThrottle > limit) SetThrottle(BaseThrottle - _properties.aceleration * deltaTime * (isNearGround ? 2f : 1f));
            }
            else SetThrottle(Mathf.MoveTowards(BaseThrottle, 0, (isNearGround ? 0.8f : 1f) * deltaTime));

        }
        else SlowDownEngine();

    }

    private void Rotate()
    {
        bool blockMouseRotation = ShouldBlockMouseRotationForFreeLook();
        mouseX = blockMouseRotation ? 0f : Math.Clamp(InputManager.GetAxis("Mouse X") * Settings.Instance._controls.jet_sensibility, -_properties.max_rotation_value, _properties.max_rotation_value);
        mouseY = blockMouseRotation ? 0f : Math.Clamp(InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.jet_sensibility, -_properties.max_pitch_value, _properties.max_pitch_value);

        if (InputManager.GetKey(Settings.Instance._keybinds.JET_pitchUpKey)) mouseY = _properties.max_pitch_value;
        if (InputManager.GetKey(Settings.Instance._keybinds.JET_pitchDownKey)) mouseY = -_properties.max_pitch_value;

        if (Settings.Instance._controls.invert_vertical_jet_mouse) mouseY *= -1;

        if (Math.Abs(mouseY) > 1 && BaseThrottle > 0 && !isNearGround)
            SetThrottle(BaseThrottle - Math.Abs(mouseY) * Time.fixedDeltaTime * 10);

        UpdateTrails();
        if (_isThrustVectoring) return;
        rb.AddTorque(-transform.forward * mouseX * Mathf.Clamp01(_totalThrottle / _properties.max_throttle) * _properties.rotation_value * rb.mass);
        rb.AddTorque(-transform.right * mouseY * Mathf.Clamp01(_totalThrottle / _properties.max_throttle) * _properties.pitch_value * rb.mass);
    }

    private void Lean()
    {
        if (_isThrustVectoring) return;
        float speedFactor = Mathf.Clamp01(speed / MAX_SPEED);
        if (Mathf.Abs(rb.angularVelocity.y) >= _properties.max_lean_speed) return;

        float forceMultiplier = (isNearGround && (Throttle >= 20 || Throttle < -10) && Throttle <= 50) ? 70 : speedFactor;
        rb.AddTorque(transform.up * leanValue * _properties.lean_value * rb.mass * forceMultiplier);
    }

    private void ApplyForwardPropulsion()
    {
        rb.AddForce(Physics.gravity * _currentGravity * rb.mass);
        _totalThrottle = Throttle + _diveSpeedModifier;
        if (_isThrustVectoring)
            _totalThrottle *= Mathf.Clamp01(_properties.thrustVectorForwardThrust);
        if (speed < MAX_SPEED)
        {
            rb.AddForce(transform.forward * _totalThrottle * _properties.max_throttle);
        }
    }

    private void ApplyThrustVectoring()
    {
        float minSpeed = Mathf.Max(0f, _properties.thrustVectorMinSpeed);
        float fullStrengthSpeed = Mathf.Max(minSpeed + 1f, _properties.thrustVectorFullStrengthSpeed);
        float speedStrength = Mathf.Lerp(0.15f, 1f,
            Mathf.InverseLerp(minSpeed, fullStrengthSpeed, rb.linearVelocity.magnitude));
        float rollInput = Mathf.Clamp(mouseX / Mathf.Max(Mathf.Abs(_properties.max_rotation_value), 0.001f), -1f, 1f);
        float pitchInput = Mathf.Clamp(mouseY / Mathf.Max(Mathf.Abs(_properties.max_pitch_value), 0.001f), -1f, 1f);
        if (InputManager.GetKey(Settings.Instance._keybinds.JET_pitchUpKey)) pitchInput = 1f;
        if (InputManager.GetKey(Settings.Instance._keybinds.JET_pitchDownKey)) pitchInput = -1f;
        Vector3 targetAngularVelocity = new Vector3(-pitchInput, leanValue, -rollInput) *
                                        _properties.thrustVectorTurnRate;
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 angularAcceleration = (targetAngularVelocity - localAngularVelocity) *
                                      _properties.thrustVectorResponse;
        angularAcceleration = Vector3.ClampMagnitude(angularAcceleration,
            _properties.thrustVectorAngularAcceleration) * speedStrength;

        rb.AddRelativeTorque(angularAcceleration, ForceMode.Acceleration);
        rb.AddForce(-rb.linearVelocity * _properties.thrustVectorSpeedLoss * speedStrength, ForceMode.Acceleration);
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
        else _diveSpeedModifier = Mathf.Lerp(_diveSpeedModifier, 0, 2 * Time.fixedDeltaTime);

    }

    private void ApplyGravityModifier()
    {
        if (isNearGround) { _currentGravity = 0; return; }

        float targetGravity = 1f;
        if (_downwardComponent > 0.3f) targetGravity = (moveForward > 0 ? (_properties.max_throttle / (speed * 2)) : (_properties.max_throttle / speed)) * -_downwardComponent;
        else if (_downwardComponent < -0.3f) targetGravity = (moveForward > 0 ? 1.5f : (_properties.max_throttle / speed)) * -_downwardComponent;
        else if (moveForward > 0) targetGravity = 0;
        else if (Throttle < 100) targetGravity = _properties.max_throttle / (speed * 10);

        _currentGravity = Mathf.Clamp(Mathf.Lerp(_currentGravity, targetGravity, Time.fixedDeltaTime), 0f, 5f);
    }

    private void CalculateGForce()
    {
        _gForce = mouseY != 0 ? (Time.fixedDeltaTime / 3) * speed * mouseY : Mathf.MoveTowards(_gForce, 0f, Time.fixedDeltaTime * 5);
        _gForce = Math.Clamp(_gForce, -10, 10);
    }

    protected override KeyCode GetBoostKey() => IsThrustVectorInputHeld ? KeyCode.None : base.GetBoostKey();
    protected override bool CanApplyBoost() => !IsThrustVectorInputHeld &&
        InputManager.GetKey(Settings.Instance._keybinds.JET_speedUpKey) && speed < MAX_SPEED;
    private void SlowDownEngine()
    {
        _diveSpeedModifier = 0;
        SetThrottle(Mathf.Lerp(BaseThrottle, 0, Time.fixedDeltaTime / 2));
    }

    protected void UpdateIsNearGround() => isNearGround = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, LayerMask.GetMask("Ground", "Voxel"));

    protected override void UpdateAnimator()
    {
        if (anim == null) return;

        anim.SetBool("AvtivateLandingGear", isNearGround);

    }

    protected void PropellerRotation()
    {
        if (propellerDatas.Length == 0) return;

        float targetSpeed = startEngine.Value && !vehicle_destroyed.Value ? _properties.max_throttle / 4 : 0f;
        float smoothTime = startEngine.Value ? PROPELLER_ACCELARATION : PROPELLER_DESELERATION;
        float t = Mathf.Clamp01(Time.fixedDeltaTime / smoothTime);

        currentPropellerSpeed = Mathf.Lerp(currentPropellerSpeed, targetSpeed, t);
        float rotationAmount = currentPropellerSpeed * Time.fixedDeltaTime * 20;

        if (propellerDatas == null) return;

        foreach (PropellerData propellerData in propellerDatas)
        {
            if (propellerData == null || propellerData.propeler == null) continue;

            Vector3 axis;
            switch (propellerData.rotationAxis)
            {
                case PropellerRotationAxis.X:
                    axis = Vector3.right;
                    break;
                case PropellerRotationAxis.Y:
                    axis = Vector3.up;
                    break;
                case PropellerRotationAxis.Z:
                    axis = Vector3.forward;
                    break;
                default:
                    continue;
            }

            propellerData.propeler.transform.Rotate(axis, rotationAmount, Space.Self);
        }
    }
    #endregion

    #region Ejection & Sounds
    protected override void HandleVehicleInput()
    {
        base.HandleVehicleInput();
        if (!isInVehicle) return;
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.PLAYER_interactKey) && exit_cooldown > 0.1f && Throttle > 10) EjectPlayer();
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
            startEngine.Value = !startEngine.Value;
            if (startEngine.Value) SoundManager.Instance.RequestPlay3dLoopSound(_properties.interiorTurbineSound.clip.name, _properties.interiorTurbineSound.properties, transform, true);
        }
    }

    private void UpdateEngineSound()
    {
        if (vehicle_destroyed.Value) return;
        float targetPitch = startEngine.Value ? Mathf.Lerp(_properties.minTurbinePitch, _properties.maxTurbinePitch + (boostModifier / 300), Throttle / _properties.max_throttle) : 0f;
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * 2);

        bool shouldBePlaying = _currentPitch > 0.01f;
        if (shouldBePlaying) SoundManager.SetLoopSoundPitchLocal(_properties.interiorTurbineSound.clip, transform, _currentPitch);

        if (_wasEnginePlaying && !shouldBePlaying)
        {
            SoundManager.Instance.RequestStop3dLoopSound(_properties.interiorTurbineSound.clip.name, transform);
            _wasEnginePlaying = false;
        }
        else if (!_wasEnginePlaying && shouldBePlaying)
        {
            SoundManager.Instance.RequestPlay3dLoopSound(_properties.interiorTurbineSound.clip.name, _properties.interiorTurbineSound.properties, transform, true);
            _wasEnginePlaying = true;
        }
    }

    protected void UpdateTrails()
    {
        if (_trails == null) return;

        bool shouldEmit = (mouseX > 10 || mouseX < -10 || mouseY > 10 || mouseY < -10) && speed > 100;
        foreach (TrailRenderer trail in _trails.GetComponentsInChildren<TrailRenderer>())
        {
            if (shouldEmit && !trail.emitting) { trail.Clear(); trail.emitting = true; }
            else if (!shouldEmit && trail.emitting) trail.emitting = false;
        }
    }

    public override float GetCurrentThrottle() => _totalThrottle;
    public override float GetMinFov() => Settings.Instance._video.jet_fov;
    public override float GetMaxSpeed() => MAX_SPEED;
    public override float GetMaxThrottle() => _properties.max_throttle;
    protected override float ClampThrottle(float value) => Mathf.Clamp(value, -50f, _properties.max_throttle);
    protected override float GetBoostedThrottle(float value) => value > 0f ? value + boostModifier : value;
    protected override float ClampBoostedThrottle(float value) => Mathf.Clamp(value, -50f, _properties.max_throttle + 150f);
    protected override float GetCameraSensitivity() => Settings.Instance._controls.jet_sensibility;
    #endregion
}
