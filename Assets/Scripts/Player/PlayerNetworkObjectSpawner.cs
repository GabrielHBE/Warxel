using FishNet.Object;
using UnityEngine;

public class PlayerNetworkObjectSpawner : NetworkBehaviour
{
    [HideInInspector] private GameObject playerNetworkObjectPrefab;
    [SerializeField] private WeaponProperties[] weapon_prefabs;

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

    [ServerRpc(RequireOwnership = false)]
    public void ServerSpawnBullet(GameObject bulletPrefab, Bullet.BulletData data, NetworkObject shooter, string weaponshooted_name = null)
    {
        GameObject instantiaded_obj = Instantiate(bulletPrefab, data.position, data.rotation);

        Spawn(instantiaded_obj, shooter.Owner);

        Bullet bullet = instantiaded_obj.GetComponent<Bullet>();
        if (bullet != null)
        {
            //GameObject weaponPrefab = LocateWeaponPrefabByName(weaponshooted_name);
            GameObject weaponPrefab = null;
            bullet.CreateBullet(data, transform, weaponPrefab);
        }
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


    public GameObject GetSpawnedPlayerNetworkObject()
    {
        return playerNetworkObjectPrefab;
    }
}