using FishNet.Object;
using UnityEngine;

public class RocketPodsMissile : Missiles
{
    [Header("Properties")]
    [SerializeField] private float bulletDropMultiplier;

    private Vector3 gravityForce;

    protected override void Update()
    {
        if (parent_gameobject == null || !parent_gameobject.gameObject.activeSelf) Explode(transform.position);
        if (didShoot) DestroyTimer();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // NOVA TRAVA: Se ainda não atirou, não calcula física de queda!
        if (!didShoot) return;

        // Calcula a força da gravidade
        gravityForce = Vector3.down * bulletDropMultiplier * rb.mass;
        rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void Shoot()
    {
        if (didShoot) return;
        CmdShoot();
    }

    [ObserversRpc]
    private void CmdShoot()
    {

        CreateSound(shoot_sound);
        missile_collider.enabled = true;

        transform.SetParent(null);
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            didShoot = true;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = transform.right * travel_speed;
        }

        trail.gameObject.SetActive(true);
    }
}