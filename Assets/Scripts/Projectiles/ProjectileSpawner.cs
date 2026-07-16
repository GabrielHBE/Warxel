using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class ProjectileSpawner : NetworkBehaviour
{
    public static ProjectileSpawner Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void CreateProjectile(GameObject projectile, GameObject dummyProjectile, Projectile.ProjectileProperties projectileProperties, Projectile.ProjectileValues projectileValues)
    {
        InstantiateLocalPooledProjectile(projectile, projectileProperties, projectileValues);
        RequestSpawnProjectile(dummyProjectile, projectileProperties, projectileValues);
    }

    #region Server Methods
    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnProjectile(GameObject dummyProjectile, Projectile.ProjectileProperties projectileProperties, Projectile.ProjectileValues projectileValues, NetworkConnection caller = null) => CmdSpawnDummyProjectile(dummyProjectile, projectileProperties, projectileValues, caller.ClientId);
    
    [ObserversRpc]
    private void CmdSpawnDummyProjectile(GameObject dummyProjectile, Projectile.ProjectileProperties projectileProperties, Projectile.ProjectileValues projectileValues, int callerId) 
    {
        if (ClientManager.Connection.ClientId == callerId) return;
        InstantiateDummyLocalPooledProjectile(dummyProjectile, projectileProperties, projectileValues);
    }
    #endregion

    #region Helper Methods
    private void InstantiateDummyLocalPooledProjectile(GameObject dummyProjectile, Projectile.ProjectileProperties projectileProperties, Projectile.ProjectileValues projectileValues) => LocalObjectPooling.Instance.GetPooledItem(dummyProjectile).GetComponent<DummyProjectile>().CreateProjectile(projectileProperties, projectileValues);
    private void InstantiateLocalPooledProjectile(GameObject projectile, Projectile.ProjectileProperties projectileProperties, Projectile.ProjectileValues projectileValues) => LocalObjectPooling.Instance.GetPooledItem(projectile).GetComponent<Projectile>().CreateProjectile(projectileProperties, projectileValues);
    #endregion
}