using FishNet.Object;
using UnityEngine;

public class TowMissileController : MissileController
{
    [SerializeField] private Transform fowardReference;
    [SerializeField] private NetworkObject networkObject;

    protected override void Update()
    {
        base.Update();

        if(IsOwner) UpdateRotation();
    }

    protected override void ExecuteShot()
    {
        UpdateCurrentSpawnPointShootIndex();

        if (initializeDummyMissiles) RequestActivateDummyMissile(false);

        Projectile.ProjectileProperties prop = new Projectile.ProjectileProperties
        {
            position = spawnPoints[currentSpawnPointShootIndex.Value].position,
            rotation = spawnPoints[currentSpawnPointShootIndex.Value].rotation,
            ignoredObject = transform.root,
            root = transform.root.gameObject,
            target = networkObject
        };

        print(prop.target);

        if (ProjectileSpawner.Instance != null) ProjectileSpawner.Instance.CreateProjectile(properties.missilePrefab, properties.dummyMissilePrefab, prop, properties.projectileValues);

        UpdateAmmoAfterShot();
    }

    private void UpdateRotation()
    {
        transform.rotation = fowardReference.rotation;
    }

}
