using FishNet.Object;
using UnityEngine;

public class Jdam : Bombs
{
    [SerializeField] private TrailRenderer trailRenderer;
    [Header("Sounds")]
    [SerializeField] private AudioSource drop_bomb_sound;

    private Vector3 gravityForce;

    protected override void Update()
    {
        if (!IsSpawned || hasExploded || !IsOwner) return;

        if (parent_gameobject == null || !parent_gameobject.activeSelf)
            Explode(transform.position);

        if (didShoot) DestroyTimer();
    }

    protected override void FixedUpdate()
    {
        if (!IsSpawned || hasExploded || !IsOwner) return;

        base.FixedUpdate();

        if (!didShoot) return;

        // Aplica gravidade constante (como nos mísseis)
        gravityForce = Vector3.down * travel_speed * rb.mass;
        rb.AddForce(gravityForce, ForceMode.Acceleration);
    }

    
    public override void ShootBomb()
    {
        if (didShoot) return;
        
        SoundManager.Instance.RequestPlay2dSound(shootSound.name, shootSoundProperties);

        RequestShootBomb();
    }

    [ServerRpc]
    private void RequestShootBomb() => CmdShoot();

    [ObserversRpc]
    private void CmdShoot()
    {
        bomb_collider.enabled = true;

        if (trailRenderer != null) trailRenderer.enabled = true;
        if (trail != null) trail.gameObject.SetActive(true);

        Vector3 velocity = GetComponentInParent<Vehicle>().GetLinearVelocity();

        transform.SetParent(null);

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            didShoot = true;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = velocity;
        }
    }

    public Rigidbody GetRigidbody() => rb;
    public float GetTravelSpeed() => travel_speed;
    public bool GetDidShoot() => didShoot;

}