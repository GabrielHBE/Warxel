using System;
using UnityEngine;

public class Helicopter : Vehicle
{
    #region Inspector Variables

    [Header("Helicopter variables")]
    [SerializeField] protected Transform pilot_position;

    [SerializeField] protected HeliProperties heliProperties;
    [SerializeField] protected GameObject main_propeller;
    [SerializeField] protected GameObject back_propeller;

    [SerializeField] protected AudioSource inside_propeller_sound;
    [SerializeField] protected AudioSource outside_propeller_sound;
    [SerializeField] protected AudioSource fall_alarm;

    [HideInInspector] public bool is_pilot_seat_occupied = false;
    [HideInInspector] public bool is_gunner_seat_occupied = false;

    protected Transform player_camera;

    [Space]
    [Space]

    #endregion

    #region Private Variables
    [SerializeField] protected HelicopterHudManager helicopterHudManager;

    protected float mouseY;
    protected float mouseX;
    protected bool is_pilot;

    // Movement
    private float gravity_force;
    private float move_upwards;

    // Rotation
    private float lean_value;
    private float rotate_value;
    private float destroyTimer = 0f;
    protected Vector3 liftDirection;

    #endregion


    #region Movement 

    protected void HandleThrottleControls()
    {
        move_upwards = 0;

        if (Input.GetKey(keyBinds.HELICOPTER_increase_throtlle) && Input.GetKey(keyBinds.HELICOPTER_decrease_throtlle))
        {
            move_upwards = 0;
        }
        else if (Input.GetKey(keyBinds.HELICOPTER_increase_throtlle))
        {
            move_upwards = 1;
        }
        else if (Input.GetKey(keyBinds.HELICOPTER_decrease_throtlle))
        {
            move_upwards = -1;
        }
    }


    protected override void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (!is_pilot_seat_occupied)
        {
            throttle = 0;
            gravity_force = 5;
        }
        else
        {
            HandleThrottleInput(deltaTime);
        }

        rb.AddForce(Vector3.down * gravity_force, ForceMode.Acceleration);
        throttle = Mathf.Clamp(throttle, 0, heliProperties.max_lift_force);
    }

    protected void HandleThrottleInput(float deltaTime)
    {
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

        if (vehicle_destroyed) move_upwards = -1;

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
        if (!is_pilot) return;

        float deltaTime = Time.fixedDeltaTime;
        CalculateRotationInput(deltaTime);
        ApplyRotationTorque();


    }

    protected void CalculateRotationInput(float deltaTime)
    {
        mouseX = Math.Clamp(Input.GetAxis("Mouse X") * controls.helicopter_sensibility, -heliProperties.max_rotation_value, heliProperties.max_rotation_value);
        mouseY = Math.Clamp(Input.GetAxis("Mouse Y") * controls.helicopter_sensibility, -heliProperties.max_pitch_value, heliProperties.max_pitch_value);

        if (Input.GetKey(keyBinds.HELICOPTER_pitch_up_key))
            mouseY = heliProperties.max_pitch_value;
        if (Input.GetKey(keyBinds.HELICOPTER_pitch_down_key))
            mouseY = -heliProperties.max_pitch_value;

        HandleLeanInput(deltaTime);

        if (controls.invert_vertical_heli_mouse)
        {
            mouseY *= -1;
        }
    }

    protected void HandleLeanInput(float deltaTime)
    {
        if (Input.GetKey(keyBinds.HELICOPTER_lean_left_key))
        {
            lean_value -= heliProperties.lean_value * deltaTime;
            lean_value = Mathf.Clamp(lean_value, -heliProperties.max_lean_value, heliProperties.max_lean_value);
        }
        else if (Input.GetKey(keyBinds.HELICOPTER_lean_right_key))
        {
            lean_value += heliProperties.lean_value * deltaTime;
            lean_value = Mathf.Clamp(lean_value, -heliProperties.max_lean_value, heliProperties.max_lean_value);
        }
        else
        {
            lean_value = Mathf.Lerp(lean_value, 0, 2 * deltaTime);
        }
    }

    protected void ApplyRotationTorque()
    {
        rb.AddTorque(transform.forward * -mouseX * heliProperties.rotation_value * 200, ForceMode.Force);
        rb.AddTorque(transform.right * -mouseY * heliProperties.pitch_value * 2000, ForceMode.Force);
        rb.AddTorque(transform.up * lean_value * rb.mass * 1.5f);
    }

    #endregion

    #region Utility 

    protected void SnapPlayerToSeat(Transform seat)
    {
        if (player == null) return;

        // Fazer o parenting
        player.transform.SetParent(seat, false);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;
    }


    protected override void UpdateHUD()
    {
        helicopterHudManager.helicopterPilotHUD.UpdateDamage();
        helicopterHudManager.helicopterPilotHUD.UpdateRotationX(transform.eulerAngles.x);
        helicopterHudManager.helicopterPilotHUD.UpdateRotationY(transform.eulerAngles.y);
        helicopterHudManager.helicopterPilotHUD.UpdateAltitude(transform.position.y / 3);
        helicopterHudManager.helicopterPilotHUD.UpdateSpeed(rb.linearVelocity.magnitude);
        helicopterHudManager.helicopterPilotHUD.UpdateThrottle(throttle);

        if (countermeasures != null)
        {
            if (countermeasures.is_active)
            {
                helicopterHudManager.UpdateCountermeasuresStatus("Active");
            }
            else if (!countermeasures.is_active && countermeasures.is_reloading)
            {
                helicopterHudManager.UpdateCountermeasuresStatus("Reloading... [" + countermeasures.reload_countermeasures_duration.ToString("F0") + "]");
            }
            else if (!countermeasures.is_active && !countermeasures.is_reloading)
            {
                helicopterHudManager.UpdateCountermeasuresStatus("Ready");
            }
        }

    }

    #endregion

    #region Collision & Destruction 

    protected override void OnCollisionEnter(Collision collision)
    {
        if (!vehicle_destroyed)
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

            if (playerController != null) playerController.Damage(100);

            ExitVehicle();

            // Usando os valores obtidos da colisão
            HandleCollision(collision, 50);
            Explode(contactPoint, contactNormal, collision.gameObject.layer, 12);
        }
    }

    bool DestroyAnimation_do_once = true;
    protected override void DestroyAnimation()
    {
        if (DestroyAnimation_do_once)
        {
            fire_effects_parent.SetActive(true);
            DestroyAnimation_do_once = false;
        }
        destroyTimer += Time.fixedDeltaTime;
        rotate_value = Math.Clamp(Mathf.Pow(destroyTimer * 15, 2f), 0, 900);

        if (destroyTimer >= 10)
        {
            Explode(transform.position, transform.position.normalized, LayerMask.NameToLayer("Voxel"), 1);
        }

        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, collisionLayers))
        {
            //print(hit.distance);
            if (hit.distance >= 5)
            {
                if (!fall_alarm.isPlaying)
                {
                    fall_alarm.Play();
                }

                //transform.Rotate(0, rotate_value / 4, 0, Space.Self);

                rb.AddTorque(transform.up * rotate_value * rb.mass);
            }
            else
            {

                if (player != null)
                {
                    player.GetComponent<PlayerController>().Damage(100);
                    ExitVehicle();
                }

                Explode(hit.point, hit.normal, hit.transform.gameObject.layer, 12);

            }
        }
    }


    #endregion

}