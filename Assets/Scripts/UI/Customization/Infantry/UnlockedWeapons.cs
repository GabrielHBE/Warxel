using UnityEngine;

public static class UnlockedWeapons
{
    public static void UnlockWeapon(string weapon_name)
    {
        PlayerPrefs.SetInt($"Weapon_Unlocked_{weapon_name}", 1);
        PlayerPrefs.Save();
    }

    public static void LockWeapon(string weapon_name)
    {
        PlayerPrefs.SetInt($"Weapon_Unlocked_{weapon_name}", 0);
        PlayerPrefs.Save();
    }

    public static bool CheckWeaponStatus(string weapon_name) => PlayerPrefs.GetInt($"Weapon_Unlocked_{weapon_name}") == 1;
    
    public static void RemoveStatus(string weapon_name)
    {
        PlayerPrefs.DeleteKey($"Weapon_Unlocked_{weapon_name}");
        PlayerPrefs.Save();
    }
}