using FishNet.Object;
using UnityEngine;

public class VehicleTowMissileController : VehicleMissileController
{
    [SerializeField] private Transform fowardReference;
    [SerializeField] private NetworkObject networkObject;

    protected override void Update()
    {
        base.Update();

        if (IsOwner && isActive) UpdateRotation();
    }

    protected override void ExecuteShot()
    {
        int spawnIndex = currentSpawnPointShootIndex.Value;
        if (initializeDummyMissiles) RequestActivateDummyMissile(spawnIndex, false);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = spawnPoints[spawnIndex].position,
            rotation = spawnPoints[spawnIndex].rotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject,
            target = networkObject
        };

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.bulletPref, properties.dummyBullet.gameObject, prop, properties.projectileValues);

        PlayShotEffects();
        UpdateAmmoAfterShot();
        UpdateCurrentSpawnPointShootIndex();
    }

    private void UpdateRotation()
    {
        transform.rotation = fowardReference.rotation;
    }

}
