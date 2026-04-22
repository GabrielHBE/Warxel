using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;

public class TankMainShell : NetworkBehaviour
{
    [SerializeField] private Collider shell_collider;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private VoxCollider voxCollider;
    [SerializeField] private GameObject explosion_efect;
    [SerializeField] private AudioSource explosion_sound;
    public float reload_time;
    public float infantary_damage;
    public float vehicle_damage;
    public float travel_speed;
    public float fall_multiplier;
    public float recoil_force;

    private GameObject ignoreCollision;
    [HideInInspector]public Sprite image_hud;

    void OnCollisionEnter(Collision collision)
    {

        if(ignoreCollision == collision.gameObject) return;

        print(collision.gameObject.name);

        Vector3 contact_point = collision.contacts[0].point;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            Vehicle vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

            if (vehicle != null)
            {

                float target_resistance = vehicle.GetResistance();
                float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);

                vehicle.RequestDamage(vehicle_damage);

                if (vehicle.vehicle_destroyed.Value)
                {
                    EliminationMarker.Instance.InstantiateVehicleImage();
                }
            }


        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                PlayerProperties player_properties = player.GetComponent<PlayerProperties>();
                player.RequestDamage(infantary_damage);

                float target_resistance = player.GetResistance();
                float final_actual_damage = infantary_damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);

                if (player_properties.is_dead.Value)
                {
                    EliminationMarker.Instance.InstantiateVehicleImage();
                }
            }


        }
        else
        {
            voxCollider.SphereExplosion(contact_point, infantary_damage, vehicle_damage);
        }

        shell_collider.enabled = false;

        explosion_sound.transform.SetParent(null);
        explosion_sound.GetComponent<AudioDistanceController>().StartGrowth();

        SpawnExplosionEffect(contact_point);

        Mod_DestroyAfterAll mod_DestroyAfterAll = collision.gameObject.GetComponentInParent<Mod_DestroyAfterAll>();
        mod_DestroyAfterAll?.StartCoroutine(mod_DestroyAfterAll.FallUpperVoxels(voxCollider.destructionRadius, contact_point, true));


        RequestDespawn();
    }
    

    [ServerRpc]
    private void SpawnExplosionEffect(Vector3 pos)
    {
        GameObject explosion = Instantiate(explosion_efect, pos, Quaternion.identity);
        Spawn(explosion);
    }

    void FixedUpdate()
    {
        if(!IsOwner) return;

        rb.AddForce(Vector3.down * fall_multiplier);
    }
    [ObserversRpc]
    public void Shoot(Vector3 direction, GameObject ignoreCollision = null)
    {
        this.ignoreCollision = ignoreCollision;
        rb.useGravity = false;
        rb.linearVelocity = direction * travel_speed;
    }

    [ServerRpc]
    private void RequestDespawn()
    {
        if (IsSpawned)
        {
            Despawn(gameObject);
        }
    }
}
