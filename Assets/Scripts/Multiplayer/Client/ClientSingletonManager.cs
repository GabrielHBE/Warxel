using FishNet.Object;
using UnityEngine;
using FishNet;
using FishNet.Component.Spawning;
using System.Collections;

public class ClientSingletonManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerSpawnController;
    [SerializeField] private GameObject loadoutCustomization;
    [SerializeField] private GameObject vehicleLoadoutCustomization;

    private GameObject instantiated_player_spawner;
    private GameObject instantiated_infantary_loadout_customization;
    private GameObject instantiated_vehicle_loadout_customization;


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
        if (instantiated_infantary_loadout_customization != null) Destroy(instantiated_infantary_loadout_customization);
        if (instantiated_vehicle_loadout_customization != null) Destroy(instantiated_vehicle_loadout_customization);
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