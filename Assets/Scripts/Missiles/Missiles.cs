using UnityEngine;
using VoxelDestructionPro.Tools;
public class Missiles : MonoBehaviour
{
    [SerializeField] protected float time_to_explode;
    [SerializeField] protected Collider missile_collider;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected float damage;
    [SerializeField] protected float travel_speed;
    [SerializeField] protected AudioSource explosion_sound;
    [SerializeField] protected GameObject explosion_effect;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected ParticleSystem trail;
    [SerializeField] protected AudioSource shoot_sound;
    protected float speed;

    public GameObject parent_gameobject;
    private EliminationMarker eliminationMarker;
    private DamageMarker damageMarker;

    protected bool didShoot;

    #region Unity Lifecycle
    protected virtual void Start()
    {
        eliminationMarker = GameObject.FindGameObjectWithTag("GeneralHUD").GetComponent<EliminationMarker>();
        damageMarker = eliminationMarker.GetComponent<DamageMarker>();
        SetParentVehicle();
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
        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        trail.transform.SetParent(null, true);
        trail.transform.localScale = Vector3.one;
        Destroy(trail.gameObject, trail.main.duration + trail.main.startLifetime.constantMax);

        if (collision.gameObject != parent_gameobject)
        {
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
                PlayerProperties player_properties = player.GetComponent<PlayerProperties>();
                damageMarker.UpdateDamage(player.Damage(damage));
                if (player_properties.is_dead)
                {
                    eliminationMarker.InstantiateVehicleImage();
                }

            }
            else
            {
                voxCollider.SphereExplosion(collision.contacts[0].point, damage);
                missile_collider.enabled = false;
            }

            Explode(collision.contacts[0].point);
        }

    }

    #endregion

    #region Collision / Explosion

    void Explode(Vector3 contact_point)
    {
        CreateSound(explosion_sound);

        if (explosion_effect != null)
        {
            GameObject explosion = Instantiate(explosion_effect, contact_point, Quaternion.identity);
            explosion.transform.localScale *= 2;
        }


        Destroy(gameObject);

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

    public virtual void Shoot() { }

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


    #endregion

}
