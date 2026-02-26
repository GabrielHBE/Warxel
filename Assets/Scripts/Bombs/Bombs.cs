using UnityEngine;
using VoxelDestructionPro.Tools;

public class Bombs : MonoBehaviour
{
    [SerializeField] protected float time_to_explode;
    [SerializeField] protected Collider bomb_collider;
    [SerializeField] protected VoxCollider voxCollider;
    [SerializeField] protected float damage;
    [SerializeField] protected float travel_speed;
    [SerializeField] protected AudioSource explosion_sound;
    [SerializeField] protected GameObject explosion_effect;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected GameObject trail;
    [SerializeField] protected AudioSource shoot_sound;
    protected float speed;

    public GameObject parent_gameobject;

    protected bool didShoot;

      #region Unity Lifecycle
    protected virtual void Start()
    {
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

        if (collision.gameObject != parent_gameobject)
        {
            Explode(collision.contacts[0].point);
        }

    }

    #endregion

    #region Collision / Explosion

    void Explode(Vector3 contact_point)
    {
        AudioDistanceController AudioDistanceController = explosion_sound.GetComponent<AudioDistanceController>();
        AudioDistanceController.StartGrowth();

        voxCollider.SphereExplosion(contact_point, damage);
        bomb_collider.enabled = false;

        if (explosion_effect != null)
        {
            GameObject explosion = Instantiate(explosion_effect, contact_point, Quaternion.identity);
            explosion.transform.localScale *= 2;
        }


        Destroy(gameObject);

    }


    protected virtual void CreateSound(AudioSource sound)
    {
        sound.transform.SetParent(null);
        sound.Play();
        Destroy(sound, sound.clip.length);
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
