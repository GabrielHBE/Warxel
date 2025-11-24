using System;
using Unity.VisualScripting;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Jdam : MonoBehaviour, JetUpgrades
{
    [Header("Properties")]
    [SerializeField] private float damage;

    [Header("Instances")]
    [SerializeField] private Jet jet;
    [SerializeField] private float fall_speed;
    [SerializeField] private GameObject landingIndicator_prefab;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject explosion_effect;
    [SerializeField] private MeshCollider mesh_collider;


    [Header("Sounds")]
    [SerializeField] private AudioSource explosion_sound;
    [SerializeField] private AudioSource drop_bomb_sound;

    [Header("Collider")]
    [SerializeField] private VoxCollider voxCollider;

    private bool isShot = false;
    private bool is_active;

    private float lifetime = 10;


    void Start()
    {
        jet = GetComponentInParent<Jet>();
        rb.isKinematic = true;

    }

    void Update()
    {
        if (isShot)
        {
            lifetime -=Time.deltaTime;
            if (lifetime <= 0)
            {
                Explode();
            }
        }
    }


    public void CreateSound(AudioSource sound)
    {
        GameObject soundObject = new GameObject("JDAMSound");
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

    public void Shoot()
    {
        if (isShot) return;

        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        transform.SetParent(null);
        // PEGAR O MOMENTUM DO JET
        Rigidbody jetRb = jet.GetComponent<Rigidbody>();
        if (jetRb != null)
        {
            rb.linearVelocity = jetRb.linearVelocity;
        }

        isShot = true;

    }


    public void SetActive(bool active)
    {
        is_active = active;

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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == jet.gameObject) return;

        Explode();
    }

    public GameObject GetGameObject()
    {
        try
        {
            return gameObject;
        }
        catch (Exception)
        {
            return null;
        }
    }

}