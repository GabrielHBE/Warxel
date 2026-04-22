using System.Collections;
using FishNet.Object;
using UnityEngine;
using VoxelDestructionPro.VoxDataProviders;

public class VehicleSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private float spawnInterval = 10f;

    private GameObject currentVehicle;
    private Transform spawnPoint;

    private float original_spawn_interval;

    void Start()
    {
        original_spawn_interval = spawnInterval;
        spawnPoint = transform;
    }

    void Update()
    {
        // Apenas o servidor executa a lógica de spawn
        if (!IsServerInitialized)
            return;

        if (currentVehicle != null)
        {
            return;
        }

        spawnInterval -= Time.deltaTime;

        if (spawnInterval <= 0f)
        {
            SpawnVehicle();
        }
    }

    [Server]
    void SpawnVehicle()
    {
        // Apenas servidor instancia e spawna o veículo
        GameObject vehicleObj = Instantiate(vehiclePrefab, spawnPoint.position, spawnPoint.rotation);

        if (vehicleObj.GetComponent<NetworkObject>() != null)
        {


            Spawn(vehicleObj);

            var vehicle = vehicleObj.GetComponent<Vehicle>();
            if (vehicle != null && vehicle.IsSpawned)
            {
                vehicle.Initialize();
                currentVehicle = vehicleObj;
                spawnInterval = original_spawn_interval;
            }
            else
            {
                Debug.LogError("Vehicle not properly spawned in network");
            }
        }
    }

}