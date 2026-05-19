using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public Vehicle.VehicleCategory vehicleCategory;

    [SerializeField] private Transform[] spawn_points;
    
    [Header("Click Settings")]
    private float doubleClickThreshold = 0.2f; // Tempo máximo entre os cliques para ser considerado duplo
    private float lastClickTime = 0f;

    private void OnMouseDown()
    {
        // Calcula o tempo desde o último clique
        float timeSinceLastClick = Time.time - lastClickTime;
        
        // Atualiza o tempo do último clique para o tempo atual
        lastClickTime = Time.time;

        // Se o tempo entre os cliques for menor que o limite, é um clique duplo!
        if (timeSinceLastClick <= doubleClickThreshold)
        {
            ExecuteSpawn();
        }
    }

    private void ExecuteSpawn()
    {

        Transform selected_spawn_point = spawn_points[Random.Range(0, spawn_points.Length)];

        switch (vehicleCategory)
        {
            case Vehicle.VehicleCategory.AttackHelicopter:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedAttackHeli, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.AttackJet:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedAttackJet, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.Gunship:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedGunship, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.IFV:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedIfv, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.MBT:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedMbt, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.ScoutHelicopter:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedScountHeli, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.StealthJet:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedStealthJet, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.TransportHelicopter:
                PlayerSpawnController.Instance.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedTransportHeli, selected_spawn_point);
                break;
        }
    }
}