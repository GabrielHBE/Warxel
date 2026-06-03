using UnityEngine;

public static class InputManager
{
    public static bool GetKeyDown(KeyCode keyCode)
    {
        if(SettingsHUD.Instance!=null && SettingsHUD.Instance.is_menu_settings_active) return false;
        return Input.GetKeyDown(keyCode);
    }

    public static bool GetKey(KeyCode keyCode)
    {
        if(SettingsHUD.Instance!=null && SettingsHUD.Instance.is_menu_settings_active) return false;
        return Input.GetKey(keyCode);
    }

    public static float GetAxis(string axis)
    {
        if(SettingsHUD.Instance!=null && SettingsHUD.Instance.is_menu_settings_active) return 0;
        return Input.GetAxis(axis);
    }

    public static float GetAxisRaw(string axis)
    {
        if(SettingsHUD.Instance!=null && SettingsHUD.Instance.is_menu_settings_active) return 0;
        return Input.GetAxisRaw(axis);
    }

    public static bool GetMouseButtonDown(int mouse)
    {
        if(SettingsHUD.Instance!=null && SettingsHUD.Instance.is_menu_settings_active) return false;

        return Input.GetMouseButtonDown(mouse);
    }

    public static bool GetMouseButton(int mouse)
    {
        if(SettingsHUD.Instance!=null && SettingsHUD.Instance.is_menu_settings_active) return false;

        return Input.GetMouseButton(mouse);
    }
    
}
