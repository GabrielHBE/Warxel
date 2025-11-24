using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using VoxelDestructionPro.VoxelObjects;

public class Jet : Vehicle
{
    [HideInInspector] public bool is_in_jet;

    [Header("Keycodes")]
    public KeyCode switch_camera_key;
    public KeyCode main_cannon_key;
    public KeyCode upgrade_gun_key;
    public KeyCode shoot_key;
    public KeyCode pitch_up_key;
    public KeyCode pitch_down_key;
    public KeyCode lean_left_key;
    public KeyCode lean_right_key;
    public KeyCode start_engine_key;
    public KeyCode enter_jet_key;
    public KeyCode zoom_key;
    public KeyCode free_look_key;
    public KeyCode landing_gear_key;

    [SerializeField] private Transform foward_camera_position;
    [SerializeField] private Transform backward_camera_position;
    [SerializeField] private Transform inside_camera_position;


    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI hud_gravity_controller;
    [SerializeField] private TextMeshProUGUI hud_Gforce_controller;
    [SerializeField] private TextMeshProUGUI hud_speed_controller;
    [SerializeField] private GameObject hud_speed_controller_min_pos;
    [SerializeField] private GameObject hud_speed_controller_max_pos;
    [SerializeField] private TextMeshProUGUI hud_altidude_controller;
    [SerializeField] private GameObject hud_altidude_controller_max_pos;
    [SerializeField] private GameObject hud_altidude_controller_min_pos;


    [Header("Instances")]
    [SerializeField] private GameObject trails;
    [SerializeField] private Transform a10_core;
    [SerializeField] private Transform shoot_position;
    [SerializeField] private GameObject arms;
    [SerializeField] private Image blackImage;
    [SerializeField] private GameObject TurbineSmoke;
    [SerializeField] private GameObject glass;
    [SerializeField] private JetProperties jetProperties;


    [Header("Public variables")]
    [HideInInspector] public bool is_on_ground;
    [HideInInspector] public float mouseX, mouseY;
    [HideInInspector] public float moveForward;
    [HideInInspector] public int current_camera;
    [HideInInspector] public bool retract_landingGear = false;
    [HideInInspector] public float speed;
    [HideInInspector] public bool using_main_cannon = true;
    [HideInInspector] public JetUpgradeController upgrade;

    [Header("Sound")]
    [SerializeField] private AudioSource tinnitus;


    [HideInInspector] public float throttle = 0f;

    private float next_time_to_fire = 0;
    private float overheat;
    private bool overheated;
    private float passout_timer;
    private bool passout;
    private float current_speed_modifier;
    private float totalThrottle;
    private Vignette vignette;


    private Quaternion initial_camera_rotation;
    [HideInInspector] public float lean_value;
    private float exit_cooldown;
    private Volume volume;
    private float current_gravity = 0;
    private float downwardComponent;
    private float G_force;
    float max_speed = 500;



    void Start()
    {
        base.VehicleStart();

        hp = jetProperties.hp;
        resistance = jetProperties.resistance;

        MeshRenderer rend = hud.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            hud_material = rend.material;
            hud_material.color = hud__color;
            hud_material.SetColor("_EmissionColor", hud__color);
            hud_material.EnableKeyword("_EMISSION");

            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;

        }

        Color visibleColor = new Color(hud__color.r, hud__color.g, hud__color.b, 1f);
        hud_material.color = visibleColor;
        hud_material.SetColor("_EmissionColor", visibleColor);
        hud_altidude_controller.color = visibleColor;
        hud_speed_controller.color = visibleColor;


        TurbineSmoke.SetActive(false);
        vehicle_audio_listener.enabled = false;
        blackImage.enabled = false;
        volume = GetVolume();
        volume.profile.TryGet(out vignette);

        is_in_jet = false;

        current_camera = 1;

        initial_camera_rotation = inside_camera_position.localRotation;

        vehicle_camera.transform.localPosition = inside_camera_position.transform.position;
        vehicle_camera.enabled = false;


        acceleration = jetProperties.aceleration;

    }

    Volume GetVolume()
    {

        GameObject globalVolumeObj = GameObject.FindGameObjectWithTag("GlobalVolume");
        if (globalVolumeObj != null)
        {
            return globalVolumeObj.GetComponent<Volume>();
        }

        return null;

    }

    IEnumerator IncreasePitch(AudioSource audio, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audio.pitch += 0.0001f;
            yield return null;
        }
    }

    IEnumerator DecreasePitch(AudioSource audio, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audio.pitch -= 0.0001f;
            yield return null;
        }

        audio.Stop();
    }


    void FixedUpdate()
    {
        if (is_in_jet)
        {
            if (start_engine)
            {
                Move();

                Rotate();

            }

            ApplyDiveSpeedBoost();
        }

        GravityModifier();
        rb.AddForce(Physics.gravity * current_gravity, ForceMode.Acceleration);
        if (speed < max_speed)
        {
            totalThrottle = throttle + current_speed_modifier;
            rb.AddForce(forwardReference * totalThrottle * jetProperties.max_throttle);
        }


    }

    void Update()
    {
        speed = rb.linearVelocity.magnitude;
        if (is_in_jet)
        {
            UpdateHUD();

            moveForward = Input.GetAxisRaw("Vertical");

            forwardReference = -transform.right;

            exit_cooldown += Time.deltaTime;

            Quaternion targetRotation = Quaternion.Euler(glass.transform.localEulerAngles.x, glass.transform.localEulerAngles.y, 0);
            glass.transform.localRotation = Quaternion.RotateTowards(glass.transform.localRotation, targetRotation, 30 * Time.deltaTime);

            Start_Stop_Engine();
            CameraController();
            FreeLook();
            CalculateGForce();

            if (Input.GetKeyDown(enter_jet_key) && exit_cooldown > 0.1f)
            {
                hud.SetActive(false);
                player.SetActive(true);
                vehicle_camera.enabled = false;
                vehicle_audio_listener.enabled = false;
                TurbineSmoke.SetActive(false);
                arms.SetActive(false);
                vignette.intensity.value = 0;
                is_in_jet = false;

                if (throttle > 10)
                {
                    EjectPlayer();
                }
                else
                {
                    ExitHevicle();
                }

            }

            if (start_engine)
            {
                Lean();
                Shoot();
                Passout();
                Switch_weapon();

                jetProperties.interior_turbine.pitch = Math.Clamp(jetProperties.interior_turbine.pitch, 0.15f, 2);
                TurbineSmoke.SetActive(true);

                if (Physics.Raycast(a10_core.position, Vector3.down, 6, LayerMask.GetMask("Ground") | LayerMask.GetMask("Voxel")))
                {
                    retract_landingGear = false;
                }
                else
                {
                    retract_landingGear = true;
                }

            }
            else
            {

                jetProperties.interior_turbine.pitch = Math.Clamp(jetProperties.interior_turbine.pitch, 0.01f, 2);
                TurbineSmoke.SetActive(false);
                if (throttle > 0)
                {
                    throttle -= acceleration * Time.deltaTime * 1.5f;
                }

            }

        }
        else
        {
            if (vignette.intensity.value > 0)
            {
                vignette.intensity.value -= Time.deltaTime;
            }

            Quaternion targetRotation = Quaternion.Euler(glass.transform.localEulerAngles.x, glass.transform.localEulerAngles.y, -90);
            glass.transform.localRotation = Quaternion.RotateTowards(glass.transform.localRotation, targetRotation, 30 * Time.deltaTime);

            jetProperties.interior_turbine.Stop();
            jetProperties.interior_turbine.pitch = 0.01f;

            if (throttle > 0)
            {
                throttle -= acceleration * Time.deltaTime * 1.5f;
            }
        }
    }


    void FreeLook()
    {
        if (Input.GetKey(free_look_key))
        {
            float mouseY_freelook = Input.GetAxis("Mouse Y") * -mouseSensitivity;
            float mouseX_freelook = Input.GetAxis("Mouse X") * mouseSensitivity;

            Vector3 currentEuler = inside_camera_position.localEulerAngles;

            float currentX = (currentEuler.x > 100) ? currentEuler.x - 360 : currentEuler.x;
            float currentY = (currentEuler.y > 100) ? currentEuler.y - 360 : currentEuler.y;

            currentX += mouseY_freelook;
            currentY += mouseX_freelook;

            currentX = Mathf.Clamp(currentX, -80f, 10f);
            currentY = Mathf.Clamp(currentY, -180f, 0f);

            inside_camera_position.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
        else
        {
            inside_camera_position.localRotation = Quaternion.Lerp(
                inside_camera_position.localRotation,
                initial_camera_rotation,
                Time.deltaTime * 3
            );
        }
    }

    void Zoom()
    {
        if (Input.GetKey(zoom_key))
        {

            if (using_main_cannon)
            {
                float targetFov = minFov / jetProperties.zoom;
                vehicle_camera.fieldOfView = Mathf.Lerp(vehicle_camera.fieldOfView, targetFov, 2 * Time.deltaTime);

                if (upgrade != null)
                {
                    upgrade.UseCamera(false);
                }

            }
            else
            {
                if (upgrade != null)
                {
                    upgrade.UseCamera(true);
                }

            }

        }
        else
        {
            vehicle_camera.enabled = true;
            vehicle_audio_listener.enabled = true;

            if (upgrade != null)
            {
                upgrade.UseCamera(false);
            }

            vehicle_camera.fieldOfView = Mathf.Lerp(
            vehicle_camera.fieldOfView,
            minFov,
            2 * Time.deltaTime);
        }
    }

    void Shoot()
    {
        if (using_main_cannon)
        {
            if (Input.GetKey(shoot_key) && overheated == false)
            {


                if (next_time_to_fire <= 0f)
                {
                    jetProperties.shoot_sound.PlayOneShot(jetProperties.shoot_sound.clip);
                    Transform bulletObj = Instantiate(jetProperties.bullefPref, shoot_position.position, shoot_position.rotation);

                    Destroy(bulletObj.gameObject, 10f);

                    bulletObj.GetComponent<Bullet>().CreateBullet(shoot_position.forward, jetProperties.muzzle_velocity, jetProperties.bullet_drop, jetProperties.damage, jetProperties.damage_dropoff, jetProperties.damage_dropoff_timer, jetProperties.destruction_force, jetProperties.minimum_damage, 2, jetProperties.bullet_hit_effect);
                    next_time_to_fire = jetProperties.interval;

                }
                overheat += Time.deltaTime;

            }
            else
            {
                if (overheated == true)
                {
                    overheat -= Time.deltaTime / 2;
                }
                else
                {
                    overheat -= Time.deltaTime;
                }
            }

            if (overheat >= jetProperties.overheat_time || overheated == true)
            {
                overheated = true;
            }

            if (overheat <= 0.02)
            {
                overheated = false;
            }

            overheat = Math.Clamp(overheat, 0, jetProperties.overheat_time);

            next_time_to_fire -= Time.deltaTime;


        }
        else if (upgrade != null)
        {
            if (Input.GetKeyDown(shoot_key) && upgrade.CanShoot())
            {
                upgrade.Shoot();
            }
        }



    }

    void Passout()
    {

        if (totalThrottle >= 100)
        {
            if (!passout)
            {
                if (G_force > 0)
                {
                    float deltaTime = (G_force / 100) * Time.deltaTime;
                    vignette.color.value = Color.Lerp(vignette.color.value, Color.black, deltaTime * 3);
                    vignette.intensity.value += deltaTime;
                }
                else if (G_force < 0)
                {
                    float deltaTime = (-G_force / 100) * Time.deltaTime;
                    vignette.color.value = Color.Lerp(vignette.color.value, Color.darkRed, deltaTime * 3);
                    vignette.intensity.value += deltaTime;
                }
                else if (vignette.intensity.value > 0)
                {
                    vignette.intensity.value -= Time.deltaTime;
                }

                vignette.intensity.value = Mathf.Clamp(vignette.intensity.value, 0, 1);

                if (vignette.intensity.value >= 1f)
                {
                    passout_timer += Time.deltaTime;
                    if (passout_timer >= 3f)
                    {
                        passout = true;
                        blackImage.enabled = true;
                        passout_timer = 3f;
                    }
                }
            }
            else
            {
                passout_timer -= Time.deltaTime;

                if (passout_timer <= 0f)
                {
                    passout = false;
                    vignette.intensity.value = 0f;
                    blackImage.enabled = false;
                    passout_timer = 0f;

                    vignette.color.value = Color.black;
                }
            }
        }

        if (blackImage.enabled && !tinnitus.isPlaying)
        {
            tinnitus.Play();
            jetProperties.interior_turbine.Stop();
        }
        else if (!blackImage.enabled && !jetProperties.interior_turbine.isPlaying)
        {
            tinnitus.Stop();
            jetProperties.interior_turbine.Play();
        }
    }


    void Rotate()
    {
        if (passout)
            return;

        float deltaTime = Time.fixedDeltaTime;

        float rawMouseX = Input.GetAxis("Mouse X") * mouseSensitivity * jetProperties.pitch_value;
        float rawMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * jetProperties.pitch_value;

        float keyboardPitch = 0f;

        if (Input.GetKey(pitch_up_key))
            keyboardPitch += 1f;
        if (Input.GetKey(pitch_down_key))
            keyboardPitch -= 1f;

        keyboardPitch = Mathf.Clamp(keyboardPitch, -1f, 1f);

        rawMouseY += keyboardPitch * jetProperties.pitch_value * mouseSensitivity;

        if (jetProperties.invertY)
            rawMouseY *= -1;

        mouseX = Mathf.Clamp(rawMouseX, -jetProperties.max_rotation_value, jetProperties.max_rotation_value);
        mouseY = Mathf.Clamp(rawMouseY, -jetProperties.max_pitch_value, jetProperties.max_pitch_value);

        if (Math.Abs(mouseY) > 1)
        {
            throttle -= (Math.Abs(mouseY) * deltaTime * speed) / 70;
        }


        foreach (TrailRenderer trail in trails.GetComponentsInChildren<TrailRenderer>())
        {
            if ((mouseX > 10 || mouseX < 10 || mouseY > 10 || mouseY < 10) && (mouseX != 0 || mouseY != 0) && speed > 100)
            {
                if (!trail.emitting)
                {

                    trail.Clear();
                    trail.emitting = true;
                }
            }
            else
            {
                trail.emitting = false;
            }
        }

        rb.AddTorque(transform.right * mouseX * speed * jetProperties.max_throttle / 2 * rb.mass * deltaTime);
        rb.AddTorque(-transform.forward * mouseY * speed * jetProperties.max_throttle / 2 * rb.mass * deltaTime);
    }


    void CalculateGForce()
    {
        float deltaTime = Time.deltaTime;

        if (mouseY != 0)
        {
            G_force = (deltaTime / 3) * speed * mouseY;

        }
        else
        {
            G_force = Mathf.MoveTowards(G_force, 0f, deltaTime * 5);
        }

        G_force = Math.Clamp(G_force, -10, 10);

    }



    void Lean()
    {
        if (passout) return;

        float leanInput = 0f;

        if (Input.GetKey(lean_left_key))
        {
            leanInput -= 1f;
            lean_value += leanInput * jetProperties.lean_value * Time.deltaTime;
        }
        else if (Input.GetKey(lean_right_key))
        {
            leanInput += 1f;
            lean_value += leanInput * jetProperties.lean_value * Time.deltaTime;
        }
        else
        {
            lean_value = Mathf.MoveTowards(lean_value, 0f, 25 * Time.deltaTime);
        }

        float speedFactor = Mathf.Clamp01(speed / jetProperties.max_throttle);

        float rotationAmount = is_on_ground && (throttle >= 20 || throttle < -10) && throttle <= 50 ? lean_value * Time.deltaTime : lean_value * speedFactor * Time.deltaTime;

        if (throttle < 0)
        {
            rotationAmount *= -1;
        }

        rotationAmount = Mathf.Clamp(rotationAmount, -jetProperties.max_lean_value, jetProperties.max_lean_value);
        transform.Rotate(Vector3.up * rotationAmount, Space.Self);
    }

    void Move()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (is_on_ground && mouseY > 0 && speed > 50)
        {
            rb.AddForce(Vector3.up * rb.mass * 20);
        }

        if (moveForward > 0 && passout == false)
        {
            jetProperties.interior_turbine.pitch = Mathf.MoveTowards(
                jetProperties.interior_turbine.pitch,
                2f,
                0.1f * deltaTime
            );

            throttle += acceleration * deltaTime;
            if (throttle > jetProperties.max_throttle)
            {
                throttle = jetProperties.max_throttle;
            }
        }
        else if (moveForward < 0 && passout == false)
        {
            if (is_on_ground)
            {
                if (throttle > -30)
                {
                    Debug.Log("OI");
                    jetProperties.interior_turbine.pitch = Mathf.MoveTowards(
                        jetProperties.interior_turbine.pitch,
                        0.15f,
                        0.1f * deltaTime
                    );
                    throttle -= acceleration * deltaTime * 2;
                }

            }
            else
            {
                if (throttle > 100)
                {

                    jetProperties.interior_turbine.pitch = Mathf.MoveTowards(
                        jetProperties.interior_turbine.pitch,
                        0.15f,
                        0.1f * deltaTime
                    );
                    throttle -= acceleration * deltaTime;
                }
            }
        }
        else
        {

            if (is_on_ground)
            {
                float decelerationRate = acceleration * 0.8f;
                throttle = Mathf.MoveTowards(throttle, 0, decelerationRate * deltaTime);

                jetProperties.interior_turbine.pitch = Mathf.MoveTowards(
                    jetProperties.interior_turbine.pitch,
                    0.15f,
                    0.1f * deltaTime
                );

                jetProperties.interior_turbine.pitch = Mathf.Clamp(jetProperties.interior_turbine.pitch, 0.15f, 2f);
            }
            else
            {
                float decelerationRate = acceleration * 0.1f;
                throttle = Mathf.MoveTowards(throttle, 0, decelerationRate * deltaTime);

                jetProperties.interior_turbine.pitch = Mathf.MoveTowards(
                    jetProperties.interior_turbine.pitch,
                    0.15f,
                    0.005f * deltaTime
                );

                jetProperties.interior_turbine.pitch = Mathf.Clamp(jetProperties.interior_turbine.pitch, 0.15f, 2f);
            }

        }

    }


    void ApplyDiveSpeedBoost()
    {
        Vector3 jetForward = -transform.right;

        downwardComponent = -jetForward.y;

        if (downwardComponent > 0.3f) // Down
        {
            float gravityBoost = downwardComponent * Physics.gravity.magnitude * 0.5f;
            float aerodynamicBoost = downwardComponent * jetProperties.dive_speed_boost;

            float totalBoost = (gravityBoost + aerodynamicBoost) * Time.fixedDeltaTime;

            totalBoost = Mathf.Clamp(totalBoost, 0, jetProperties.max_throttle * 1.2f);

            current_speed_modifier = totalBoost * 400 * Time.fixedDeltaTime;

        }
        else if (downwardComponent < -0.3f) // Up
        {
            float upwardIntensity = -downwardComponent;

            float airResistance = upwardIntensity * Physics.gravity.magnitude * 0.3f;
            float gravityPenalty = upwardIntensity * jetProperties.dive_speed_boost * 0.5f;

            float totalPenalty = (airResistance + gravityPenalty) * Time.fixedDeltaTime;

            totalPenalty = Mathf.Clamp(totalPenalty, 0, jetProperties.max_throttle * 0.7f);

            current_speed_modifier = -totalPenalty * 400 * Time.fixedDeltaTime;
        }
        else
        {
            current_speed_modifier = Mathf.Lerp(current_speed_modifier, 0, 2 * Time.fixedDeltaTime);
        }

    }

    void GravityModifier()
    {
        if (is_on_ground)
        {
            current_gravity = 0;
            return;
        }

        float targetGravity = 1f;


        if (downwardComponent > 0.3f) //Down
        {
            if (moveForward > 0)
            {
                targetGravity = (jetProperties.max_throttle / (speed * 2)) * -downwardComponent;
            }
            else
            {
                targetGravity = (jetProperties.max_throttle / speed) * -downwardComponent;
            }
        }
        else if (downwardComponent < -0.3f) //Up
        {

            if (moveForward > 0)
            {
                targetGravity = 1.5f * -downwardComponent;
            }
            else
            {
                targetGravity = (jetProperties.max_throttle / speed) * -downwardComponent;
            }
        }
        else
        {
            if (moveForward > 0)
            {
                targetGravity = 0;
            }
            else
            {
                if (throttle < 100)
                {
                    targetGravity = jetProperties.max_throttle / (speed * 3);
                }

            }
        }

        current_gravity = Mathf.Lerp(current_gravity, targetGravity, Time.fixedDeltaTime);

        current_gravity = Mathf.Clamp(current_gravity, 0f, 5f);
    }


    void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.GetComponent<JetUpgrades>() != null)
            return;


        float explosionForce = throttle / 9;

        if (throttle > 400)
        {
            explosionForce = 400 / 9;
        }


        ContactPoint contact = collision.contacts[0];

        bool do_once = true;
        Collider[] colliders = Physics.OverlapSphere(contact.point, explosionForce);


        if (throttle > 100 && crash_explosion != null && !is_on_ground)
        {

            Explode(colliders, contact, collision, explosionForce);

        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Voxel"))
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                PlayerProperties playerProps = colliders[i].GetComponentInParent<PlayerProperties>();

                if (do_once && playerProps != null)
                {
                    Debug.Log("Colidiu com Player");
                    float distance = Vector3.Distance(transform.position, playerProps.transform.position);
                    float damage = Mathf.Clamp(explosionForce * (1 - distance / 10), 0, explosionForce);
                    playerProps.Damage(damage * 2);

                    CameraShake cameraShake = playerProps.GetComponentInChildren<CameraShake>();
                    if (cameraShake != null)
                    {
                        cameraShake.StartCoroutine(cameraShake.ExplosionShake(damage / 10, 1f));
                    }
                    else
                    {
                        Debug.LogWarning("CameraShake não encontrado!");
                    }

                    do_once = false;
                }

                DynamicVoxelObj vox = colliders[i].GetComponentInParent<DynamicVoxelObj>();

                if (vox == null)
                    continue;


                vox.AddDestruction_Line(contact.point, contact.normal, explosionForce);
            }

            Damage(explosionForce);

        }

    }

    void EjectPlayer()
    {

        Quaternion spawnRotation = new Quaternion(0, exit_vehicle_position.transform.rotation.y, 0, exit_vehicle_position.transform.rotation.w);
        Vector3 spawnPosition = new Vector3(inside_camera_position.position.x, glass.transform.position.y + 5, inside_camera_position.position.z);


        player.transform.position = spawnPosition;
        player.transform.rotation = spawnRotation;


        // Aplica força ao novo player
        Rigidbody player_rb = player.GetComponent<Rigidbody>();
        if (player_rb != null)
        {

            Vector3 ejectDirection = Vector3.up;

            // Intensidade da ejeção
            float ejectForce = player_rb.mass * (throttle / 2);

            player_rb.AddForce(ejectDirection * 25, ForceMode.Impulse);
            player_rb.AddForce(forwardReference * ejectForce, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-200f, 200f),
                UnityEngine.Random.Range(-100f, 100f),
                UnityEngine.Random.Range(-200f, 200f)
            );
            player_rb.AddTorque(randomTorque, ForceMode.Impulse);
        }

    }

    protected override void UpdateHUD()
    {

        hud_speed_controller.text = speed.ToString("F0");
        hud_altidude_controller.text = (transform.position.y / 2).ToString("F0");
        hud_gravity_controller.text = current_gravity.ToString("F1") + " ↓";
        hud_Gforce_controller.text = G_force.ToString("F0");

        Vector3 speed_currentPosition = Vector3.Lerp(hud_speed_controller_min_pos.transform.localPosition, hud_speed_controller_max_pos.transform.localPosition, Mathf.Clamp01(speed / max_speed));
        hud_speed_controller.transform.localPosition = speed_currentPosition;

        Vector3 altitude_currentPosition = Vector3.Lerp(
            hud_altidude_controller_min_pos.transform.localPosition,
            hud_altidude_controller_max_pos.transform.localPosition,
            Mathf.Clamp01(transform.position.y / 2 / 400)
        );

        hud_altidude_controller.transform.localPosition = altitude_currentPosition;

    }


    public override void EnterVehicle(GameObject _player)
    {
        exit_cooldown = 0;

        is_in_jet = !is_in_jet;
        player = _player;

        if (is_in_jet)
        {
            player.SetActive(false);
            vehicle_audio_listener.enabled = true;
            minFov = vehicle_camera.fieldOfView;
            vehicle_camera.enabled = true;
            TurbineSmoke.SetActive(start_engine);
            vehicle_camera.transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y + 3, _player.transform.position.z);
            vehicle_camera.transform.rotation = _player.transform.rotation;
            arms.SetActive(true);
            hud.SetActive(true);
        }

    }

    protected override void Start_Stop_Engine()
    {
        if (Input.GetKeyDown(start_engine_key))
        {
            start_engine = !start_engine;

            if (start_engine)
            {
                jetProperties.interior_turbine.Play();
                StartCoroutine(IncreasePitch(jetProperties.interior_turbine, 2));
            }
            else
            {
                StartCoroutine(DecreasePitch(jetProperties.interior_turbine, 2));
            }
        }
    }


    protected override void CameraController()
    {
        Zoom();

        Transform target = null;
        float targetDistortion = 0f;
        float targetBlend = 0f;
        float change_camera_speed = 0;

        if (Input.GetKeyDown(switch_camera_key))
        {
            current_camera += 1;
            if (current_camera > 3)
            {
                current_camera = 1;
            }
        }

        if (current_camera == 1)
        {
            target = inside_camera_position.transform;
            targetDistortion = 0f;
            targetBlend = 0f;
            vehicle_camera.transform.SetParent(target);
            change_camera_speed = 3;
        }
        else if (current_camera == 2)
        {
            target = foward_camera_position.transform;
            targetDistortion = 0.1f;
            targetBlend = 1f;
            vehicle_camera.transform.SetParent(transform);
            change_camera_speed = 3;
        }
        else
        {
            target = backward_camera_position.transform;
            targetDistortion = 0.1f;
            targetBlend = 1f;
            vehicle_camera.transform.SetParent(transform);
            change_camera_speed = 3;
        }

        vehicle_camera.transform.position = Vector3.Lerp(vehicle_camera.transform.position, target.position, change_camera_speed * Time.deltaTime);
        vehicle_camera.transform.rotation = Quaternion.Lerp(vehicle_camera.transform.rotation, target.rotation, change_camera_speed * Time.deltaTime);

        distortion.distortionLevel = targetDistortion;
        jetProperties.interior_turbine.spatialBlend = targetBlend;

    }


    protected override void Switch_weapon()
    {

        if (Input.GetKeyDown(main_cannon_key))
        {
            using_main_cannon = true;
            upgrade.SetActive(!using_main_cannon);
        }

        if (Input.GetKeyDown(upgrade_gun_key) && upgrade != null)
        {
            using_main_cannon = false;
            upgrade.SetActive(!using_main_cannon);
        }

    }

}