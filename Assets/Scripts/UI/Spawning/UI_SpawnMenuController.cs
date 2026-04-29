using UnityEngine;

public class UI_SpawnMenuController : MonoBehaviour
{
    public void BTN_EnablePlayerCustomization()
    {
        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.EnablePlayerCustomization();
        }
    }

    public void BTN_EnableVehicleCustomization()
    {
        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.EnableVehicleCustomization();
        }
    }
}