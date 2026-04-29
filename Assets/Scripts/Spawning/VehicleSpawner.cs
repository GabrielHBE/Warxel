using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public Vehicle.VehicleCategory vehicleCategory;

    [SerializeField] private Transform[] spawn_points;

    private void OnMouseDown()
    {
        if (AccountManager.Instance.selected_class != ClassManager.Class.Pilot)
        {

            GeneralHudAlertMessages.Instance.CreateMessage("Only the pilot Class can drive vehicles", 2);
            return;

        }

        PlayerSpawnController localController = PlayerSpawnManager.Instance?.GetPlayerSpawnController();
        if (localController == null)
        {
            Debug.LogWarning("No local spawn controller found!");
            return;
        }

        Transform selected_spawn_point = spawn_points[Random.Range(0, spawn_points.Length)];

        switch (vehicleCategory)
        {
            case Vehicle.VehicleCategory.AttackHelicopter:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedAttackHeli, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.AttackJet:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedAttackJet, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.Gunship:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedGunship, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.IFV:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedIfv, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.MBT:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedMbt, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.ScoutHelicopter:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedScountHeli, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.StealthJet:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedStealthJet, selected_spawn_point);
                break;

            case Vehicle.VehicleCategory.TransportHelicopter:
                localController.InitializeSpawnVehicle(VehicleLoadoutCustomization.Instance.selectedTransportHeli, selected_spawn_point);
                break;
        }


    }

}