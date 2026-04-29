using FishNet.Object;
using UnityEngine;

public class TowMissile : Missiles
{
    [SerializeField] private float turnSpeed = 5f;

    private TowMissileController controller; // Referência segura de rede

    protected override void Update()
    {
        if (parent_gameobject == null || !parent_gameobject.gameObject.activeSelf) Explode(transform.position);
        if (!didShoot) return;
        DestroyTimer();

        // movimento continua na direção ATUAL do míssil
        transform.position += transform.forward * travel_speed * Time.deltaTime;

        // Verifica se o controller e a câmera existem antes de seguir
        if (controller != null && controller.camera_transform != null)
        {
            // direção desejada (câmera)
            Vector3 foward = controller.camera_transform.forward;

            // suavização (delay real)
            transform.forward = Vector3.Slerp(
                transform.forward,
                foward,
                turnSpeed * Time.deltaTime
            );
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void Shoot(TowMissileController shooterController)
    {
        CmndShoot(shooterController);

    }

    [ObserversRpc]
    private void CmndShoot(TowMissileController shooterController)
    {
        controller = shooterController;
        didShoot = true;

        if (controller != null && controller.camera_transform != null)
            transform.forward = controller.camera_transform.forward;

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