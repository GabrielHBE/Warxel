using UnityEngine;

public static class UnlockedWeapons
{
    public static void UnlockWeapon(string weaponName)
    {
        PlayerPrefs.SetInt($"Weapon_Unlocked_{weaponName}", 1);
        PlayerPrefs.Save();
    }

    public static void LockWeapon(string weaponName)
    {
        PlayerPrefs.SetInt($"Weapon_Unlocked_{weaponName}", 0);
        PlayerPrefs.Save();
    }

    public static bool CheckWeaponStatus(string weaponName) => PlayerPrefs.GetInt($"Weapon_Unlocked_{weaponName}") == 1;
    
    public static void RemoveStatus(string weaponName)
    {
        PlayerPrefs.DeleteKey($"Weapon_Unlocked_{weaponName}");
        PlayerPrefs.Save();
    }
}