using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using VoxelDestructionPro.VoxelObjects;
using static VoxelDestructionPro.Data.DestructionData;

public class Jet : MonoBehaviour
{
    public bool is_in_jet;
    [Header("Keycodes")]
    public KeyCode switch_camera_key;
    public KeyCode shoot_key;
    public KeyCode pitch_up_key;
    public KeyCode pitch_down_key;
    public KeyCode lean_left_key;
    public KeyCode lean_right_key;
    public KeyCode start_engine_key;
    public KeyCode enter_jet_key;
    public KeyCode zoom_key;
    public KeyCode free_look_key;

    [Header("Cameras")]
    public float mouseSensitivity = 2f;
    public Camera jet_camera;
    public Transform foward_camera_position;
    public Transform backward_camera_position;
    public Transform inside_camera_position;

    [Header("Destruction")]
    [SerializeField] private DestructionType destructionType = DestructionType.Sphere;


    [Header("Positions")]
    public Transform exit_jet_position;
    [SerializeField] private Transform shoot_position;

    [Header("Instances")]
    public GameObject player;
    public GameObject playerPrefab;
    public GameObject arms;
    private Vignette vignette;
    public Volume volume;
    public Image blackImage;
    public GameObject TurbineSmoke;
    public GameObject glass;
    public GameObject crash_explosion;
    public AudioSource crash_sound;
    public Vector3 forwardReference;
    public JetProperties jetProperties;
    public Rigidbody rb;

    [Header("Sound")]
    public AudioSource tinnitus;
    public AudioListener jet_audio_listener;
    public AudioListener player_audio_listener;
    public AudioDistortionFilter distortion;
    [HideInInspector] public float mouseX, mouseY;
    private float acceleration;
    [HideInInspector] public float currentSpeed = 0f;
    private bool start_engine;
    [HideInInspector] public float moveForward;
    [HideInInspector] public int current_camera;
    private float minFov;
    float next_time_to_fire = 0;
    float overheat;
    bool overheated;
    float passout_timer;
    bool passout;
    
    Quaternion initial_camera_rotation;
    [HideInInspector] public float lean_value;
    private float exit_cooldown;
    private float gravity=10;

    void Start()
    {
        
        TurbineSmoke.SetActive(false);
        jet_audio_listener.enabled = false;
        blackImage.enabled = false;
        start_engine = false;
        volume.profile.TryGet(out vignette);

        is_in_jet = false;

        current_camera = 1;
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        initial_camera_rotation = inside_camera_position.localRotation;

        jet_camera.transform.localPosition = inside_camera_position.transform.position;
        jet_camera.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        acceleration = jetProperties.aceleration;

        if (shoot_key == KeyCode.Alpha0)
        {
            shoot_key = KeyCode.Mouse0;
        }
        if (zoom_key == KeyCode.Alpha1)
        {
            zoom_key = KeyCode.Mouse1;
        }

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

    public void Enter_Exit(GameObject _player)
    {
        if (Input.GetKey(enter_jet_key) && !is_in_jet)
        {
            exit_cooldown = 0;

            is_in_jet = !is_in_jet;
            player = _player;

            if (is_in_jet)
            {
                // Destroi o player atual em vez de desativar
                Destroy(player);
                jet_audio_listener.enabled = true;
                minFov = jet_camera.fieldOfView;
                jet_camera.enabled = true;
                TurbineSmoke.SetActive(start_engine);
                jet_camera.transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y + 3, _player.transform.position.z);
                jet_camera.transform.rotation = _player.transform.rotation;
                arms.SetActive(true);
            }
        }
    }


    void Update()
    {
        if (is_in_jet)
        {
            forwardReference = - transform.right;

            exit_cooldown += Time.deltaTime;

            if (Input.GetKeyDown(enter_jet_key) && exit_cooldown > 0.1f)
            {
                EjectPlayer();
            }

            Quaternion targetRotation = Quaternion.Euler(glass.transform.localEulerAngles.x, glass.transform.localEulerAngles.y, 0);
            glass.transform.localRotation = Quaternion.RotateTowards(glass.transform.localRotation, targetRotation, 30 * Time.deltaTime);

            moveForward = Input.GetAxisRaw("Vertical");

            Move();
            Lean();
            SwitchCamera();
            Shoot();
            Zoom();
            Rotate();
            FreeLook();
            Passout();

            if (start_engine)
            {
                jetProperties.interior_turbine.pitch = Math.Clamp(jetProperties.interior_turbine.pitch, 0.15f, 2);
                TurbineSmoke.SetActive(true);
            }
            else
            {
                jetProperties.interior_turbine.pitch = Math.Clamp(jetProperties.interior_turbine.pitch, 0.01f, 2);
                TurbineSmoke.SetActive(false);
                currentSpeed -= acceleration * Time.deltaTime * 1.5f; // Diminui a velocidade com o tempo
            }
        }
        else
        {
            Quaternion targetRotation = Quaternion.Euler(glass.transform.localEulerAngles.x, glass.transform.localEulerAngles.y, -90);
            glass.transform.localRotation = Quaternion.RotateTowards(glass.transform.localRotation, targetRotation, 30 * Time.deltaTime);

            jetProperties.interior_turbine.Stop();
            jetProperties.interior_turbine.pitch = 0.01f;

            currentSpeed -= acceleration * Time.deltaTime * 1.5f;
        }

        /*
        if (currentSpeed > 100)
        {
            //rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            //rb.isKinematic = false;
            rb.useGravity = true;
        }
        */
        currentSpeed = Math.Clamp(currentSpeed, 0, jetProperties.max_speed);
        rb.AddForce(forwardReference * currentSpeed * (jetProperties.max_speed/5));
        
        //transform.position += forwardReference.forward * currentSpeed * Time.deltaTime;
    }

    void FreeLook()
    {
        if (Input.GetKey(free_look_key))
        {
            float mouseY_freelook = Input.GetAxis("Mouse Y") * -mouseSensitivity;
            float mouseX_freelook = Input.GetAxis("Mouse X") * mouseSensitivity;

            Vector3 currentEuler = inside_camera_position.localEulerAngles;

            // Corrige os valores para o intervalo -180° a 180°
            float currentX = (currentEuler.x > 100) ? currentEuler.x - 360 : currentEuler.x;
            float currentY = (currentEuler.y > 100) ? currentEuler.y - 360 : currentEuler.y;

            // Aplica o input
            currentX += mouseY_freelook;
            currentY += mouseX_freelook;

            // Limita dentro dos valores desejados
            currentX = Mathf.Clamp(currentX, -80f, 10f);
            currentY = Mathf.Clamp(currentY, -180f, 0f);  // Equivalente a Y de 180° a 360°

            // Aplica a rotação final
            inside_camera_position.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
        else
        {
            inside_camera_position.localRotation = Quaternion.Lerp(
                inside_camera_position.localRotation,
                initial_camera_rotation,
                1f
            );
        }
    }




    void Zoom()
    {
        if (Input.GetKey(zoom_key))
        {

            float targetFov = minFov / jetProperties.zoom;

            jet_camera.fieldOfView = Mathf.Lerp(jet_camera.fieldOfView, targetFov, 2 * Time.deltaTime);
        }
        else
        {
            jet_camera.fieldOfView = Mathf.Lerp(
            jet_camera.fieldOfView,
            minFov,
            2 * Time.deltaTime);
        }
    }

    void Shoot()
    {

        if (Input.GetKey(shoot_key) && overheated == false)
        {


            if (next_time_to_fire <= 0f)
            {
                jetProperties.shoot_sound.PlayOneShot(jetProperties.shoot_sound.clip);
                Transform bulletObj = Instantiate(jetProperties.bullefPref, shoot_position.position, shoot_position.rotation);

                Destroy(bulletObj.gameObject, 10f);

                bulletObj.GetComponent<Bullet>().CreateBullet(shoot_position.forward, jetProperties.muzzle_velocity, jetProperties.bullet_drop, jetProperties.damage, jetProperties.damage_dropoff, jetProperties.damage_dropoff_timer, jetProperties.destruction_force, jetProperties.bullet_hit_effect);
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

    void SwitchCamera()
    {


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

            jet_camera.transform.position = Vector3.Lerp(jet_camera.transform.position, inside_camera_position.transform.position, 3 * Time.deltaTime);
            jet_camera.transform.rotation = Quaternion.Lerp(jet_camera.transform.rotation, inside_camera_position.transform.rotation, 3 * Time.deltaTime);
            distortion.distortionLevel = 0;
            jetProperties.interior_turbine.spatialBlend = 0;

        }
        else if (current_camera == 2)
        {


            jet_camera.transform.position = Vector3.Lerp(jet_camera.transform.position, foward_camera_position.transform.position, 3 * Time.deltaTime);
            jet_camera.transform.rotation = Quaternion.Lerp(jet_camera.transform.rotation, foward_camera_position.transform.rotation, 3 * Time.deltaTime);
            distortion.distortionLevel = 0.5f;
            jetProperties.interior_turbine.spatialBlend = 1;

        }
        else
        {

            jet_camera.transform.position = Vector3.Lerp(jet_camera.transform.position, backward_camera_position.transform.position, 3 * Time.deltaTime);
            jet_camera.transform.rotation = Quaternion.Lerp(jet_camera.transform.rotation, backward_camera_position.transform.rotation, 3 * Time.deltaTime);
            distortion.distortionLevel = 0.5f;
            jetProperties.interior_turbine.spatialBlend = 1;
        }


    }

    void Passout()
    {
        if (currentSpeed >= 100)
        {
            if (!passout)
            {
                // Lógica para entrar no estado de passout
                if (mouseY > 0)
                {
                    vignette.intensity.value += 0.0002f * (currentSpeed / 500);
                }
                else
                {
                    vignette.intensity.value -= 0.0008f * (currentSpeed / 500);
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
                }

            }

        }

        // Lógica do áudio
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
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * jetProperties.pitch_value;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * jetProperties.pitch_value;

        if (Input.GetKey(pitch_up_key) && passout == false)
        {
            mouseY += Time.deltaTime * jetProperties.pitch_value;
        }


        if (Input.GetKey(pitch_down_key) && passout == false)
        {
            mouseY -= Time.deltaTime * jetProperties.pitch_value;
        }

        if (jetProperties.invertY)
        {
            mouseY *= -1 * Time.deltaTime;
        }

        mouseX = Math.Clamp(mouseX, -jetProperties.rotation_value, jetProperties.rotation_value);
        mouseY = Math.Clamp(mouseY, -jetProperties.pitch_value, jetProperties.pitch_value);

        /*
        if (currentSpeed >= 30)
        {
            transform.Rotate(Vector3.back * mouseY * currentSpeed / jetProperties.max_speed * jetProperties.pitch_value, Space.Self);
            transform.Rotate(Vector3.right * mouseX * currentSpeed / jetProperties.max_speed * jetProperties.rotation_value, Space.Self);
        }
        */

        rb.AddTorque(transform.right * mouseX * currentSpeed * jetProperties.max_speed/2);
        rb.AddTorque(-transform.forward * mouseY * currentSpeed * jetProperties.max_speed/2);


    }

    void Lean()
    {

        if (Input.GetKey(lean_left_key) && passout == false)
        {
            lean_value -= 0.1f;

        }
        else if (Input.GetKey(lean_right_key) && passout == false)
        {
            lean_value += 0.1f;
        }
        else
        {
            lean_value = 0;
        }

        lean_value = Math.Clamp(lean_value, -jetProperties.lean_value, jetProperties.lean_value);

        transform.Rotate(Vector3.up * lean_value * currentSpeed / jetProperties.max_speed * Time.deltaTime, Space.Self);

    }

    void Move()
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

        if (start_engine && moveForward > 0 && passout == false)
        {
            if (gravity > 0)
            {
                gravity -= 2 * Time.deltaTime;
            }
            jetProperties.interior_turbine.pitch += 0.1f * Time.deltaTime;
            currentSpeed += acceleration * Time.deltaTime;
        }
        else if (start_engine && moveForward < 0 && passout == false)
        {
            gravity += 2 * Time.deltaTime;
            jetProperties.interior_turbine.pitch -= 0.1f * Time.deltaTime;
            currentSpeed -= acceleration * Time.deltaTime * 1.5f * Time.deltaTime;
        }

    }


    void OnCollisionEnter(Collision collision)
    {
        float explosionForce = currentSpeed / 9;

        if (currentSpeed > 400)
        {
            explosionForce = 400 / 9;
        }

        if (collision.gameObject.CompareTag("Voxel"))
        {
            ContactPoint contact = collision.contacts[0];

            bool do_once = true;
            Collider[] colliders = Physics.OverlapSphere(contact.point, explosionForce);


            if (currentSpeed > 100 && crash_explosion != null)
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
                    {
                        continue;
                    }

                    // PRIMEIRO: Destruição imediata (raio menor)
                    vox.AddDestruction_Sphere(contact.point, explosionForce);


                }
                Mod_DestroyAfterAll mod_DestroyAfterAll = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
                mod_DestroyAfterAll?.StartCoroutine(mod_DestroyAfterAll.FallUpperVoxels(explosionForce, contact.point));

                GameObject obj = Instantiate(crash_explosion, contact.point + contact.normal, Quaternion.LookRotation(contact.normal));
                obj.transform.localScale *= explosionForce / 10;


                AudioSource crash_sound_obj = obj.AddComponent<AudioSource>();
                // Copiar todas as propriedades do AudioSource original
                crash_sound_obj.clip = crash_sound.clip;
                crash_sound_obj.volume = crash_sound.volume;
                crash_sound_obj.pitch = crash_sound.pitch;
                crash_sound_obj.loop = crash_sound.loop;
                crash_sound_obj.spatialBlend = crash_sound.spatialBlend;
                crash_sound_obj.maxDistance = crash_sound.maxDistance;
                crash_sound_obj.rolloffMode = AudioRolloffMode.Custom;

                crash_sound_obj.Play();

                if (is_in_jet)
                {
                    EjectPlayer();
                }

                Destroy(gameObject);

            }
            else
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


            }


        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        Debug.Log(mouseY);
        if(LayerMask.LayerToName(collision.gameObject.layer) == "Ground" && mouseY>0 && currentSpeed>30)
        {
            rb.AddForce(Vector3.up * rb.linearVelocity.magnitude * 135);
        }
    }

    void EjectPlayer()
    {

        Quaternion spawnRotation = new Quaternion(0, exit_jet_position.transform.rotation.y, 0,exit_jet_position.transform.rotation.w);

        if (currentSpeed < 100)
        {
            GameObject newPlayer = Instantiate(playerPrefab, exit_jet_position.position, spawnRotation);
            player = newPlayer;

        }
        else
        {
            // Instancia um novo player em vez de ativar
            if (playerPrefab != null)
            {
                Vector3 spawnPosition = new Vector3(inside_camera_position.position.x, glass.transform.position.y + 5, inside_camera_position.position.z);

                GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
                player = newPlayer;

                // Aplica força ao novo player
                Rigidbody player_rb = newPlayer.GetComponent<Rigidbody>();
                if (player_rb != null)
                {
                    player_rb.AddForce(Vector3.up * 10, ForceMode.Impulse);
                    player_rb.AddForce(exit_jet_position.forward * currentSpeed / 2, ForceMode.Impulse);
                }
            }
            else
            {
                Debug.LogError("PlayerPrefab não está atribuído no Jet!");
            }

        }


        jet_camera.enabled = false;
        jet_audio_listener.enabled = false;
        TurbineSmoke.SetActive(false);
        arms.SetActive(false);
        is_in_jet = false;
    }
}