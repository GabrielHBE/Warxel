using System.Collections.Generic;
using UnityEngine;

public class AssistsManager : MonoBehaviour
{
    public static AssistsManager Instance { get; private set; }
    private List<PlayerProperties> infantryAssistList = new List<PlayerProperties>();
    private List<Vehicle> vehicleAssistList = new List<Vehicle>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Loop reverso para a lista de infantaria
        for (int i = infantryAssistList.Count - 1; i >= 0; i--)
        {
            PlayerProperties p = infantryAssistList[i];

            if (p.is_dead.Value)
            {
                ConfirmInfantryAssist(i);
            }
        }

        // Loop reverso para a lista de veículos
        for (int i = vehicleAssistList.Count - 1; i >= 0; i--)
        {
            Vehicle v = vehicleAssistList[i];

            if (v.vehicle_destroyed.Value)
            {
                ConfirmVehicleAssist(i);
            }
        }
    }

    public void AddInfantryAssist(PlayerProperties player)
    {
        foreach (PlayerProperties p in infantryAssistList)
        {
            if (p == player) return;
        }

        infantryAssistList.Add(player);
    }

    public void AddVehicleAssist(Vehicle vehicle)
    {
        foreach (Vehicle v in vehicleAssistList)
        {
            if (v == vehicle) return;
        }

        vehicleAssistList.Add(vehicle);
    }

    private void ConfirmInfantryAssist(int i)
    {
        infantryAssistList.RemoveAt(i);
        EliminationMarker.Instance.InstantiateInfantryAssistImage();
    }

    private void ConfirmVehicleAssist(int i)
    {
        vehicleAssistList.RemoveAt(i);
        EliminationMarker.Instance.InstantiateVehicleAssistImage();
    }
}