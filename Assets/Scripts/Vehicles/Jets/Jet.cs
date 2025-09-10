using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


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

    [Header("Positions")]
    public Transform exit_jet_position;

    [Header("Instances")]
    public GameObject player;
    private Vignette vignette;
    public Volume volume;
    public Image blackImage;
    public GameObject TurbineSmoke;
    public GameObject glass;

    [Header("Sound")]
    public AudioSource tinnitus;
    public AudioListener jet_audio_listener;
    public AudioListener player_audio_listener;
    public AudioDistortionFilter distortion;

    [HideInInspector] public float mouseX, mouseY;
    public JetProperties jetProperties;
    public Transform forwardReference;
    private float acceleration; // Velocidade aumenta 1 unidade por segundo
    public float currentSpeed = 0f;
    private bool start_engine;
    [HideInInspector] public float moveForward;
    public bool is_ejecting;
    [HideInInspector] public int current_camera;
    private float minFov;
    float next_time_to_fire = 0;
    float rotation_increaser;
    float pitch_increaser;

    float overheat;
    bool overheated;
    float passout_timer;
    bool passout;
    public Rigidbody rb;
    Quaternion initial_camera_rotation;
    [HideInInspector] public float lean_value;


    void Start()
    {

        TurbineSmoke.SetActive(false);
        jet_audio_listener.enabled = false;
        blackImage.enabled = false;
        start_engine = false;
        volume.profile.TryGet(out vignette);

        is_in_jet = false;

        current_camera = 1;
        //rb = GetComponent<Rigidbody>();
        //jetProperties = GetComponent<JetProperties>();

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


            is_in_jet = true;
            player = _player;

            if (is_in_jet)
            {
                player.SetActive(false);
                jet_audio_listener.enabled = true;
                minFov = jet_camera.fieldOfView;
                jet_camera.enabled = true;
                TurbineSmoke.SetActive(start_engine);
            }

        }
    }


    void Update()
    {

        if (is_in_jet)
        {
            if (Input.GetKeyDown(enter_jet_key))
            {
                is_in_jet =false;
                player.SetActive(true);
                jet_camera.enabled = false;
                jet_audio_listener.enabled = false;
                TurbineSmoke.SetActive(false);

                // Reposiciona o player na posição de saída
                player.transform.position = exit_jet_position.position;
                player.transform.rotation = exit_jet_position.rotation;
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

            currentSpeed -= acceleration * Time.deltaTime * 1.5f; // Diminui a velocidade com o tempo

        }

        currentSpeed = Math.Clamp(currentSpeed, 0, jetProperties.max_speed);
        transform.position += forwardReference.forward * currentSpeed * Time.deltaTime;
        //rb.linearVelocity += (forwardReference.forward * currentSpeed * Time.deltaTime) / 10;

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
                Transform bulletObj = Instantiate(jetProperties.bullefPref, jetProperties.barrel.transform.position, jetProperties.barrel.transform.rotation);
                Destroy(bulletObj.gameObject, 10f);

                bulletObj.GetComponent<Bullet>().SetDirection(jetProperties.barrel.transform.forward, jetProperties.muzzle_velocity, jetProperties.bullet_drop);
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
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (Input.GetKey(pitch_up_key) && passout == false)
        {
            mouseY += jetProperties.pitch_value;
        }

        if (Input.GetKey(pitch_down_key) && passout == false)
        {
            mouseY -= jetProperties.pitch_value;
        }


        if (jetProperties.invertY)
        {
            mouseY *= -1;
        }

        mouseX = Math.Clamp(mouseX, -jetProperties.rotation_value, jetProperties.rotation_value);
        mouseY = Math.Clamp(mouseY, -jetProperties.pitch_value, jetProperties.pitch_value);

        if (mouseY != 0)
        {
            pitch_increaser += Time.deltaTime * jetProperties.maneuverability;

        }
        else
        {
            pitch_increaser = 0;

        }

        if (mouseX != 0)
        {
            rotation_increaser += Time.deltaTime * jetProperties.maneuverability;
        }
        else
        {
            rotation_increaser = 0;
        }

        pitch_increaser = Math.Clamp(pitch_increaser, 0, 1);
        rotation_increaser = Math.Clamp(rotation_increaser, 0, 1);



        if (currentSpeed >= 30)
        {
            transform.Rotate(Vector3.back * mouseY * currentSpeed / jetProperties.max_speed * pitch_increaser * jetProperties.pitch_value, Space.Self);
            transform.Rotate(Vector3.right * mouseX * currentSpeed / jetProperties.max_speed * rotation_increaser * jetProperties.rotation_value, Space.Self);
        }


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

        transform.Rotate(Vector3.up * lean_value * currentSpeed / jetProperties.max_speed, Space.Self);

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
            jetProperties.interior_turbine.pitch += 0.0001f;

            currentSpeed += acceleration * Time.deltaTime; // Aumenta a velocidade com o tempo



        }
        else if (start_engine && moveForward < 0 && passout == false)
        {
            jetProperties.interior_turbine.pitch -= 0.0001f;

            currentSpeed -= acceleration * Time.deltaTime * 1.5f; // Diminui a velocidade com o tempo


        }

        // Gravidade mínima e máxima (ajuste conforme necessário)
        float minGravity = 0f;
        float maxGravity = -2f;

        // Calcula a gravidade proporcional à velocidade atual
        float speedPercent = currentSpeed / jetProperties.max_speed; // de 0 a 1
        float gravityY = Mathf.Lerp(maxGravity, minGravity, speedPercent); // diminui com a velocidade

        Physics.gravity = new Vector3(0, gravityY, 0);

    }


}
