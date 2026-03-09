using UnityEngine;

public class RocketPodsMissile : Missiles
{
    [Header("Properties")]
    [SerializeField] private float bulletDropMultiplier;
    [SerializeField] private float rotationSpeed = 5f; // Velocidade de rotação suave

    private Vector3 gravityForce;

    protected override void Update()
    {
        if (parent_gameobject == null || !parent_gameobject.gameObject.activeSelf) Explode(transform.position);
        if (didShoot) DestroyTimer();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Calcula a força da gravidade
        gravityForce = Vector3.down * bulletDropMultiplier * rb.mass;
        rb.AddForce(gravityForce, ForceMode.Acceleration);

    }

    public override void Shoot()
    {
        if (didShoot) return;

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