using FishNet.Object;
using UnityEngine;

public class Tank : Vehicle
{
    [Header("----------------------------TANK SETTINGS----------------------------")]
    [Space(5)]

    #region Inspector Variables
    [Header("Properties")]
    [SerializeField] private TankProperties tankProperties;

    [Header("Instances")]
    [SerializeField] protected Light[] lights;
    
    [Header("Wheels")]
    [SerializeField] public WheelCollider[] leftWheels;
    [SerializeField] public WheelCollider[] rightWheels;
    [SerializeField] public Transform[] leftWheelsTransform;
    [SerializeField] public Transform[] rightWheelsTransform;
    #endregion

    #region Public & Private Fields
    [HideInInspector] public int moveForward;
    [HideInInspector] public int moveSideways;
    private float _currentEnginePitch;
    private AudioSource _engineAudioSource;

    #endregion

    #region Unity Lifecycle & Initialization
    protected override void Awake()
    {
        base.Awake();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    protected override void Update()
    {
        base.Update();
        UpdateEngineSound();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        UpdateWheelVisuals(leftWheels, leftWheelsTransform);
        UpdateWheelVisuals(rightWheels, rightWheelsTransform);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        SetHpProperties(tankProperties.hp, tankProperties.resistance);
        CreateEngineAudioSource();
    }

    #endregion

    #region State Implementations
    protected override void HandleEngineOn()
    {
        if (currentSeat != null && currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            ThrottleInput();
            RotateInput();
            Move();
        }
        else
        {
            moveForward = 0;
            moveSideways = 0;
            SetThrottle(0f);
        }
        
        WheelsController();
        AddForceDown();
    }

    protected override void HandleEngineOff()
    {
        base.HandleEngineOff();
        moveForward = 0;
        moveSideways = 0;
        StopWheels();
    }

    protected override void HandleEmptyVehicle()
    {
        base.HandleEmptyVehicle();
        moveForward = 0;
        moveSideways = 0;
        StopWheels();
    }

    protected override void OnDestructionPhysicsTick(float timer) => StopWheels();
    #endregion

    #region Movement & Physics
    protected void Move()
    {
        float targetForwardSpeed = Throttle;
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedError = targetForwardSpeed - currentForwardSpeed;
        rb.AddForce(transform.forward * speedError * 2f, ForceMode.Acceleration);

        float targetYawSpeed = moveSideways * Mathf.Max(0f, tankProperties.max_rotation_speed);
        float currentYawSpeed = Vector3.Dot(rb.angularVelocity, transform.up);
        // The existing rotation value controls how quickly yaw reaches the target speed.
        float response = Mathf.Clamp01(Mathf.Max(0f, tankProperties.rotation_value) * 0.1f * Time.fixedDeltaTime);
        float yawAcceleration = (targetYawSpeed - currentYawSpeed) * response / Time.fixedDeltaTime;
        rb.AddTorque(transform.up * yawAcceleration, ForceMode.Acceleration);
    }

    private void ThrottleInput()
    {
        if (InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle))
        {
            moveForward = 1;
        }
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle))
        {
            moveForward = -1;
        }
        else
        {
            moveForward = 0;
        }

        float targetThrottle = moveForward * Mathf.Max(0f, tankProperties.max_throttle);
        SetThrottle(Mathf.MoveTowards(BaseThrottle, targetThrottle,
            Mathf.Max(0f, tankProperties.acceleration) * Time.fixedDeltaTime));
    }

    private void RotateInput()
    {
        moveSideways = 0;

        if (InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_left_key) && InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_right_key))
            moveSideways = 0;
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_right_key))
            moveSideways = 1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_turn_left_key))
            moveSideways = -1;
    }
    #endregion

    #region Wheels Controller
    private void WheelsController()
    {
        float baseTorque = Mathf.Max(0f, tankProperties.wheelMotorTorque);
        float speedError = Throttle - Vector3.Dot(rb.linearVelocity, transform.forward);

        float driveTorque = Mathf.Clamp(speedError, -1f, 1f) * baseTorque;
        float leftTorque = driveTorque;
        float rightTorque = driveTorque;

        if (moveForward == 0 && moveSideways != 0)
        {
            leftTorque = moveSideways * baseTorque;
            rightTorque = -moveSideways * baseTorque;
        }
        else if (moveForward != 0 && moveSideways != 0)
        {
            if (moveSideways > 0) rightTorque /= 2f;
            else leftTorque /= 2f;
        }

        leftTorque = Mathf.Clamp(leftTorque, -baseTorque, baseTorque);
        rightTorque = Mathf.Clamp(rightTorque, -baseTorque, baseTorque);

        ApplyTorqueToWheels(leftWheels, leftTorque);
        ApplyTorqueToWheels(rightWheels, rightTorque);
    }

    private void StopWheels()
    {
        ApplyTorqueToWheels(leftWheels, 0f);
        ApplyTorqueToWheels(rightWheels, 0f);
    }

    private void ApplyTorqueToWheels(WheelCollider[] colliders, float torque)
    {
        if (colliders == null) return;
        foreach (WheelCollider collider in colliders)
            if (collider != null) collider.motorTorque = torque;
    }

    private void UpdateWheelVisuals(WheelCollider[] colliders, Transform[] visuals)
    {
        if (colliders == null || visuals == null) return;
        for (int i = 0; i < Mathf.Min(colliders.Length, visuals.Length); i++)
        {
            if (colliders[i] == null || visuals[i] == null) continue;
            colliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            visuals[i].SetPositionAndRotation(pos, rot);
        }
    }
    #endregion

    #region Systems
    public override float GetMinFov() => Settings.Instance._video.tank_fov;
    protected override bool CanApplyBoost() =>
        InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle) &&
        !InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle);

    private void CreateEngineAudioSource()
    {
        if (_engineAudioSource != null || tankProperties.engineSound?.clip == null) return;

        GameObject audioObject = new GameObject("Tank Engine Audio");
        audioObject.transform.SetParent(transform, false);
        _engineAudioSource = SoundManager.CreateConfiguredAudioSource(
            audioObject, tankProperties.engineSound.clip, tankProperties.engineSound.properties, true, true);
    }

    private void UpdateEngineSound()
    {
        if (!IsClientInitialized) return;
        if (_engineAudioSource == null) CreateEngineAudioSource();
        if (_engineAudioSource == null) return;

        bool engineRunning = startEngine.Value && !vehicle_destroyed.Value;
        float throttleLoad = Mathf.Clamp01(Mathf.Abs(Throttle) /
                                            Mathf.Max(0.01f, tankProperties.max_throttle));
        float turnLoad = Mathf.Clamp01(Mathf.Abs(Vector3.Dot(rb.angularVelocity, transform.up)) / Mathf.Max(0.01f, tankProperties.max_rotation_speed));
        float engineLoad = Mathf.Max(throttleLoad, turnLoad);
        float targetPitch = engineRunning
            ? Mathf.Lerp(tankProperties.minEnginePitch,
                tankProperties.maxEnginePitch + boostModifier / 300f, engineLoad)
            : 0f;
        _currentEnginePitch = Mathf.MoveTowards(_currentEnginePitch, targetPitch, Time.deltaTime);

        bool shouldBePlaying = _currentEnginePitch > 0.01f;
        if (shouldBePlaying)
        {
            _engineAudioSource.pitch = Mathf.Max(0.1f, _currentEnginePitch);
            if (!_engineAudioSource.isPlaying) _engineAudioSource.Play();
        }
        else if (_engineAudioSource.isPlaying) _engineAudioSource.Stop();
    }

    protected override void StartStopEngine()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey) && IsOwner)
        {
            bool engineOn = !startEngine.Value;
            startEngine.Value = engineOn;
            CmdSetEngineState(engineOn);
            foreach (Light light in lights) light.enabled = startEngine.Value;
        }
    }

    [ServerRpc]
    private void CmdSetEngineState(bool engineOn) => startEngine.Value = engineOn;

    public override float GetMaxSpeed() => Mathf.Max(0f, tankProperties.max_throttle) * MovementMultiplier;
    public override float GetMaxThrottle() => Mathf.Max(0f, tankProperties.max_throttle);
    protected override float ClampThrottle(float value) => Mathf.Clamp(value,
        -Mathf.Max(0f, tankProperties.max_throttle), Mathf.Max(0f, tankProperties.max_throttle));
    protected override float ClampBoostedThrottle(float value) => Mathf.Clamp(value,
        -Mathf.Max(0f, tankProperties.max_throttle),
        Mathf.Max(0f, tankProperties.max_throttle) * Mathf.Max(1f, boostMultiplier));

    protected override void UpdateAnimator(){}
    #endregion
}
