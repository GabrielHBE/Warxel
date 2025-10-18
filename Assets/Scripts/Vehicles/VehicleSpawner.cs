using UnityEngine;

public class JetSpawner : MonoBehaviour
{

    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 10f;

    private GameObject currentVehicle;
    
    void Start()
    {
        currentVehicle = Instantiate(vehiclePrefab, spawnPoint.position, spawnPoint.rotation);
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
            spawnInterval = 10f;
        }

    }


}
