using UnityEngine;

public class CursorManager : InMatchClientSingleton<CursorManager>
{

    protected override void Awake()
    {
        base.Awake();

        if (PlayerSpawnController.Instance == null)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

    }

    void Update()
    {
        if (PlayerSpawnController.Instance == null) return;

        if (SettingsHUD.Instance.is_menu_settings_active || PlayerSpawnController.Instance.player_instantiated == null)
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
