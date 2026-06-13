using FishNet.Object;
using UnityEngine;
using FishNet;
using FishNet.Component.Spawning;
using System.Collections;

public class ClientSingletonManager : NetworkBehaviour
{
    [SerializeField] private GameObject accountManager;
    [SerializeField] private GameObject playerSpawnController;
    [SerializeField] private GameObject generalHUD;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject loadoutCustomization;
    [SerializeField] private GameObject vehicleLoadoutCustomization;
    [SerializeField] private GameObject bulletObjectPooling;


    private GameObject instantiated_player_spawner;
    private GameObject instantiated_account_manager;
    private GameObject instantiated_gerenal_hud;
    private GameObject instantiated_settings;
    private GameObject instantiated_infantary_loadout_customization;
    private GameObject instantiated_vehicle_loadout_customization;
    private GameObject instantiades_bullet_object_pooling;



    public override void OnStartClient()
    {
        base.OnStartClient();

        // Verifica se este objeto pertence ao cliente local
        if (IsOwner)
        {
            SpawnClientObjects();
        }
        else
        {
            gameObject.SetActive(false);
        }

    }

    private void SpawnClientObjects()
    {

        // Spawna o PlayerSpawnController (que tem NetworkBehaviour)
        if (playerSpawnController != null)
        {
            SpawnPlayerSpawner();
        }

        if (bulletObjectPooling != null)
        {
            instantiades_bullet_object_pooling = Instantiate(bulletObjectPooling);
        }

        // Para objetos sem NetworkBehaviour, instancia normalmente
        if (accountManager != null)
            instantiated_account_manager = Instantiate(accountManager);

        if (generalHUD != null)
            instantiated_gerenal_hud = Instantiate(generalHUD);

        if (settings != null)
            instantiated_settings = Instantiate(settings);

        if (loadoutCustomization != null)
            instantiated_infantary_loadout_customization = Instantiate(loadoutCustomization);

        if (vehicleLoadoutCustomization != null)
        {
            instantiated_vehicle_loadout_customization = Instantiate(vehicleLoadoutCustomization);
            StartCoroutine(DisableVehicleCustomization());
        }

    }

    private IEnumerator DisableVehicleCustomization()
    {
        yield return null;
        VehicleLoadoutCustomization.Instance.gameObject.SetActive(false);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (instantiated_account_manager != null) Destroy(instantiated_account_manager);
        if (instantiated_gerenal_hud != null) Destroy(instantiated_gerenal_hud);
        if (instantiated_settings != null) Destroy(instantiated_settings);
        if (instantiated_infantary_loadout_customization != null) Destroy(instantiated_infantary_loadout_customization);
        if (instantiated_vehicle_loadout_customization != null) Destroy(instantiated_vehicle_loadout_customization);
        if (instantiades_bullet_object_pooling != null) Destroy(instantiades_bullet_object_pooling);

    }


    [ServerRpc]
    private void SpawnPlayerSpawner()
    {
        PlayerSpawner playerSpawner = InstanceFinder.NetworkManager.GetComponent<PlayerSpawner>();

        instantiated_player_spawner = Instantiate(playerSpawnController);
        instantiated_player_spawner.transform.position = playerSpawner.Spawns[0].position;
        Spawn(instantiated_player_spawner, Owner);

    }

}