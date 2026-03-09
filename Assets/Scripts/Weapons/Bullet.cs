using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class Bullet : MonoBehaviour
{
    [Header("Trail")]
    [SerializeField] private float delay = 0.5f;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private VoxCollider voxCollider;
    private float bulletDropMultiplier;
    private float damage;
    private float damage_dropoff;
    private float damage_dropoff_timer;
    private float minimum_damage;
    private float hs_multiplier;
    private bool can_damage_vehicles;
    private float vehicle_damage;


    [Header("Sounds")]
    public GameObject ricochet_sound;
    private RicochetSounds ricochetSounds;
    private Vector3 original_position;
    private AudioSource hit_sound;


    [Header("HitEffects")]
    [SerializeField] private GameObject glass_hit_effect;
    [SerializeField] private GameObject metal_hit_effect;
    [SerializeField] private GameObject wood_hit_effect;
    [SerializeField] private GameObject concrete_hit_effect;
    [SerializeField] private GameObject sand_hit_effect;
    [SerializeField] private GameObject dirt_hit_effect;
    [SerializeField] private GameObject softbody_hit_effect;


    private WeaponProperties weaponProperties;
    private Vehicle vehicle;
    private GameObject igore_hit_gameobject;


    bool did_ricochet;
    GameObject custom_hit_effect_instance;
    float timer;

    public void CreateBullet(Vector3 direction, float speed, float dropMultiplier, float dmg, float dmg_dropoff, float dmg_dropoff_timer, float destruction_force, float minimum_damage, float hs_multiplier, float size, float delay, bool can_damage_vehicles, float vehicle_damage, GameObject hit_effect = null, AudioSource hit_sound = null, WeaponProperties weaponProperties = null, Vehicle vehicle = null, GameObject igore_hit_gameobject = null)
    {

        voxCollider.destructionRadius = destruction_force;
        did_ricochet = false;
        damage = dmg;
        damage_dropoff = dmg_dropoff;
        damage_dropoff_timer = dmg_dropoff_timer;
        custom_hit_effect_instance = hit_effect;
        this.minimum_damage = minimum_damage;
        this.hs_multiplier = hs_multiplier;
        this.hit_sound = hit_sound;
        this.delay = delay;
        this.can_damage_vehicles = can_damage_vehicles;
        this.vehicle_damage = vehicle_damage;
        this.weaponProperties = weaponProperties;
        this.vehicle = vehicle;
        this.igore_hit_gameobject = igore_hit_gameobject;

        SetDirection(direction, speed, dropMultiplier);

        if (size != 0) transform.localScale *= size;

        StartCoroutine(EnableTrailAfterDelay());
    }

    public void SetDirection(Vector3 direction, float speed, float dropMultiplier)
    {
        original_position = transform.localPosition;
        rb.useGravity = false;
        rb.linearVelocity = direction * speed;
        bulletDropMultiplier = dropMultiplier;
    }

    IEnumerator EnableTrailAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        trail.enabled = true;
    }

    void FixedUpdate()
    {
        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);
    }

    void Update()
    {

        if (damage > minimum_damage)
        {
            timer += Time.deltaTime;
            if (timer >= damage_dropoff_timer)
            {
                damage -= damage_dropoff;
                vehicle_damage -= damage_dropoff;
                timer = 0;
            }
        }

    }

    void OnCollisionEnter(Collision collision)
    {

        if (igore_hit_gameobject != null)
        {
            if (collision.gameObject == igore_hit_gameobject)
            {
                return;
            }
        }

        if (did_ricochet)
        {
            Destroy(gameObject);
            return;
        }


        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            float calculated_distance = Vector3.Distance(original_position, contact.point);

            if (calculated_distance > 3)
            {

                if (collision.gameObject.layer == LayerMask.NameToLayer("Voxel"))
                {

                    HitEffects(contact.point, contact.normal, collision.transform.GetComponentInParent<DynamicVoxelObj>());

                    if (voxCollider.destructionRadius > 2)
                    {

                        voxCollider.SphereExplosion(contact.point, damage);
                    }
                    else
                    {
                        voxCollider.Collide(collision);
                    }


                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    voxCollider.SphereExplosion(contact.point, damage);
                    HitEffects(contact.point, contact.normal, null);
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle") && can_damage_vehicles)
                {
                    HitEffects(contact.point, contact.normal, null);

                    HitMarker.Instance.CreateVehicleMarker();
                    Vehicle hit_vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

                    //print(vehicle_damage);
                    if (hit_vehicle != null)
                    {
                        if (!hit_vehicle.vehicle_destroyed)
                        {
                            float damage_dealt = hit_vehicle.Damage(vehicle_damage);
                            if (damage_dealt != 0)
                            {
                                if (vehicle != null) vehicle.UpgradeVehicleLevel(damage_dealt / 10);
                                DamageMarker.Instance.UpdateDamage(damage_dealt);
                            }

                            if (hit_vehicle.vehicle_destroyed)
                            {
                                AccountManager.Instance.AddPointsToLevelUp(10);
                                if (weaponProperties != null) weaponProperties.UpgradeWeaponLevel(damage_dealt / 10);
                                EliminationMarker.Instance.InstantiateVehicleImage();
                            }
                        }

                    }

                }


                if (hit_sound != null)
                {
                    AudioSource.PlayClipAtPoint(hit_sound.clip, contact.point);
                }

                Destroy(gameObject);

            }
            else
            {
                Ricochet(contact.point, contact.normal);
            }

        }



    }

    //Player Collision
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.tag == "OwnerPlayer")
        {
            return;
        }

        if (igore_hit_gameobject != null)
        {
            if (other.gameObject == igore_hit_gameobject)
            {
                return;
            }
        }

        bool hs_hit = false;

        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
        {
            PlayerController player = other.gameObject.GetComponentInParent<PlayerController>();
            float damage_dealt;

            if (other.gameObject.CompareTag("PlayerHead"))
            {
                //hitMarker.CreateHeadShotMarker();
                damage_dealt = damage * hs_multiplier;
                player.Damage(damage_dealt);
                hs_hit = true;
            }
            else if (other.gameObject.CompareTag("Arms and Legs"))
            {
                //hitMarker.CreateHeadShotMarker();
                damage_dealt = damage * 0.8f;
                player.Damage(damage_dealt);
            }
            else if (other.gameObject.CompareTag("Feet and Hands"))
            {
                //hitMarker.CreateHeadShotMarker();
                damage_dealt = damage * 0.5f;
                player.Damage(damage_dealt);
            }
            else
            {
                damage_dealt = damage;
                HitMarker.Instance.CreateBodyShotMarker();
                player.Damage(damage_dealt);

            }

            PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();
            weaponProperties.UpgradeWeaponLevel(damage_dealt / 10);

            if (playerProperties.is_dead)
            {
                AccountManager.Instance.status.AddKill();
                if (hs_hit) AccountManager.Instance.status.AddHeadShotKill();

                AccountManager.Instance.AddPointsToLevelUp(10);
            }

            if (damage_dealt != 0) DamageMarker.Instance.UpdateDamage(damage_dealt);

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


        if (vox != null)
        {
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

}
