using FishNet.Object;
using UnityEngine;

public class PlayerNetworkObjectSpawner : NetworkBehaviour
{
    [HideInInspector] private GameObject playerNetworkObjectPrefab;
    [SerializeField] private WeaponProperties[] weapon_prefabs;

    #region Sounds
    [ServerRpc(RequireOwnership = false)]
    public void CmdPlayWeaponSound(string weapon_name, Vector3 position)
    {
        RpcPlayWeaponSound(weapon_name, position);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcPlayWeaponSound(string weapon_name, Vector3 position)
    {
        // Procura a arma pelo nome na lista do Spawner
        GameObject weaponPrefab = LocateWeaponPrefabByName(weapon_name);

        if (weaponPrefab != null)
        {
            // Busca os sons originais dentro do Prefab da arma
            WeaponSounds ws = weaponPrefab.GetComponentInChildren<WeaponSounds>();

            if (ws != null && ws.shoot_sound != null)
            {
                // Instancia localmente para os inimigos/aliados ouvirem
                GameObject duplicatedObject = Instantiate(ws.shoot_sound.gameObject, position, Quaternion.identity);
                AudioDistanceController controller = duplicatedObject.GetComponent<AudioDistanceController>();

                if (controller != null)
                {
                    controller.StartGrowth();
                }
                else
                {
                    ws.shoot_sound.PlayOneShot(ws.shoot_sound.clip);
                }
            }
        }
    }
    #endregion

    #region Bullet
    [ServerRpc(RequireOwnership = true)]
    public void ServerSpawnBullet(GameObject bulletPrefab, Bullet.BulletData data, NetworkObject shooter, string weaponshooted_name = null)
    {
        GameObject instantiaded_obj = Instantiate(bulletPrefab, data.position, data.rotation);
        Bullet bullet = instantiaded_obj.GetComponent<Bullet>();
        Spawn(instantiaded_obj, shooter.Owner);
        bullet.CreateBullet(data, transform, null);
    }

    private GameObject LocateWeaponPrefabByName(string weapon_name)
    {
        if (string.IsNullOrEmpty(weapon_name)) return null;

        // Limpa o "(Clone)" e espaços em branco que possam vir na string
        string clean_name = weapon_name.Replace("(Clone)", "");

        foreach (WeaponProperties weapon in weapon_prefabs)
        {
            if (weapon.gameObject.name == clean_name) return weapon.gameObject;
        }

        return null;
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


    public GameObject GetSpawnedPlayerNetworkObject()
    {
        return playerNetworkObjectPrefab;
    }
}