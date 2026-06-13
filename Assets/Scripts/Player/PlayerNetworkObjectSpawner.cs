using FishNet.Object;
using UnityEngine;

public class PlayerNetworkObjectSpawner : NetworkBehaviour
{

    [SerializeField] private WeaponProperties[] weapon_prefabs;

    #region Bullet
    [ServerRpc(RequireOwnership = true)]
    public void ServerSpawnBullet(NetworkObject bulletPref, Bullet.BulletData data, string weaponshooted_name = null)
    {
        // Proteção: Se o prefab chegar nulo, cancela a execução antes de estourar o erro
        if (bulletPref == null)
        {
            Debug.LogError("ServerSpawnBullet: O bulletPref veio nulo! Verifique se o Prefab está na lista de 'Spawnable Prefabs' do NetworkManager.");
            return;
        }

        // 1. Retira o objeto do pool do FishNet
        NetworkObject pooledNetworkObj = NetworkManager.GetPooledInstantiated(bulletPref, IsServerInitialized);
        
        // 2. IMPORTANTÍSSIMO: Atualiza a posição e rotação no servidor ANTES de enviar para a rede
        pooledNetworkObj.transform.position = data.position;
        pooledNetworkObj.transform.rotation = data.rotation;

        // 3. Pega o componente da Bullet
        Bullet bullet = pooledNetworkObj.GetComponent<Bullet>();

        // 4. Faz o Spawn na rede definindo quem é o Dono (Owner)
        Spawn(pooledNetworkObj, Owner);

        // 5. Executa a inicialização que vai propagar os dados e ativar a física via ObserversRpc
        bullet.CreateBullet(data, transform, this.gameObject);
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