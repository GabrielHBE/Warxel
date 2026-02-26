using UnityEngine;

public class VehicleSpawner : MonoBehaviour
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
        currentVehicle = Instantiate(vehiclePrefab, spawnPoint.position, spawnPoint.rotation);
        currentVehicle.GetComponent<Vehicle>().Spawn();
    }

    void Update()
    {   
        if (currentVehicle != null)
        {
            return;
        }

        spawnInterval -= Time.deltaTime;

        if (spawnInterval <= 0f)
        {
            currentVehicle = Instantiate(vehiclePrefab, spawnPoint.position, spawnPoint.rotation);
            currentVehicle.GetComponent<Vehicle>().Spawn();
            spawnInterval = original_spawn_interval;
        }

    }


}
