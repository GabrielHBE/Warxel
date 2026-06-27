using System;
using FishNet.Object;
using UnityEngine;

public abstract class Helicopter : Vehicle, ICurrentRotationUIValues
{
    [Header("----------------------------HELICOPTER SETTINGS----------------------------")]
    [Space(5)]

    #region Inspector Variables
    [Header("Sounds")]
    [SerializeField] protected AudioClip insidePropellerSound;
    [SerializeField] protected SoundManager.SoundProperties insidePropellerSoundProperties = SoundManager.SoundProperties.Default;
    [SerializeField] protected AudioClip outsidePropellerSound;
    [SerializeField] protected SoundManager.SoundProperties outsidePropellerSoundProperties = SoundManager.SoundProperties.Default;
    [SerializeField] protected AudioClip fallAlarmSound;
    [SerializeField] protected SoundManager.SoundProperties fallAlarmSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Helicopter variables")]
    [SerializeField] protected HeliProperties heliProperties;
    [SerializeField] protected GameObject main_propeller;
    [SerializeField] protected GameObject back_propeller;
    #endregion

    #region Private Variables
    protected float mouseY;
    protected float mouseX;
    private float move_upwards;
    private float gravity_force;
    private float lean_value;
    protected Vector3 liftDirection;
    private float currentPropellerSpeed = 0f;
    private float propellerAccelerationTime = 10f;
    private float propellerDecelerationTime = 1f;
    #endregion

    #region State Implementations
    protected override void HandleEngineOn()
    {
        float deltaTime = Time.fixedDeltaTime;
        PropellerRotation();

        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            HandleThrottleInput(deltaTime);
            CalculateRotationInput(deltaTime);
            ApplyRotationTorque();
            rb.AddForce(liftDirection * throttle * rb.mass);
        }
        else
        {
            throttle = 0;
            gravity_force = 0.2f;
            AddForceDown(gravity_force);
        }
    }

    protected override void HandleEngineOff()
    {
        base.HandleEngineOff();
        PropellerRotation();
    }

    protected override void HandleEmptyVehicle()
    {
        base.HandleEmptyVehicle();
        PropellerRotation();
    }

    protected override void OnDestructionPhysicsTick(float timer)
    {
        float rotate_value = Math.Clamp(Mathf.Pow(timer * 15, 2f), 0, 900);

        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000, collisionLayers))
        {
            if (hit.distance >= 5)
            {
                rb.AddTorque(transform.up * rotate_value * rb.mass);
            }
            else
            {
                Explode(hit.point, hit.normal, hit.transform.gameObject.layer, 12);
            }
        }
    }
    #endregion

    #region Movement Physics
    protected void HandleThrottleInput(float deltaTime)
    {
        move_upwards = 0;
        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_increase_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_decrease_throtlle))
            move_upwards = 1;
        else if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_decrease_throtlle) && !InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_increase_throtlle))
            move_upwards = -1;

        float pitchAngle = transform.eulerAngles.x > 180f ? transform.eulerAngles.x - 360f : transform.eulerAngles.x;
        float rollAngle = transform.eulerAngles.z > 180f ? transform.eulerAngles.z - 360f : transform.eulerAngles.z;

        float absPitchAngle = Mathf.Abs(pitchAngle);
        float absrollAngle = Mathf.Abs(rollAngle);

        liftDirection = transform.up;

        if (absPitchAngle >= 10f && absPitchAngle <= 50 && absrollAngle >= -20 && absrollAngle <= 20)
        {
            float forwardRatio = (absPitchAngle - 15f) / 30f;
            float pitchDirection = Mathf.Sign(pitchAngle);
            liftDirection = (transform.up * (1f - forwardRatio)) + (transform.forward * forwardRatio * pitchDirection);
            liftDirection.Normalize();
        }

        if (transform.position.y > MapSettings.Instance.max_altitude) move_upwards = -1;

        if (move_upwards > 0)
        {
            throttle += deltaTime * heliProperties.lift_force;
            gravity_force = 100f;
        }
        else if (move_upwards < 0)
        {
            throttle -= deltaTime * heliProperties.lift_force * 2;
            gravity_force = 100f;
        }
        else
        {
            throttle -= deltaTime * heliProperties.lift_force;
            gravity_force = 100f;
        }

        AddForceDown(gravity_force);
        throttle = Mathf.Clamp(throttle, 0, heliProperties.max_lift_force);
    }

    protected void CalculateRotationInput(float deltaTime)
    {
        mouseX = Math.Clamp(InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility, -heliProperties.max_rotation_value, heliProperties.max_rotation_value);
        mouseY = Math.Clamp(InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility, -heliProperties.max_pitch_value, heliProperties.max_pitch_value);

        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_pitch_up_key)) mouseY = heliProperties.max_pitch_value;
        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_pitch_down_key)) mouseY = -heliProperties.max_pitch_value;

        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_lean_left_key))
            lean_value -= heliProperties.lean_value * deltaTime;
        else if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_lean_right_key))
            lean_value += heliProperties.lean_value * deltaTime;
        else
            lean_value = 0;

        lean_value = Mathf.Clamp(lean_value, -heliProperties.max_lean_value, heliProperties.max_lean_value);
        if (Settings.Instance._controls.invert_vertical_heli_mouse) mouseY *= -1;
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(transform.forward * -mouseX * heliProperties.rotation_value * (rb.mass / 10));
        rb.AddTorque(transform.right * -mouseY * heliProperties.pitch_value * rb.mass);
        if (lean_value != 0) rb.AddTorque(transform.up * lean_value * rb.mass);
    }

    protected void PropellerRotation()
    {
        float targetSpeed = start_engine && !vehicle_destroyed.Value ? heliProperties.max_lift_force : 0f;
        float smoothTime = start_engine ? propellerAccelerationTime : propellerDecelerationTime;
        float t = Mathf.Clamp01(Time.fixedDeltaTime / smoothTime);

        currentPropellerSpeed = Mathf.Lerp(currentPropellerSpeed, targetSpeed, t);
        float rotationAmount = currentPropellerSpeed * Time.fixedDeltaTime * 20;

        if (main_propeller != null) main_propeller.transform.Rotate(0, rotationAmount * 4, 0, Space.Self);
        if (back_propeller != null) back_propeller.transform.Rotate(0, 0, rotationAmount * 4, Space.Self);
    }
    #endregion

    #region Engine Audio & System
    protected override void StartStopEngine()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            if (start_engine)
                SoundManager.Instance.RequestPlay3dLoopSound(insidePropellerSound.name, insidePropellerSoundProperties, transform, true);
            else
                SoundManager.Instance.RequestPause3dLoopSound(insidePropellerSound.name, transform);
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (vehicle_destroyed.Value && IsInLayerMask(collision.gameObject.layer, collisionLayers))
        {
            SoundManager.Play2dSoundLocal(fallAlarmSound, fallAlarmSoundProperties);
        }
    }

    public override float GetMinFov() => Settings.Instance._video.helicopter_fov;
    public override float GetMaxThrottle() => heliProperties.max_lift_force;
    public float GetXRotation() => transform.eulerAngles.x;
    public float GetYRotation() => transform.eulerAngles.y;
    public float GetZRotation() => transform.eulerAngles.z;
    #endregion
}