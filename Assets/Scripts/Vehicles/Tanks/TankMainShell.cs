using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;

public class TankMainShell : NetworkBehaviour, IsVehicleCustomizationPart, IVehicleArmory
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
    public Sprite image_hud;

    void OnCollisionEnter(Collision collision)
    {

        if(ignoreCollision == collision.gameObject) return;

        GameObject ignoreGoInExplosion = null;

        Vector3 contact_point = collision.contacts[0].point;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            Vehicle vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

            if (vehicle != null)
            {
                ignoreGoInExplosion = vehicle.gameObject;
                
                string[] occupantNames = vehicle.GetOccupantNames();

                float target_resistance = vehicle.GetResistance();
                float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);

                vehicle.RequestDamage(vehicle_damage);

                if (vehicle.vehicle_destroyed.Value)
                {
                    ProcessKill.ProcessVehicleKill(gameObject, occupantNames);
                }
            }

        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                ignoreGoInExplosion = player.gameObject;
                PlayerProperties player_properties = player.GetComponent<PlayerProperties>();
                player.RequestDamage(infantary_damage);

                float target_resistance = player.GetResistance();
                float final_actual_damage = infantary_damage * ((100f - target_resistance) / 100f);

                DamageMarker.Instance.UpdateDamage(final_actual_damage);

                if (player_properties.is_dead.Value)
                {
                    ProcessKill.ProcessInfantryKill(gameObject, false, player_properties.player_name.Value);
                }
            }

        }

        voxCollider.SphereExplosion(contact_point, infantary_damage, vehicle_damage, ignoreGoInExplosion);

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

    #region Interface implementations
    //IsVehicleCustomizationPart
    public void Activate()
    {
        throw new System.NotImplementedException();
    }

    public void Deactivate()
    {
        throw new System.NotImplementedException();
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        throw new System.NotImplementedException();
    }

    public string GetCustomizationPartName()
    {
        throw new System.NotImplementedException();
    }

    //IVehicleArmory
    public void Shoot()
    {
        throw new System.NotImplementedException();
    }

    public Sprite GetArmoryIcon()
    {
        throw new System.NotImplementedException();
    }

    public void ActivateArmory()
    {
        throw new System.NotImplementedException();
    }

    public string GetCurrentAmmo()
    {
        throw new System.NotImplementedException();
    }

    public float GetHeatingLevel()
    {
        throw new System.NotImplementedException();
    }

    public void DeactivateArmory()
    {
        throw new System.NotImplementedException();
    }

    public float GetMaxOverheat()
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
