using System.Collections;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;

public class AirStrikeMissile : NetworkBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip explosionSound;    
    [SerializeField] private SoundManager.SoundProperties soundProperties = SoundManager.SoundProperties.Default;

    [Header("Settings")]
    [SerializeField] private GameObject trail;
    [SerializeField] private VoxCollider voxCollider;

    [SerializeField] private GameObject explosion_effect;

    private float missileSpeed = 200f;
    private float infantary_damage = 200;
    private float vehicle_damage = 100;

    [ObserversRpc]
    public void EnableMissile(Vector3 pos)
    {
        trail.SetActive(true);
        trail.GetComponent<ParticleSystem>().Play();
        StartCoroutine(GoToLogation(pos));
    }

    private IEnumerator GoToLogation(Vector3 pos)
    {
        transform.LookAt(pos);

        while (Vector3.Distance(transform.position, pos) > 1f)
        {
            // Move o míssil
            transform.position = Vector3.MoveTowards(transform.position, pos, missileSpeed * Time.deltaTime);
            yield return null;
        }

        // Garante que a posição final seja exatamente a posição calculada pelo raio 
        // antes de detonar.
        transform.position = pos;

        SoundManager.Play3dSoundLocal(explosionSound, soundProperties, transform.position);
        Detonate();
        ShephereExplosion();

    }
    void OnCollisionEnter(Collision collision)
    {
        //SoundManager.Instance.RequestPlay3dSound(explosionSound.name, soundProperties, collision.contacts[0].point, true);
        SoundManager.Play3dSoundLocal(explosionSound, soundProperties, collision.contacts[0].point);
        Detonate();
        ShephereExplosion();
    }

    private void ShephereExplosion()
    {
        voxCollider.SphereExplosion(transform.position, infantary_damage, vehicle_damage);
    }


    [ServerRpc(RequireOwnership = false)]
    public void Detonate()
    {

        GameObject explosion = Instantiate(explosion_effect, transform.position, Quaternion.identity);
        Spawn(explosion);
        explosion.transform.localScale *= 2;

        RequestDespawn();
    }



    [ServerRpc(RequireOwnership = false)]
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
}