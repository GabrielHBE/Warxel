
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;
public abstract class Missiles : NetworkBehaviour
{

    [Header("Damage")]
    [SerializeField] protected float infantary_damage;
    [SerializeField] protected float vehicle_damage;

    [Header("Properties")]
    [SerializeField] protected float time_to_explode;
    [SerializeField] protected float travel_speed;

    [Header("Sounds")]
    [SerializeField] protected AudioClip explosionSound;
    [SerializeField] protected SoundManager.SoundProperties explosionSoundProperties = SoundManager.SoundProperties.Default;
    [SerializeField] protected AudioClip shootSound;
    [SerializeField] protected SoundManager.SoundProperties shootSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Instances")]
    [SerializeField] protected Collider missile_collider;
    [SerializeField] protected VoxCollider voxCollider;
    public MeshRenderer mesh;
    [SerializeField] protected GameObject explosion_effect;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected ParticleSystem trail;
    public GameObject parent_gameobject;


    protected float speed;
    protected bool didShoot;
    protected bool hasExploded; // Nova variável para evitar execuções duplicadas

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        SetParentVehicle();
        if (!didShoot && rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    protected virtual void LateUpdate()
    {
        if (!didShoot && transform.parent != null)
        {
            transform.localPosition = Vector3.zero;
        }
    }

    protected virtual void Update() { }

    protected void DestroyTimer()
    {
        if (didShoot)
        {
            time_to_explode -= Time.deltaTime;

            if (time_to_explode <= 0) Explode(transform.position);
        }

    }


    protected virtual void FixedUpdate()
    {
        speed = rb.linearVelocity.magnitude;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!IsSpawned || hasExploded || collision.gameObject == parent_gameobject) return;

        GameObject ignoreGoInExplosion = null;

        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        trail.transform.SetParent(null, true);
        trail.transform.localScale = Vector3.one;
        Destroy(trail.gameObject, trail.main.duration + trail.main.startLifetime.constantMax);


        hasExploded = true;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            Vehicle vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();

            if (vehicle != null && !vehicle.vehicle_destroyed.Value)
            {
                ignoreGoInExplosion = vehicle.gameObject;

                string[] occupantNames = vehicle.GetOccupantNames();

                float target_resistance = vehicle.GetResistance();
                float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);
                vehicle.RequestDamage(vehicle_damage);

                if (vehicle.vehicle_destroyed.Value)
                    ProcessKill.ProcessVehicleKill(gameObject, occupantNames);
                else
                    DamageMarker.Instance?.UpdateDamage(final_actual_damage);
            }

        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
            PlayerProperties player_properties = player.GetComponent<PlayerProperties>();
            player.RequestDamage(infantary_damage);
            float target_resistance = player.GetResistance();
            float final_actual_damage = infantary_damage * ((100f - target_resistance) / 100f);

            ignoreGoInExplosion = player.gameObject;

            if (player_properties.is_dead.Value)
            {
                ProcessKill.ProcessInfantryKill(gameObject, false, player_properties.player_name.Value);
            }
            else
            {
                DamageMarker.Instance.UpdateDamage(final_actual_damage);
            }

        }

        missile_collider.enabled = false;

        SoundManager.Play3dSoundLocal(explosionSound, explosionSoundProperties, collision.contacts[0].point);

        voxCollider.SphereExplosion(collision.contacts[0].point, infantary_damage, vehicle_damage, ignoreGoInExplosion);
        Explode(collision.contacts[0].point);

    }
    #endregion

    #region Collision / Explosion
    protected void Explode(Vector3 contact_point)
    {
        if (explosion_effect != null)
        {
            GameObject explosion = Instantiate(explosion_effect, contact_point, Quaternion.identity);
            //Spawn(explosion);
            explosion.transform.localScale *= 2;
        }

        RequestDespawn();
    }


    #endregion

    #region Utility
    public virtual void Shoot(Vector3 direction) { }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    protected void SetParentVehicle()
    {
        Vehicle vehicle = GetComponentInParent<Vehicle>();
        if (vehicle != null)
        {
            voxCollider.parent_vehicle = vehicle;
        }
    }
    [ServerRpc]
    private void RequestDespawn()
    {

        if (IsSpawned)
        {
            Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
    #endregion
}