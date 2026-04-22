using Unity.Transforms;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }


    void Start()
    {
        Instance = this;

        if (PlayerSpawnManager.Instance.GetPlayerSpawnController() == null)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

        }

    }

    void Update()
    {
        if (PlayerSpawnManager.Instance.GetPlayerSpawnController()== null) return;

        if (SettingsHUD.Instance.is_menu_settings_active || PlayerSpawnManager.Instance.GetPlayerSpawnController().player_instantiated == null)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

}
