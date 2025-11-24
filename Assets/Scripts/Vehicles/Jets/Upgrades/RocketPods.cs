using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class RocketPods : MonoBehaviour, JetUpgrades
{
    [Header("Properties")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float damage_dropoff_timer;
    [SerializeField] private float damage_dropoff;
    [SerializeField] private float bulletDropMultiplier;

    [Header("Instances")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject explosion_effect;
    [SerializeField] private GameObject trail;
    public MeshCollider mesh_collider;
    public Vehicle vehicle;

    [Header("Sounds")]
    [SerializeField] private AudioSource explosion_sound;
    [SerializeField] private AudioSource shoot_sound;

    [Header("Collider")]
    [SerializeField] private VoxCollider voxCollider;


    [HideInInspector] public bool did_shoot = false;


    float timer;


    void Start()
    {
        trail.SetActive(false);
    }



    void FixedUpdate()
    {

        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);

        timer += Time.deltaTime;
        if (timer >= damage_dropoff_timer)
        {
            damage -= damage_dropoff;
            timer = 0;
        }


    }


    public bool CanShoot()
    {
        return true;
    }

    public void Explode()
    {
        bool do_once = true;
        Collider[] colliders = Physics.OverlapSphere(transform.position, voxCollider.destructionRadius);

        for (int i = 0; i < colliders.Length; i++)
        {
            PlayerProperties player = colliders[i].GetComponentInParent<PlayerProperties>();
            Vehicle vehicle = colliders[i].GetComponentInParent<Vehicle>();

            if (do_once && player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                float damage = Mathf.Clamp(voxCollider.destructionRadius * (1 - (distance / voxCollider.destructionRadius)), 0, voxCollider.destructionRadius);
                player.Damage(damage * this.damage);

                CameraShake cameraShake = player.GetComponentInChildren<CameraShake>();
                if (cameraShake != null)
                {
                    cameraShake.StartCoroutine(cameraShake.ExplosionShake(damage / 10, 1f));
                }

                do_once = false;
            }

            if (do_once && vehicle != null)
            {
                float distance = Vector3.Distance(transform.position, vehicle.GetPosition());
                float damage = Mathf.Clamp(voxCollider.destructionRadius * (1 - (distance / voxCollider.destructionRadius)), 0, voxCollider.destructionRadius);
                vehicle.Damage(damage * this.damage);

                do_once = false;
            }

            DynamicVoxelObj vox = colliders[i].GetComponentInParent<DynamicVoxelObj>();

            if (vox == null)
                continue;

            vox.AddDestruction_Sphere(transform.position, voxCollider.destructionRadius);
        }

        GameObject explosion = Instantiate(explosion_effect, transform.position, Quaternion.identity);
        explosion.transform.localScale *= 2;
        CreateSound(explosion_sound);

        Destroy(gameObject);
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void Shoot()
    {
        if (did_shoot) return;

        CreateSound(shoot_sound);

        transform.SetParent(null);
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            did_shoot = true;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = transform.right * speed;
        }

        trail.SetActive(true);

    }


    public void CreateSound(AudioSource sound)
    {

        GameObject soundObject = new GameObject("TemporarySoundObject");
        soundObject.transform.SetParent(null);

        soundObject.transform.position = transform.position;
        soundObject.transform.rotation = transform.rotation;

        AudioSource tempAudioSource = soundObject.AddComponent<AudioSource>();

        tempAudioSource.clip = sound.clip;
        tempAudioSource.volume = sound.volume;
        tempAudioSource.pitch = sound.pitch;
        tempAudioSource.spatialBlend = sound.spatialBlend;
        tempAudioSource.maxDistance = sound.maxDistance;
        tempAudioSource.rolloffMode = sound.rolloffMode;

        tempAudioSource.Play();

        Destroy(soundObject, tempAudioSource.clip.length);


    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == vehicle.GetGameObject() || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            return;

        // Se estiver usando camadas, você pode adicionar uma verificação adicional:
        // if (collision.gameObject.layer == LayerMask.NameToLayer("Jet")) return;

        Debug.Log(collision.gameObject);
        Explode();

    }

}
