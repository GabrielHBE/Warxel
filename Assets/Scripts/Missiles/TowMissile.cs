using FishNet.Object;
using UnityEngine;

public class TowMissile : Missiles
{
    [SerializeField] private float turnSpeed = 5f;


    protected override void Update()
    {
        if (parent_gameobject == null || !parent_gameobject.gameObject.activeSelf) Explode(transform.position);
        if (!didShoot) return;
        DestroyTimer();

        // movimento continua na direção ATUAL do míssil
        transform.position += transform.forward * travel_speed * Time.deltaTime;


        // direção desejada (câmera)
        Vector3 foward = Camera.main.transform.forward;

        // suavização (delay real)
        transform.forward = Vector3.Slerp(
            transform.forward,
            foward,
            turnSpeed * Time.deltaTime
        );

    }

    [ServerRpc(RequireOwnership = false)]
    public override void Shoot(Vector3 direction)
    {
        CmndShoot(direction);

    }

    [ObserversRpc]
    private void CmndShoot(Vector3 direction)
    {
        didShoot = true;

        CreateSound(shoot_sound);
        missile_collider.enabled = true;

        transform.SetParent(null);
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        trail.gameObject.SetActive(true);
    }
}