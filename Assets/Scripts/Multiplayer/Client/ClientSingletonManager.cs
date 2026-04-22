using FishNet.Object;
using FishNet.Managing;
using FishNet.Connection;
using UnityEngine;
using FishNet;
using FishNet.Component.Spawning;
using Unity.VisualScripting;

public class ClientSingletonManager : NetworkBehaviour
{
    [SerializeField] private GameObject accountManager;
    [SerializeField] private GameObject playerSpawnController;
    [SerializeField] private GameObject generalHUD;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject loadoutCustomization;

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

        // Para objetos sem NetworkBehaviour, instancia normalmente
        if (accountManager != null)
            Instantiate(accountManager);

        if (generalHUD != null)
            Instantiate(generalHUD);

        if (settings != null)
            Instantiate(settings);

        if (loadoutCustomization != null)
            Instantiate(loadoutCustomization);
    }

    [ServerRpc]
    private void SpawnPlayerSpawner()
    {
        PlayerSpawner playerSpawner = InstanceFinder.NetworkManager.GetComponent<PlayerSpawner>();

        GameObject obj = Instantiate(playerSpawnController);
        obj.transform.position = playerSpawner.Spawns[0].position;
        Spawn(obj, Owner);
        
    }
}