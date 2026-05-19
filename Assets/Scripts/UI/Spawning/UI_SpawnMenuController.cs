using UnityEngine;

public class UI_SpawnMenuController : MonoBehaviour
{
    public GameObject parent;
    public void BTN_EnablePlayerCustomization()
    {
        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.EnablePlayerCustomization();
        }
    }

    public void BTN_EnableVehicleCustomization()
    {
        if (AccountManager.Instance.selected_class != ClassManager.Class.Pilot)
        {

            GeneralHudAlertMessages.Instance.CreateMessage("Only the pilot Class can drive vehicles", 2);
            return;

        }

        if (PlayerSpawnController.Instance != null)
        {
            PlayerSpawnController.Instance.EnableVehicleCustomization();
        }
    }

}