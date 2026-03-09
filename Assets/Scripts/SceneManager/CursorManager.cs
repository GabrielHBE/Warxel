using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private PlayerLoadoutCustomization playerLoadoutCustomization;
    [SerializeField] private PlayerBaseSpawn playerBaseSpawn;


    void Update()
    {
        if (SettingsHUD.Instance.is_menu_settings_active || playerBaseSpawn.player == null)
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
