using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Agm : MonoBehaviour, JetUpgrades
{
    [SerializeField] private VoxCollider voxCollider;
    [SerializeField] private float damage;
    [SerializeField] private float maneuverability;
    [SerializeField] private AudioSource explosion_sound;
    [SerializeField] private GameObject explosion_effect;
    private Vehicle target_vehicle;
    [HideInInspector] public Vehicle parent_vehicle;
    private bool can_follow_target = false;

    private Transform target_transform;

    private bool do_once = true;

    void Update()
    {
        if (!can_follow_target) return;

        if (target_vehicle.used_locking_countermeasure == false)
        {

            target_transform = target_vehicle.GetGameObject().transform;

        }
        else
        {

            if (do_once)
            {
                target_transform.position = new Vector3(target_transform.position.x + Random.Range(0,100), target_transform.position.y + Random.Range(0,100), target_transform.position.z + Random.Range(0,100));
                do_once=false;
            }

            
        }

        transform.LookAt(target_transform);

        transform.position = Vector3.Lerp(transform.position, target_transform.position, maneuverability);

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == parent_vehicle.GetGameObject())
            return;


        Explode();

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

        if (explosion_effect != null)
        {
            GameObject explosion = Instantiate(explosion_effect, transform.position, Quaternion.identity);
            explosion.transform.localScale *= 2;
            CreateSound(explosion_sound);
        }


        Destroy(gameObject);
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void Shoot()
    {
        can_follow_target = true;
    }
    public void SetVehicle(Vehicle vehicle)
    {
        target_vehicle = vehicle;
    }
}
