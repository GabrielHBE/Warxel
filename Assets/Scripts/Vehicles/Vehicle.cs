using UnityEngine;
using VoxelDestructionPro.VoxelObjects;

public class Vehicle : MonoBehaviour
{

    [Header("Global vehicle configuration")]
    public bool is_in_vehicle;
    protected bool start_engine;
    [SerializeField] public Vector3 forwardReference;
    protected float minFov;
    protected Material hud_material;
    protected GameObject player;
    protected float acceleration;
    public bool ignore_damage;
    public bool used_locking_countermeasure;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Countermeasures countermeasures;
    [SerializeField] protected GameObject hud;
    [SerializeField] protected Color hud__color;
    [SerializeField] protected float mouseSensitivity = 2f;
    [SerializeField] protected Transform exit_vehicle_position;
    [SerializeField] protected AudioDistortionFilter distortion;
    [SerializeField] protected AudioSource crash_sound;
    public Camera vehicle_camera;
    public AudioListener vehicle_audio_listener;
    protected float hp;
    protected float resistance;
    [SerializeField] protected GameObject crash_explosion;
    [SerializeField] protected GameObject ground_explosion;

    protected virtual void VehicleStart()
    {
        if (countermeasures != null) countermeasures.SetVehicle(this);

        hud.SetActive(false);
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        start_engine = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    public virtual void EnterVehicle(GameObject _player) { }

    protected void ExitHevicle()
    {
        Quaternion spawnRotation = new Quaternion(0, exit_vehicle_position.transform.rotation.y, 0, exit_vehicle_position.transform.rotation.w);
        player.transform.position = exit_vehicle_position.position;
        player.transform.rotation = spawnRotation;
    }

    protected virtual void Explode(Collider[] colliders, ContactPoint contact, Collision collision, float explosionForce)
    {
        GameObject explosion_effect = null;


        if (collision.gameObject.layer == LayerMask.NameToLayer("Voxel"))
        {
            bool do_once = true;

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

                    do_once = false;
                }

                DynamicVoxelObj vox = colliders[i].GetComponentInParent<DynamicVoxelObj>();

                if (vox == null)
                {
                    continue;
                }

                vox.AddDestruction_Sphere(contact.point, explosionForce);

            }

            Mod_DestroyAfterAll mod_DestroyAfterAll = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
            mod_DestroyAfterAll?.StartCoroutine(mod_DestroyAfterAll.FallUpperVoxels(explosionForce, contact.point, true));
        }


        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            explosion_effect = Instantiate(ground_explosion, contact.point + contact.normal, Quaternion.LookRotation(contact.normal));
            explosion_effect.transform.localScale *= explosionForce / 10;
        }
        else
        {
            explosion_effect = Instantiate(crash_explosion, contact.point + contact.normal, Quaternion.LookRotation(contact.normal));
            explosion_effect.transform.localScale *= explosionForce / 10;
        }


        if (explosion_effect != null)
        {
            AudioSource crash_sound_obj = explosion_effect.AddComponent<AudioSource>();

            crash_sound_obj.clip = crash_sound.clip;
            crash_sound_obj.volume = crash_sound.volume;
            crash_sound_obj.pitch = crash_sound.pitch;
            crash_sound_obj.loop = crash_sound.loop;
            crash_sound_obj.spatialBlend = crash_sound.spatialBlend;
            crash_sound_obj.maxDistance = crash_sound.maxDistance;
            crash_sound_obj.rolloffMode = AudioRolloffMode.Custom;

            crash_sound_obj.Play();

            Destroy(gameObject);
        }
    }

    public virtual void Damage(float damage)
    {
        if (ignore_damage) return;

        hp -= damage - (resistance / 100);
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void Start_Stop_Engine() { }

    protected virtual void CameraController() { }

    protected virtual void Switch_weapon() { }

    protected virtual void UseCountermeasure()
    {
        if (countermeasures != null)
        {
            countermeasures.UseCountermeasure();
        }
    }
    protected virtual void UpdateHUD() { }


    public GameObject GetGameObject() { return transform.gameObject; }

    public Transform GetTransform() { return transform; }

    public Vector3 GetLocalPosition() { return transform.localPosition; }

    public Vector3 GetPosition() { return transform.position; }

    public Quaternion GetLocalRotation() { return transform.localRotation; }

    public Quaternion GetRotation() { return transform.rotation; }

}