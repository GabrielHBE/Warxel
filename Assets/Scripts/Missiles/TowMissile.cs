using UnityEngine;

public class TowMissile : Missiles
{
    [SerializeField] private float turnSpeed = 5f;

    private Transform cameraTransform;

    protected override void Update()
    {
    
        if (!didShoot) return;
        DestroyTimer();

        // movimento continua na direção ATUAL do míssil
        transform.position += transform.forward * travel_speed * Time.deltaTime;

        // direção desejada (câmera)
        Vector3 foward = cameraTransform.forward;

        // suavização (delay real)
        transform.forward = Vector3.Slerp(
            transform.forward,
            foward,
            turnSpeed * Time.deltaTime
        );
    }

    public void Shoot(Transform cameraTransform)
    {
        trail.gameObject.SetActive(true);
        
        this.cameraTransform = cameraTransform;
        didShoot = true;

        //transform.position = cameraTransform.position;
        transform.forward = cameraTransform.forward;

        CreateSound(shoot_sound);
        missile_collider.enabled = true;

        transform.SetParent(null);
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        
    }
}
