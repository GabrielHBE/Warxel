using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public Vehicle.VehicleCategory vehicleCategory;

    [SerializeField] private Transform[] spawn_points;
    
    [Header("Click Settings")]
    private float doubleClickThreshold = 0.2f;
    private float lastClickTime = 0f;

    private void OnMouseDown()
    {
        float timeSinceLastClick = Time.time - lastClickTime;
        
        lastClickTime = Time.time;

        if (timeSinceLastClick <= doubleClickThreshold) ExecuteSpawn();
        
    }

    private void ExecuteSpawn()
    {

        Transform selected_spawn_point = spawn_points[Random.Range(0, spawn_points.Length)];

        switch (vehicleCategory)
        {
            case Vehicle.VehicleCategory.Helicopter:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedHelicopter, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.Jet:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedJet, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.Tank:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedTank, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.Boat:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedBoat, selected_spawn_point);
                break;

        }
    }
}