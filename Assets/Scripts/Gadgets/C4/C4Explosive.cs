using UnityEngine;
using VoxelDestructionPro.Tools;


public class C4Explosive : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private SoundManager.SoundProperties soundProperties = SoundManager.SoundProperties.Default;

    [Header("Damage")]
    [SerializeField] private float infantary_damage;
    [SerializeField] private float vehicle_damage;

    [Header("References")]
    [SerializeField] VoxCollider voxCollider;
    [SerializeField] private GameObject smokeEffect;
    [SerializeField] private C4Detonator c4;
    [SerializeField] private GameObject throw_hand;
    [SerializeField] private GameObject throw_hand_original_pos;
    


    public float explosionRadius = 10f;
    public float explosionForce = 20f;


    public void Detonate()
    {
        voxCollider.SphereExplosion(transform.position, infantary_damage, vehicle_damage);

        Instantiate(smokeEffect, transform.position, Quaternion.identity);

        SoundManager.Instance.RequestPlay3dSound(explosionSound.name, soundProperties, transform.position, false);
        SoundManager.Play3dSoundLocal(explosionSound, soundProperties, transform.position);

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