using System.Collections;
using Photon.Realtime;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Bullet : MonoBehaviour
{
    public GameObject bullet_hole;
    private Rigidbody rb;
    private VoxCollider voxCollider;
    private float bulletDropMultiplier;
    private float damage;
    private float damage_dropoff;
    private float damage_dropoff_timer;


    [Header("Sounds")]
    public GameObject ricochet_sound;
    private RicochetSounds ricochetSounds;
    private Vector3 original_position;

    [Header("HitEffects")]
    [SerializeField] private GameObject glass_hit_effect;
    [SerializeField] private GameObject metal_hit_effect;
    [SerializeField] private GameObject wood_hit_effect;
    [SerializeField] private GameObject concrete_hit_effect;
    [SerializeField] private GameObject sand_hit_effect;
    [SerializeField] private GameObject dirt_hit_effect;
    [SerializeField] private GameObject softbody_hit_effect;


    bool did_ricochet;
    GameObject custom_hit_effect_instance;
    float timer;

    void Awake()
    {
        voxCollider = GetComponent<VoxCollider>();
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void CreateBullet(Vector3 direction, float speed, float dropMultiplier, float dmg, float dmg_dropoff, float dmg_dropoff_timer, float destruction_force, GameObject hit_effect = null)
    {
        GetComponent<MeshRenderer>().enabled = false;

        voxCollider.destructionRadius = destruction_force;
        did_ricochet = false;
        damage = dmg;
        damage_dropoff = dmg_dropoff;
        damage_dropoff_timer = dmg_dropoff_timer;
        custom_hit_effect_instance = hit_effect;

        SetDirection(direction, speed, dropMultiplier);
    }

    public void SetDirection(Vector3 direction, float speed, float dropMultiplier)
    {
        original_position = transform.localPosition;
        rb.useGravity = false;
        rb.linearVelocity = direction * speed;
        bulletDropMultiplier = dropMultiplier;
    }

    void Update()
    {
        // Simula a queda da bala sem alterar a gravidade global
        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);
        if (rb.linearVelocity.magnitude < 10)
        {
            Destroy(gameObject);
        }

        timer += Time.deltaTime;
        if (timer >= damage_dropoff_timer)
        {
            damage -= damage_dropoff;
            timer = 0;
        }

        if (timer >= 0.05f)
        {
            GetComponent<MeshRenderer>().enabled = true;
        }


    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Bullet"))
        {


            if (did_ricochet)
            {
                Destroy(gameObject);
                return;
            }


            if (collision.contacts.Length > 0)
            {

                ContactPoint contact = collision.contacts[0];
                float calculated_position = Vector3.Distance(original_position, contact.point);

                if (calculated_position > 3)
                {

                    if (collision.gameObject.CompareTag("Player"))
                    {
                        PlayerProperties player = collision.gameObject.GetComponent<PlayerProperties>();
                        player.Damage(damage);

                    }
                    else if (collision.gameObject.CompareTag("Voxel"))
                    {
                        DynamicVoxelObj vox = collision.transform.GetComponentInParent<DynamicVoxelObj>();
                        if (vox != null)
                        {
                            if (vox.destruction_multiplier > 0)
                            {
                                voxCollider.destructionRadius *= vox.destruction_multiplier;
                            }
                        }

                        HitEffects(contact.point, contact.normal, vox);

                        if (voxCollider.destructionRadius > 2)
                        {

                            bool do_once = true;
                            Collider[] colliders = Physics.OverlapSphere(contact.point, voxCollider.destructionRadius);

                            for (int i = 0; i < colliders.Length; i++)
                            {
                                PlayerProperties playerProps = colliders[i].GetComponentInParent<PlayerProperties>();

                                if (do_once && playerProps != null)
                                {
                                    Debug.Log("Colidiu com Player");
                                    float distance = Vector3.Distance(transform.position, playerProps.transform.position);
                                    float damage_distance = Mathf.Clamp(voxCollider.destructionRadius * (1 - distance / 10), 0, voxCollider.destructionRadius);
                                    playerProps.Damage(damage * damage_distance);

                                    CameraShake cameraShake = playerProps.GetComponentInChildren<CameraShake>();
                                    if (cameraShake != null)
                                    {
                                        cameraShake.StartCoroutine(cameraShake.ExplosionShake(damage_distance / 10, 1f));
                                    }
                                    else
                                    {
                                        Debug.LogWarning("CameraShake não encontrado!");
                                    }

                                    do_once = false;
                                }

                                if (vox == null)
                                    continue;

                                vox.AddDestruction_Sphere(contact.point, voxCollider.destructionRadius);

                            }
                        }
                        else
                        {
                            voxCollider.Collide(collision);
                        }


                    }

                    Destroy(gameObject);

                }
                else
                {
                    Ricochet(contact.point, contact.normal);
                }

            }

            //Debug.Log(collision.gameObject.name);

        }


    }

    void Ricochet(Vector3 position, Vector3 normal)
    {
        did_ricochet = true;
        rb.linearVelocity /= 1.5f;
        GameObject ricochet = Instantiate(ricochet_sound, position, Quaternion.LookRotation(normal));
        ricochetSounds = ricochet.GetComponent<RicochetSounds>();
        ricochetSounds.Play();

        Destroy(ricochet, 2f);
    }

    void HitEffects(Vector3 position, Vector3 normal, DynamicVoxelObj vox)
    {
        if (custom_hit_effect_instance != null)
        {
            Instantiate(custom_hit_effect_instance, position + normal * 0.01f, Quaternion.LookRotation(normal));
            return;
        }

        switch (vox.material)
        {
            case "Glass":
                Transform child = vox.transform.GetChild(0);
                Material mat = child.GetComponent<MeshRenderer>().material;

                var particleRenderer = glass_hit_effect.GetComponent<ParticleSystemRenderer>();
                particleRenderer.material = mat;

                Instantiate(glass_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            case "Metal":
                Instantiate(metal_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            case "Wood":
                Instantiate(wood_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            case "Concrete":
                Instantiate(concrete_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            case "Sand":
                Instantiate(sand_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            case "Dirt":
                Instantiate(dirt_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            case "SoftBody":
                Instantiate(softbody_hit_effect, position + normal * 0.01f, Quaternion.LookRotation(normal));
                break;
            default:
                break;
        }

    }

}
