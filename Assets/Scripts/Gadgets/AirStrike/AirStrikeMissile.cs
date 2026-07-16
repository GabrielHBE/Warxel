using System.Collections;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;

public class AirStrikeMissile : NetworkBehaviour
{
    [Header("Sounds")]
    [SerializeField] private SoundManager.SoundComponents explosionSound;

    [Header("Settings")]
    [SerializeField] private GameObject trail;
    [SerializeField] private VoxCollider voxCollider;

    [SerializeField] private GameObject explosion_effect;

    private float missileSpeed = 5f;
    private float infantary_damage = 200;
    private float vehicle_damage = 100;

    [ObserversRpc]
    public void EnableMissile(Vector3 pos)
    {
        trail.SetActive(true);
        trail.GetComponent<ParticleSystem>().Play();
        StartCoroutine(GoToLocation(pos));
    }

    private IEnumerator GoToLocation(Vector3 pos)
    {
        transform.LookAt(pos);

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < missileSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / missileSpeed; // Progresso de 0 a 1

            // Interpolação linear entre posição inicial e destino
            transform.position = Vector3.Lerp(startPosition, pos, t);

            yield return null;
        }

        // Garante a posição final exata
        transform.position = pos;

        SoundManager.Play3dSoundLocal(explosionSound.clip, explosionSound.properties, transform.position);
        Detonate();
        ShephereExplosion();
    }
    void OnCollisionEnter(Collision collision)
    {
        //SoundManager.Instance.RequestPlay3dSound(explosionSound.name, soundProperties, collision.contacts[0].point, true);
        SoundManager.Play3dSoundLocal(explosionSound.clip, explosionSound.properties, collision.contacts[0].point);
        Detonate();
        ShephereExplosion();
    }

    private void ShephereExplosion()
    {
        voxCollider.SphereExplosion(transform.position, infantary_damage, vehicle_damage);
    }

    public void Detonate()
    {
        GameObject explosion = Instantiate(explosion_effect, transform.position, Quaternion.identity);
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