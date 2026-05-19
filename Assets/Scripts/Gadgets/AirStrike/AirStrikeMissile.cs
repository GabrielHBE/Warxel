using System.Collections;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.Tools;

public class AirStrikeMissile : NetworkBehaviour
{
    [SerializeField] private GameObject trail;
    [SerializeField] private VoxCollider voxCollider;
    [SerializeField] private GameObject explosion_sound;
    [SerializeField] private GameObject explosion_effect;

    // Aumentei a velocidade base um pouco para o MoveTowards funcionar bem
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
        // 1. Aponta para o alvo UMA ÚNICA VEZ antes de começar a voar
        transform.LookAt(pos);

        // 2. Mantém o loop ativo até a distância ser minúscula OU quase zero
        // (Aumentei a tolerância para 1 metro para evitar que ele atravesse e o loop falhe 
        // devido à alta velocidade de 200f. O OnCollision também garante a explosão.)
        while (Vector3.Distance(transform.position, pos) > 1f)
        {
            // Move o míssil
            transform.position = Vector3.MoveTowards(transform.position, pos, missileSpeed * Time.deltaTime);
            yield return null;
        }

        // Garante que a posição final seja exatamente a posição calculada pelo raio 
        // antes de detonar.
        transform.position = pos;


        Detonate();
        ShephereExplosion();

    }
    void OnCollisionEnter(Collision collision)
    {

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

        CreateSound();
        RequestDespawn();
    }

    [ObserversRpc]
    void CreateSound()
    {
        explosion_sound.transform.SetParent(null);
        explosion_sound.GetComponent<AudioDistanceController>().StartGrowth();
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