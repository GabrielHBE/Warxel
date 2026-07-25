using UnityEngine;

public class C4Explosive : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private SoundManager.SoundComponents explosionSound;

    [Header("Damage")]
    [SerializeField] private float infantryDamage;
    [SerializeField] private float vehicleDamage;
    [SerializeField] private float explosionDamageFalloff = 1;
    [SerializeField] private float destructionRadius = 10;

    [Header("References")]
    [SerializeField] private GameObject smokeEffect;
    [SerializeField] private C4Detonator c4;
    [SerializeField] private GameObject throw_hand;
    [SerializeField] private GameObject throw_hand_original_pos;



    public float explosionRadius = 10f;
    public float explosionForce = 20f;


    public void Detonate()
    {
        Explosion.SphereExplosion(transform.position, infantryDamage, vehicleDamage, destructionRadius, explosionDamageFalloff, null, gameObject);
        //voxCollider.SphereExplosion(transform.position, infantary_damage, vehicle_damage);

        Instantiate(smokeEffect, transform.position, Quaternion.identity);

        SoundManager.Instance.RequestPlay3dSound(explosionSound.clip.name, explosionSound.properties, transform.position, false);
        SoundManager.Play3dSoundLocal(explosionSound.clip, explosionSound.properties, transform.position);

        Destroy(gameObject);
    }



    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Detonate();

        }
        else if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Player"))
        {

            // Destroy Rigidbody to prevent further physics interactions
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
                Destroy(rb);

            transform.parent = collision.transform;

        }

    }

}