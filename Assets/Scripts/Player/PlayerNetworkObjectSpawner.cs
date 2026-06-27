using FishNet.Object;
using UnityEngine;

public class PlayerNetworkObjectSpawner : NetworkBehaviour
{

    [SerializeField] private WeaponProperties[] weapon_prefabs;

    #region Bullet
    [ServerRpc]
    public void ServerSpawnBullet(NetworkObject bulletPref, Bullet.BulletData data, string weaponshooted_name = null)
    {
        // Proteção: Se o prefab chegar nulo, cancela a execução antes de estourar o erro
        if (bulletPref == null)
        {
            Debug.LogError("ServerSpawnBullet: O bulletPref veio nulo! Verifique se o Prefab está na lista de 'Spawnable Prefabs' do NetworkManager.");
            return;
        }

        NetworkObject pooledNetworkObj = NetworkManager.GetPooledInstantiated(bulletPref, IsServerInitialized);

        Bullet bullet = pooledNetworkObj.GetComponent<Bullet>();

        Spawn(pooledNetworkObj, Owner);

        bullet.CreateBullet(data, transform, gameObject);
    }
    #endregion

    #region  Gadget

    [ServerRpc(RequireOwnership = false)]
    public void ServerSpawnAirStrike(GameObject airStrikePrefab, Vector3 goToPos)
    {
        Vector3 pos = new Vector3(Random.Range(-500, 500), MapSettings.Instance.max_altitude, Random.Range(-500, 500));
        GameObject instantiatedAirStrike = Instantiate(airStrikePrefab, pos, Quaternion.identity);
        Spawn(instantiatedAirStrike);

        AirStrikeMissile airStrikeMissile = instantiatedAirStrike.GetComponent<AirStrikeMissile>();
        if (airStrikeMissile != null)
        {
            airStrikeMissile.EnableMissile(goToPos);
        }

    }
    #endregion
}