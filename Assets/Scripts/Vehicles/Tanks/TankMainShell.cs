using UnityEngine;
using VoxelDestructionPro.Tools;

public class TankMainShell : MonoBehaviour
{
    public Sprite image_hud;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private VoxCollider voxCollider;
    [SerializeField] private GameObject explosion_efect;
    [SerializeField] private AudioSource explosion_sound;
    public float reload_time;
    public float damage;
    public float destruction_radious;
    public float travel_speed;
    public float fall_multiplier;
    public float recoil_force;

    private EliminationMarker eliminationMarker;
    private DamageMarker damageMarker;

    void Start()
    {
        eliminationMarker = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<EliminationMarker>();
        damageMarker = eliminationMarker.GetComponent<DamageMarker>();
    }


    void OnCollisionEnter(Collision collision)
    {
        Vector3 contact_point = collision.contacts[0].point;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            Vehicle vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

            if (vehicle != null)
            {
                
                damageMarker.UpdateDamage(vehicle.Damage(damage));
                if (vehicle.vehicle_destroyed)
                {
                    eliminationMarker.InstantiateVehicleImage();
                }
            }


        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                PlayerProperties player_properties = player.GetComponent<PlayerProperties>();
                player.Damage(damage);
                damageMarker.UpdateDamage(damage);
                if (player_properties.is_dead)
                {
                    eliminationMarker.InstantiateVehicleImage();
                }
            }


        }
        else
        {
            voxCollider.SphereExplosion(contact_point, damage);


        }

        explosion_sound.transform.SetParent(null);
        explosion_sound.GetComponent<AudioDistanceController>().StartGrowth();

        Instantiate(explosion_efect, explosion_sound.transform);
        Mod_DestroyAfterAll mod_DestroyAfterAll = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
        mod_DestroyAfterAll?.StartCoroutine(mod_DestroyAfterAll.FallUpperVoxels(voxCollider.destructionRadius, contact_point, true));


        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        rb.AddForce(Vector3.down * fall_multiplier);
    }


    public void Shoot(Vector3 direction)
    {
        rb.useGravity = false;
        rb.linearVelocity = direction * travel_speed;
    }
}
