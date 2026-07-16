using System;
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
    [SerializeField] private GameObject turret;
    [SerializeField] protected Light[] lights;
    
    [Header("Wheels")]
    [SerializeField] public WheelCollider[] leftWheels;
    [SerializeField] public WheelCollider[] rightWheels;
    [SerializeField] public Transform[] leftWheelsTransform;
    [SerializeField] public Transform[] rightWheelsTransform;

    [Header("Audio")]
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource secondaryCannonSound;

    [Header("Guns")]
    [SerializeField] private Transform tankCannon;

    [Header("Settings")]
    public float maxRotationUp = 45f;
    public float maxRotationDown = 45f;
    #endregion

    #region Public & Private Fields
    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public int moveForward;
    [HideInInspector] public int moveSideways;
    [HideInInspector] public float gunnerGunOverheatAmount;

    private float _boostMaxThrottle;
    private float _boostMaxSpeed;
    private float _currentMaxSpeed;
    private float _currentAcceleration;
    private float _boostAcceleration;
    private bool _isBoosting;
    private float _cannonRotationAmount;

    // Movement smoothing
    private float _currentSpeedLerp = 0f;
    private float _currentThrottleLerp = 0f;
    private float _currentAccelerationLerp = 0f;
    private float _currentRotationForce = 0f;
    private float _targetRotationForce = 0f;
    private bool _appliedSpeedRotation;
    private bool _appliedBreakRotation;
    private int _previousMoveForward;

    [Header("Boost Transition Settings")]
    [SerializeField] private float boostTransitionSpeed = 5f;
    [SerializeField] private AnimationCurve boostCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    #endregion

    #region Unity Lifecycle & Initialization
    public override void OnStartClient()
    {
        base.OnStartClient();

        InitializeClientItems();
        SetHpProperties(tankProperties.hp, tankProperties.resistance);
    }

    private void InitializeClientItems()
    {
        _currentMaxSpeed = tankProperties.max_speed;
        _currentAcceleration = tankProperties.acceleration;

        _boostMaxThrottle = tankProperties.max_throttle * tankProperties.boost_force;
        _boostMaxSpeed = tankProperties.max_speed * tankProperties.boost_force;
        _boostAcceleration = tankProperties.acceleration * tankProperties.boost_force;
    }
    #endregion

    #region State Implementations
    protected override void HandleEngineOn()
    {
        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            Boost();
            ThrottleInput();
            RotateInput();
            Move();
            RotateTurret();
            RotateCannon();
        }
        
        WheelsController();
        AddForceDown();
    }

    protected override void HandleEngineOff()
    {
        base.HandleEngineOff();
        WheelsControllerVisualsOnly();
    }

    protected override void HandleEmptyVehicle()
    {
        base.HandleEmptyVehicle();
        WheelsControllerVisualsOnly();
    }

    protected override void OnDestructionPhysicsTick(float timer)
    {
        // Tanques são pesados e geralmente apenas queimam e param de funcionar, sem física mirabolante.
        WheelsControllerVisualsOnly();
    }
    #endregion

    #region Movement & Physics
    protected void Move()
    {
        if (moveForward != 0)
        {
            float moveForce = moveForward * _currentAcceleration * rb.mass;
            rb.AddForce(transform.forward * moveForce, ForceMode.Force);

            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (flatVel.magnitude > _currentMaxSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _currentMaxSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

        if (moveSideways != 0)
        {
            if (Mathf.Abs(rb.angularVelocity.y) < tankProperties.max_rotation_speed)
            {
                float turnForce = moveSideways * tankProperties.rotation_value * rb.mass * 50f;
                rb.AddTorque(transform.up * turnForce, ForceMode.Force);
            }
        }
    }

    private void Boost()
    {
        if (!tankProperties.can_boost) return;

        bool shouldBoost;

        if (Settings.Instance._controls.is_vehicle_boost_on_hold)
        {
            shouldBoost = InputManager.GetKey(Settings.Instance._keybinds.TANK_boostKey) && moveForward > 0;
        }
        else
        {
            if (InputManager.GetKeyDown(Settings.Instance._keybinds.TANK_boostKey))
                _isBoosting = !_isBoosting;

            if (moveForward <= 0) _isBoosting = false;
            
            shouldBoost = _isBoosting;
        }

        float deltaTimeMultiplier = boostTransitionSpeed * Time.deltaTime;
        float boostFactor = shouldBoost ? 1f : 0f;

        _currentSpeedLerp = Mathf.MoveTowards(_currentSpeedLerp, boostFactor, deltaTimeMultiplier);
        _currentThrottleLerp = Mathf.MoveTowards(_currentThrottleLerp, boostFactor, deltaTimeMultiplier);
        _currentAccelerationLerp = Mathf.MoveTowards(_currentAccelerationLerp, boostFactor, deltaTimeMultiplier);

        _currentMaxSpeed = Mathf.Lerp(tankProperties.max_speed, _boostMaxSpeed, boostCurve.Evaluate(_currentSpeedLerp));
        _currentAcceleration = Mathf.Lerp(tankProperties.acceleration, _boostAcceleration, boostCurve.Evaluate(_currentAccelerationLerp));
    }

    private void ThrottleInput()
    {
        _targetRotationForce = 0f;

        if (InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle))
        {
            _appliedBreakRotation = false;
            moveForward = 1;
            if (!_appliedSpeedRotation)
            {
                _appliedSpeedRotation = true;
                _targetRotationForce = -4000f;
            }
        }
        else if (InputManager.GetKey(Settings.Instance._keybinds.TANK_decrease_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.TANK_increase_throtlle))
        {
            _appliedSpeedRotation = false;
            moveForward = -1;
            if (!_appliedBreakRotation)
            {
                _appliedBreakRotation = true;
                _targetRotationForce = 4000f;
            }
        }
        else
        {
            moveForward = 0;
            _targetRotationForce = 0f;
            _appliedBreakRotation = false;
            _appliedSpeedRotation = false;
        }

        _currentRotationForce = Mathf.Lerp(_currentRotationForce, _targetRotationForce, 10f * Time.deltaTime);

        if (speed < 2f || (moveForward != _previousMoveForward && moveForward != 0))
        {
            rb.AddTorque(transform.right * _currentRotationForce * rb.mass);
        }

        if (moveForward != 0) _previousMoveForward = moveForward;
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
        if (moveForward == 0 && moveSideways == 0)
        {
            WheelsControllerVisualsOnly();
            return;
        }

        float baseTorque = _currentAcceleration * Time.deltaTime;
        float maxTorque = tankProperties.max_throttle;

        float leftTorque = moveForward * baseTorque;
        float rightTorque = moveForward * baseTorque;

        // Cálculo dinâmico para substituir as 9 condicionais gigantes
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

        leftTorque = Mathf.Clamp(leftTorque, -maxTorque, maxTorque);
        rightTorque = Mathf.Clamp(rightTorque, -maxTorque, maxTorque);

        ApplyTorqueToWheels(leftWheels, leftWheelsTransform, leftTorque);
        ApplyTorqueToWheels(rightWheels, rightWheelsTransform, rightTorque);
    }

    private void WheelsControllerVisualsOnly()
    {
        ApplyTorqueToWheels(leftWheels, leftWheelsTransform, 0f);
        ApplyTorqueToWheels(rightWheels, rightWheelsTransform, 0f);
    }

    private void ApplyTorqueToWheels(WheelCollider[] colliders, Transform[] visuals, float torque)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (torque != 0f) colliders[i].motorTorque = torque;
            
            colliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            visuals[i].position = pos;
            visuals[i].rotation = rot;
        }
    }
    #endregion

    #region Turret & Cannon
    private void RotateTurret()
    {
        mouseX = Math.Clamp(InputManager.GetAxis("Mouse X") * Settings.Instance._controls.tank_sensibility,
                           -tankProperties.turret_max_rotation_value, tankProperties.turret_max_rotation_value);

        turret.transform.Rotate(Vector3.up * mouseX * tankProperties.turret_rotation_value / 20f);
    }

    private void RotateCannon()
    {
        float mouseInput = Math.Clamp(InputManager.GetAxisRaw("Mouse Y") * Settings.Instance._controls.tank_sensibility,
                           -tankProperties.turret_max_rotation_value, tankProperties.turret_max_rotation_value);

        float rotationAmount = -mouseInput * tankProperties.turret_rotation_value / 20f;
        Vector3 currentRotation = tankCannon.transform.localEulerAngles;

        float currentX = currentRotation.x > 180f ? currentRotation.x - 360f : currentRotation.x;

        _cannonRotationAmount = Mathf.Clamp(currentX + rotationAmount, -maxRotationUp, maxRotationDown);

        tankCannon.transform.localEulerAngles = new Vector3(_cannonRotationAmount, currentRotation.y, currentRotation.z);
    }
    #endregion

    #region Entry / Exit & Visibility
    protected override void OnVehicleEntered(int seatIndex, GameObject _player)
    {
        base.OnVehicleEntered(seatIndex, _player);
        // Desabilita o modelo do jogador via rede para quem está de fora ver que ele "entrou" no tanque
        CmdSetPlayerVisibility(_player, false);
    }

    protected override void ExitVehicle()
    {
        if (currentSeat != null && currentSeat.playerGameObject != null)
        {
            CmdSetPlayerVisibility(currentSeat.playerGameObject, true);
        }
        base.ExitVehicle();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdSetPlayerVisibility(GameObject player, bool isVisible) => RpcSetPlayerVisibility(player, isVisible);

    [ObserversRpc]
    private void RpcSetPlayerVisibility(GameObject player, bool isVisible)
    {
        if (player != null) player.SetActive(isVisible);
    }
    #endregion

    #region Systems
    public override float GetMinFov() => Settings.Instance._video.tank_fov;

    protected override void StartStopEngine()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            startEngine.Value = !startEngine.Value;
            foreach (Light light in lights) light.enabled = startEngine.Value;
        }
    }
    #endregion
}