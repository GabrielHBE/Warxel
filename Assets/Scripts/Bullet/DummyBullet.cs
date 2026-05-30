using UnityEngine;

public class DummyBullet : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    private Transform ignoredTransform;
    private Vector3 lastPosition;
    private float bulletDropMultiplier;
    public void CreateBullet(Bullet.BulletData data, Transform ignoredObject = null)
    {
        bulletDropMultiplier = data.dropMultiplier;
        ignoredTransform = ignoredObject;
        lastPosition = transform.position;

        if (data.size != 0) transform.localScale *= data.size;

        SetDirection(data.direction, data.speed);
    }

    public void SetDirection(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter(Collider collider)
    {
        print(collider.gameObject.name);
        // Ignora colisões com balas ou física do player
        if (collider.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
            collider.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }

        // Ignora o objeto passado no parâmetro (e as hitboxes filhas dele)
        if (ignoredTransform != null && collider.transform.IsChildOf(ignoredTransform))
        {
            return;
        }

        Destroy(gameObject);
    }

    private RaycastHit[] hit_results = new RaycastHit[128];

    void FixedUpdate()
    {
        rb.AddForce(Vector3.down * bulletDropMultiplier, ForceMode.Acceleration);


        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - lastPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
            int layerMask = ~(1 << LayerMask.NameToLayer("Projectile") | 1 << LayerMask.NameToLayer("Player"));
            int hits = Physics.RaycastNonAlloc(lastPosition, direction.normalized, hit_results, distance, layerMask);

            if (hits > 0)
            {
                float minDistance = float.MaxValue;
                bool foundValidHit = false;

                for (int i = 0; i < hits; i++)
                {
                    RaycastHit hit = hit_results[i];

                    if (ignoredTransform != null && hit.collider.transform.IsChildOf(ignoredTransform))
                    {
                        continue;
                    }

                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        foundValidHit = true;
                    }
                }

                if (foundValidHit)
                {
                    Destroy(gameObject);
                }
            }
        }

        lastPosition = currentPosition;

    }
}
