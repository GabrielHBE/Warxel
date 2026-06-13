using FishNet.Object;
using UnityEngine;

public class RocketPodsMissile : Missiles
{
    [Header("Properties")]
    [SerializeField] private float bulletDropMultiplier;

    private Vector3 gravityForce;

    protected override void Update()
    {
        if (!IsSpawned || hasExploded || !IsOwner) return;

        if (parent_gameobject == null || !parent_gameobject.gameObject.activeSelf) Explode(transform.position);
        
        if (didShoot) DestroyTimer();
    }

    protected override void FixedUpdate()
    {
        if (!IsSpawned || hasExploded || !IsOwner) return;

        base.FixedUpdate();
 
        if (!didShoot) return;

        gravityForce = Vector3.down * bulletDropMultiplier * rb.mass;
        rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    public override void Shoot(Vector3 direction)
    {
        if (didShoot) return;

        SoundManager.Play2dSoundLocal(shootSound, shootSoundProperties);

        RequestShoot(direction);
    }

    [ServerRpc]
    private void RequestShoot(Vector3 direction) => CmdShoot(direction);

    [ObserversRpc]
    private void CmdShoot(Vector3 direction)
    {

        missile_collider.enabled = true;

        transform.SetParent(null);
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            didShoot = true;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = direction * travel_speed;
        }

        trail.gameObject.SetActive(true);
    }
}