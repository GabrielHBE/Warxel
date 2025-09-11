using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject bullet_hole;
    private Rigidbody rb;
    private float bulletDropMultiplier;

    private float damage;
    private float damage_dropoff;
    private float damage_dropoff_timer;


    [Header("Sounds")]
    public GameObject ricochet_sound;
    private RicochetSounds ricochetSounds;
    private Vector3 original_position;

    bool did_ricochet;

    float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // ESSENCIAL!
    }

    public void CreateBullet(Vector3 direction, float speed, float dropMultiplier, float dmg, float dmg_dropoff, float dmg_dropoff_timer)
    {
        did_ricochet = false;
        damage = dmg;
        damage_dropoff = dmg_dropoff;
        damage_dropoff_timer = dmg_dropoff_timer;

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
                        Destroy(gameObject);
                    }
                    else if (collision.gameObject.CompareTag("Voxel"))
                    {

                        Destroy(gameObject);
                    }
                    else
                    {
                        CreateHole(contact.point, contact.normal);

                    }

                }
                else
                {
                    Ricochet(contact.point, contact.normal);
                }

            }

        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            GameObject player = collision.gameObject;
            PlayerProperties playerProperties = player.GetComponent<PlayerProperties>();
            playerProperties.hp -= damage;

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


    void CreateHole(Vector3 position, Vector3 normal)
    {
        GameObject obj = Instantiate(bullet_hole, position + normal * 0.01f, Quaternion.LookRotation(normal));
        obj.transform.position -= obj.transform.forward / 110f;
        Destroy(obj, 10f);

    }
}
