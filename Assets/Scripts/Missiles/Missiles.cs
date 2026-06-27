
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
        // Verifica se o objeto ainda está ativo na rede
        if (!IsSpawned || hasExploded || collision.gameObject == parent_gameobject) return;

        // Verifica se o NetworkTransform ainda está ativo
        if (GetComponent<FishNet.Component.Transforming.NetworkTransform>() != null)
        {
            // Desativa o NetworkTransform para evitar atualizações futuras
            GetComponent<FishNet.Component.Transforming.NetworkTransform>().enabled = false;
        }

        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        trail.transform.SetParent(null, true);
        trail.transform.localScale = Vector3.one;
        Destroy(trail.gameObject, trail.main.duration + trail.main.startLifetime.constantMax);

        hasExploded = true;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Vehicle"))
        {
            ProcessHit.VehicleHit(collision.gameObject, infantary_damage, gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("PlayerHitBox"))
        {
            ProcessHit.PlayerHit(collision.gameObject, infantary_damage, 2, gameObject);
        }

        missile_collider.enabled = false;

        SoundManager.Play3dSoundLocal(explosionSound, explosionSoundProperties, collision.contacts[0].point);

        voxCollider.SphereExplosion(collision.contacts[0].point, infantary_damage, vehicle_damage);

        // Explode e desspawna
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

        if (IsServerInitialized) RequestDespawn();
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
    [Server]
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