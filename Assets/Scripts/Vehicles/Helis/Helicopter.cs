using System;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(HeliProperties))]
public abstract class Helicopter : Vehicle, ICurrentRotationUIValues
{
    [Header("----------------------------HELICOPTER SETTINGS----------------------------")]
    [Space(5)]

    #region Inspector Variables
    [Header("Sounds")]
    [SerializeField] protected SoundManager.SoundComponents insidePropellerSound;
    [SerializeField] protected SoundManager.SoundComponents outsidePropellerSound;
    [SerializeField] protected SoundManager.SoundComponents fallAlarmSound;

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
    private const float PROPELLER_ACCELARATION = 2f;
    private const float PROPELLER_DESELERATION = 2f;

    private float localThrottle;

    // Engine sound tracking
    private bool _wasEnginePlaying = false;
    private float currentPitch = 0f;
    #endregion

    #region State Implementations
    protected override void HandleEngineOn()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            HandleThrottleInput(deltaTime);
            CalculateRotationInput(deltaTime);
            ApplyRotationTorque();
            rb.AddForce(liftDirection * Throttle * rb.mass);
        }
        
    }
    protected override void HandleEngineOff()
    {
        base.HandleEngineOff();
        localThrottle = 0f;
    }
    protected override void OnDestructionPhysicsTick(float timer)
    {
        float rotate_value = Math.Clamp(Mathf.Pow(timer * 15, 2f), 0, 900);

        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000, collisionLayers))
        {
            if (hit.distance >= 5) rb.AddTorque(transform.up * rotate_value * rb.mass);
            else Explode(hit.point, hit.normal, hit.transform.gameObject.layer, 12);
            
        }
    }
    #endregion

    #region Movement Physics

    protected override void HandleEmptyVehicle()
    {
        localThrottle = 0f;
        base.HandleEmptyVehicle();
    }
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
            localThrottle += deltaTime * heliProperties.lift_force;
            gravity_force = 100f;
        }
        else if (move_upwards < 0)
        {
            localThrottle -= deltaTime * heliProperties.lift_force;
            gravity_force = 100f;
        }
        else
        {
            localThrottle -= deltaTime * 2;
            gravity_force = 100f;
        }

        localThrottle = Mathf.Clamp(localThrottle, 0, heliProperties.max_lift_force);

        SetThrottle(localThrottle);

        AddForceDown(gravity_force);
    }

    protected void CalculateRotationInput(float deltaTime)
    {
        mouseX = Math.Clamp(InputManager.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility, -heliProperties.max_rotation_value, heliProperties.max_rotation_value);
        mouseY = Math.Clamp(InputManager.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility, -heliProperties.max_pitch_value, heliProperties.max_pitch_value);

        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_pitch_up_key)) mouseY = heliProperties.max_pitch_value;
        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_pitch_down_key)) mouseY = -heliProperties.max_pitch_value;

        if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_lean_left_key)) lean_value -= heliProperties.lean_value * deltaTime;
        else if (InputManager.GetKey(Settings.Instance._keybinds.HELICOPTER_lean_right_key)) lean_value += heliProperties.lean_value * deltaTime;
        else lean_value = 0;

        lean_value = Mathf.Clamp(lean_value, -heliProperties.max_lean_value, heliProperties.max_lean_value);
        if (Settings.Instance._controls.invert_vertical_heli_mouse) mouseY *= -1;
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(transform.forward * -mouseX * heliProperties.rotation_value * (rb.mass / 10));
        rb.AddTorque(transform.right * -mouseY * heliProperties.pitch_value * rb.mass);
        if (lean_value != 0) rb.AddTorque(transform.up * lean_value * rb.mass);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPropellerRotation() => CmdPropellerRotation();

    [ObserversRpc()]
    private void CmdPropellerRotation() => PropellerRotation();

    protected void PropellerRotation()
    {
        float targetSpeed = startEngine.Value && !vehicle_destroyed.Value ? heliProperties.max_lift_force / 4 : 0f;
        float smoothTime = startEngine.Value ? PROPELLER_ACCELARATION : PROPELLER_DESELERATION;
        float t = Mathf.Clamp01(Time.fixedDeltaTime / smoothTime);

        currentPropellerSpeed = Mathf.Lerp(currentPropellerSpeed, targetSpeed, t);
        float rotationAmount = currentPropellerSpeed * Time.fixedDeltaTime * 20;

        if (main_propeller != null) main_propeller.transform.Rotate(0, rotationAmount, 0, Space.Self);
        if (back_propeller != null) back_propeller.transform.Rotate(0, 0, rotationAmount, Space.Self);
    }
    #endregion

    #region Engine Audio & System
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        PropellerRotation();
    }
    protected override void Update()
    {
        base.Update();
        UpdatePropellerSound();
    }
    private void UpdatePropellerSound()
    {
        if (vehicle_destroyed.Value) return;

        // 1. Todos os clients calculam o alvo e suavizam o pitch localmente
        float targetPitch = startEngine.Value ? Mathf.Lerp(0.4f, 1.2f, Throttle / heliProperties.max_lift_force) : 0f;
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 5);

        bool shouldBePlaying = currentPitch > 0.01f;

        // Atualiza o pitch do AudioSource para quem já estiver tocando
        if (shouldBePlaying)
            SoundManager.SetLoopSoundPitchLocal(insidePropellerSound.clip, transform, currentPitch);

        // 2. Apenas o Owner envia comandos de Ligar/Desligar via rede para evitar duplicatas de RPC
        if (IsOwner)
        {
            if (_wasEnginePlaying && !shouldBePlaying)
            {
                SoundManager.Instance.RequestStop3dLoopSound(insidePropellerSound.clip.name, transform);
                _wasEnginePlaying = false;
            }
            else if (!_wasEnginePlaying && shouldBePlaying)
            {
                SoundManager.Instance.RequestPlay3dLoopSound(insidePropellerSound.clip.name, insidePropellerSound.properties, transform, true);
                _wasEnginePlaying = true;
            }
        }
    }

    protected override void StartStopEngine()
    {
        if (InputManager.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey) && IsOwner)
        {
            bool targetState = !startEngine.Value;
            startEngine.Value = targetState; // Atualiza a predição localmente
            CmdSetEngineState(targetState);  // Envia o estado explícito para evitar dessincronização
        }
    }

    [ServerRpc]
    private void CmdSetEngineState(bool state) => startEngine.Value = state;
    

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (vehicle_destroyed.Value && IsInLayerMask(collision.gameObject.layer, collisionLayers)) SoundManager.Play2dSoundLocal(fallAlarmSound.clip, fallAlarmSound.properties);
    }

    public override float GetCurrentThrottle() => Throttle;
    public override float GetMinFov() => Settings.Instance._video.helicopter_fov;
    public override float GetMaxThrottle() => heliProperties.max_lift_force;
    protected override float ClampThrottle(float value) => Mathf.Clamp(value, 0f, heliProperties.max_lift_force);
    public float GetXRotation() => transform.eulerAngles.x;
    public float GetYRotation() => transform.eulerAngles.y;
    public float GetZRotation() => transform.eulerAngles.z;
    #endregion
}
