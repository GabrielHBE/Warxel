using System;
using FishNet.Object;
using UnityEngine;

public class Helicopter : Vehicle, ICurrentRotationUIValues
{
    #region Inspector Variables

    [Header("Helicopter variables")]
    [SerializeField] protected HeliProperties heliProperties;
    [SerializeField] protected GameObject main_propeller;
    [SerializeField] protected GameObject back_propeller;
    [SerializeField] protected AudioSource inside_propeller_sound;
    [SerializeField] protected AudioSource outside_propeller_sound;
    [SerializeField] protected AudioSource fall_alarm;

    #endregion

    #region Private Variables
    protected float mouseY;
    protected float mouseX;

    // Movement
    private float gravity_force;
    private float move_upwards;

    // Rotation
    private float lean_value;
    private float rotate_value;
    private float destroyTimer = 0f;
    protected Vector3 liftDirection;
    private float currentPropellerSpeed = 0f;
    private float propellerAccelerationTime = 10f;
    private float propellerDecelerationTime = 1f;
    protected float exit_cooldown;
    #endregion

    #region Movement 

    protected void HandleThrottleControls()
    {
        move_upwards = 0;

        if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_increase_throtlle) && Input.GetKey(Settings.Instance._keybinds.HELICOPTER_decrease_throtlle))
        {
            move_upwards = 0;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_increase_throtlle))
        {
            move_upwards = 1;
        }
        else if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_decrease_throtlle))
        {
            move_upwards = -1;
        }
    }

    protected override void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (currentSeat.seatType == VehicleSeats.SeatType.Pilot)
        {
            
            HandleThrottleInput(deltaTime);
        }
        else
        {
            throttle = 0;
            gravity_force = 5;
        }

        rb.AddForce(Vector3.down * gravity_force, ForceMode.Acceleration);
        throttle = Mathf.Clamp(throttle, 0, heliProperties.max_lift_force);
    }

    protected void HandleThrottleInput(float deltaTime)
    {
        print("to no HandleThrottleInput");
        
        float pitchAngle = transform.eulerAngles.x;
        float rollAngle = transform.eulerAngles.z;

        //float upsideDownFactor = Mathf.Clamp01(1f - Vector3.Dot(transform.up, Vector3.up));

        if (pitchAngle > 180f)
            pitchAngle -= 360f;

        if (rollAngle > 180f)
            rollAngle -= 360f;

        float absPitchAngle = Mathf.Abs(pitchAngle);
        float absrollAngle = Mathf.Abs(rollAngle);

        liftDirection = transform.up;


        if (absPitchAngle >= 10f && absPitchAngle <= 50 && absrollAngle >= -20 && absrollAngle <= 20)
        {
            float forwardRatio = (absPitchAngle - 15f) / 30f;

            float pitchDirection = Mathf.Sign(pitchAngle);

            liftDirection = (transform.up * (1f - forwardRatio)) +
                           (transform.forward * forwardRatio * pitchDirection);


            liftDirection.Normalize();
        }

        if (vehicle_destroyed.Value || transform.position.y > MapSettings.Instance.max_altitude) move_upwards = -1;

        if (move_upwards > 0)
        {
            throttle += deltaTime * heliProperties.lift_force;

            //rb.AddForce(liftDirection * throttle, ForceMode.Acceleration);

            gravity_force = 5;
        }
        else if (move_upwards < 0)
        {
            throttle -= deltaTime * heliProperties.lift_force * 2;
            gravity_force = 50;

        }
        else
        {

            gravity_force = 50;
            throttle -= deltaTime * heliProperties.lift_force / 2;

        }
    }

    protected void Rotate()
    {
        float deltaTime = Time.fixedDeltaTime;
        CalculateRotationInput(deltaTime);
        ApplyRotationTorque();

    }

    protected void CalculateRotationInput(float deltaTime)
    {
        mouseX = Math.Clamp(Input.GetAxis("Mouse X") * Settings.Instance._controls.helicopter_sensibility, -heliProperties.max_rotation_value, heliProperties.max_rotation_value);
        mouseY = Math.Clamp(Input.GetAxis("Mouse Y") * Settings.Instance._controls.helicopter_sensibility, -heliProperties.max_pitch_value, heliProperties.max_pitch_value);

        if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_pitch_up_key))
            mouseY = heliProperties.max_pitch_value;
        if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_pitch_down_key))
            mouseY = -heliProperties.max_pitch_value;

        HandleLeanInput(deltaTime);

        if (Settings.Instance._controls.invert_vertical_heli_mouse)
        {
            mouseY *= -1;
        }
    }

    protected void HandleLeanInput(float deltaTime)
    {
        if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_lean_left_key))
        {
            lean_value -= heliProperties.lean_value * deltaTime;
            lean_value = Mathf.Clamp(lean_value, -heliProperties.max_lean_value, heliProperties.max_lean_value);
        }
        else if (Input.GetKey(Settings.Instance._keybinds.HELICOPTER_lean_right_key))
        {
            lean_value += heliProperties.lean_value * deltaTime;
            lean_value = Mathf.Clamp(lean_value, -heliProperties.max_lean_value, heliProperties.max_lean_value);
        }
        else
        {
            lean_value = 0;
        }
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(transform.forward * -mouseX * heliProperties.rotation_value * (rb.mass / 10), ForceMode.Force);
        rb.AddTorque(transform.right * -mouseY * heliProperties.pitch_value * rb.mass, ForceMode.Force);
        if (lean_value != 0) rb.AddTorque(transform.up * lean_value * rb.mass);
    }

    #endregion

    #region Utility 
    protected void PropellerRotation()
    {
        float deltaTime = Time.deltaTime;
        float targetSpeed = start_engine == true ? heliProperties.max_lift_force : 0f;

        float smoothTime = start_engine == true ? propellerAccelerationTime : propellerDecelerationTime;
        float t = Mathf.Clamp01(deltaTime / smoothTime);

        currentPropellerSpeed = Mathf.Lerp(currentPropellerSpeed, targetSpeed, t);

        float rotationAmount = currentPropellerSpeed * deltaTime * 20;

        if (main_propeller != null)
            main_propeller.transform.Rotate(0, rotationAmount * 4, 0, Space.Self);
        if (back_propeller != null)
            back_propeller.transform.Rotate(0, 0, rotationAmount * 4, Space.Self);
    }
    #endregion

    #region Collision & Destruction 

    protected override void OnCollisionEnter(Collision collision)
    {
        if (!vehicle_destroyed.Value)
        {
            base.OnCollisionEnter(collision);
        }
        else
        {
            if (!IsInLayerMask(collision.gameObject.layer, collisionLayers))
            {
                return;
            }

            // Pegando o ponto de contato e normal da colisão
            ContactPoint contact = collision.contacts[0]; // Primeiro ponto de contato
            Vector3 contactPoint = contact.point; // Ponto da colisão
            Vector3 contactNormal = contact.normal; // Normal da colisão

            if (currentSeat.playerController != null) currentSeat.playerController.RequestDamage(100);

            // Usando os valores obtidos da colisão
            HandleCollision(collision, 50);
            Explode(contactPoint, contactNormal, collision.gameObject.layer, 12);
        }
    }

    bool DestroyAnimation_do_once = true;
    protected override void DestroyAnimation()
    {
        // 1. Visual effects can be triggered locally by everyone when the SyncVar becomes true
        if (DestroyAnimation_do_once)
        {
            CmdRequestEnableFireEffects(); // Ensure Server sends RPC

            DestroyAnimation_do_once = false;
        }

        // 2. CRITICAL CHANGE: Only the server handles the timer, physics torque, and the final explosion
        //if (!IsServerInitialized) return;

        destroyTimer += Time.fixedDeltaTime;
        rotate_value = Math.Clamp(Mathf.Pow(destroyTimer * 15, 2f), 0, 900);

        if (currentSeat.playerController != null) currentSeat.playerController.RequestDamage(destroyTimer);

        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, collisionLayers))
        {
            if (hit.distance >= 5)
            {
                if (!fall_alarm.isPlaying)
                {
                    fall_alarm.Play(); // Consider making this an ObserversRpc if you want everyone to hear it
                }

                rb.AddTorque(transform.up * rotate_value * rb.mass);
            }
            else
            {
                Explode(hit.point, hit.normal, hit.transform.gameObject.layer, 12);
            }
        }

        if (destroyTimer >= 5)
        {
            Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }
    }
    #endregion

    [ServerRpc]
    private void CmdRequestEnableFireEffects()
    {
        RequestEnableFireEffects();
    }

    [ObserversRpc]
    private void RequestEnableFireEffects()
    {
        fire_effects_parent.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    protected void CmdRequestPlayEngineSound()
    {
        RpcPlayEngineSound();
    }

    [ObserversRpc]
    private void RpcPlayEngineSound()
    {
        inside_propeller_sound.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    protected void CmdRequestStopEngineSound()
    {
        RpcStopEngineSound();
    }

    [ObserversRpc]
    private void RpcStopEngineSound()
    {
        inside_propeller_sound.Stop();
    }


    protected override void StartStopEngine()
    {
        if (Input.GetKeyDown(Settings.Instance._keybinds.VEHICLE_startEngineKey))
        {
            start_engine = !start_engine;
            if (start_engine == true)
            {
                CmdRequestPlayEngineSound();
            }
            else
            {
                CmdRequestStopEngineSound();
            }
        }
    }

    protected override void CameraController() { }

    #region Interface Implementations
    public override float GetMaxThrottle() => heliProperties.max_lift_force;

    public float GetXRotation() => transform.eulerAngles.x;
    public float GetYRotation() => transform.eulerAngles.y;
    public float GetZRotation() => transform.eulerAngles.z;
    #endregion

}