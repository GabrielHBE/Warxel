
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;
public abstract class Missiles : NetworkBehaviour
{
    public MeshRenderer mesh;
    [SerializeField] protected float infantary_damage;
    [SerializeField] protected float vehicle_damage;
    [SerializeField] protected float time_to_explode;
    [SerializeField] protected Collider missile_collider;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected float travel_speed;
    [SerializeField] protected AudioSource explosion_sound;
    [SerializeField] protected GameObject explosion_effect;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected ParticleSystem trail;
    [SerializeField] protected AudioSource shoot_sound;
    protected float speed;

    public GameObject parent_gameobject;

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
        if (!IsSpawned || hasExploded) return;

        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        trail.transform.SetParent(null, true);
        trail.transform.localScale = Vector3.one;
        Destroy(trail.gameObject, trail.main.duration + trail.main.startLifetime.constantMax);

        if (collision.gameObject == parent_gameobject) return;

        hasExploded = true;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            Vehicle vehicle = collision.gameObject.GetComponent<Vehicle>() ?? collision.gameObject.GetComponentInParent<Vehicle>();
            if (vehicle != null)
            {
                if (!vehicle.vehicle_destroyed.Value)
                {
                    float target_resistance = vehicle.GetResistance();
                    float final_actual_damage = vehicle_damage * ((100f - target_resistance) / 100f);

                    vehicle.RequestDamage(vehicle_damage);

                    if (vehicle.vehicle_destroyed.Value)
                    {
                        EliminationMarker.Instance.InstantiateVehicleImage();
                    }
                    else
                    {
                        DamageMarker.Instance.UpdateDamage(final_actual_damage);
                    }
                }
            }

        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>() ?? collision.gameObject.GetComponentInParent<PlayerController>();
            PlayerProperties player_properties = player.GetComponent<PlayerProperties>();
            player.RequestDamage(infantary_damage);
            float target_resistance = player.GetResistance();
            float final_actual_damage = infantary_damage * ((100f - target_resistance) / 100f);


            if (player_properties.is_dead.Value)
            {
                AccountManager.Instance.status.AddKill();
                EliminationMarker.Instance.InstantiateVehicleImage();
            }
            else
            {
                DamageMarker.Instance.UpdateDamage(final_actual_damage);
            }

        }
        else
        {
            voxCollider.SphereExplosion(collision.contacts[0].point, infantary_damage, vehicle_damage);
            missile_collider.enabled = false;
        }

        Explode(collision.contacts[0].point);


    }
    #endregion

    #region Collision / Explosion
    [ServerRpc]
    protected void Explode(Vector3 contact_point)
    {
        // No servidor, verificar novamente se já não foi processado
        if (!IsSpawned) return;

        CmdPlayExplosionSound();
        if (explosion_effect != null)
        {
            GameObject explosion = Instantiate(explosion_effect, contact_point, Quaternion.identity);
            Spawn(explosion);
            explosion.transform.localScale *= 2;
        }
        RequestDespawn();
    }
    
    [ObserversRpc]
    private void CmdPlayExplosionSound()
    {
        AudioDistanceController audioDistanceController = explosion_sound.GetComponent<AudioDistanceController>();
        audioDistanceController.StartGrowth();
    }

    protected virtual void CreateSound(AudioSource sound)
    {
        AudioDistanceController AudioDistanceController = sound.GetComponent<AudioDistanceController>();

        if (AudioDistanceController != null)
        {
            AudioDistanceController.StartGrowth();
            return;
        }

        sound.gameObject.AddComponent<CreateSoundDesroyManager>().Initialize(sound, sound.clip.length);
    }

    private class CreateSoundDesroyManager : MonoBehaviour
    {
        public void Initialize(AudioSource audio, float DestroyTimer)
        {
            transform.SetParent(null);
            audio.Play();
            Destroy(gameObject, DestroyTimer);
        }
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