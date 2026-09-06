using System;
using System.Collections.Generic;
using UnityEngine;

public class LoadoutSaverManager : MonoBehaviour
{
    [Serializable]
    public class ClassLoadoutData
    {
        public ClassManager.Class className;
        public string primaryWeaponName;
        public string secondaryWeaponName;
        public string gadget1Name;
        public string skinName;
    }

    [Serializable]
    private class SerializationWrapper
    {
        public List<ClassLoadoutData> loadouts;
    }

    private List<ClassLoadoutData> classLoadouts = new List<ClassLoadoutData>();
    private InfantryLoadoutCustomization infantryLoadoutCustomization;

    public void Initialize(InfantryLoadoutCustomization parent)
    {
        infantryLoadoutCustomization = parent;
        InitializeLoadouts();
        LoadFromPlayerPrefs();
    }

    private void InitializeLoadouts()
    {
        if (classLoadouts.Count == 0)
        {
            foreach (ClassManager.Class classType in Enum.GetValues(typeof(ClassManager.Class)))
            {
                ClassLoadoutData loadoutData = new ClassLoadoutData
                {
                    className = classType,
                    primaryWeaponName = "",
                    secondaryWeaponName = "",
                    gadget1Name = ""
                };
                classLoadouts.Add(loadoutData);
            }
        }
    }

    public void SaveCurrentLoadout(ClassManager.Class targetClass)
    {
        if (infantryLoadoutCustomization == null) return;

        ClassLoadoutData loadoutData = GetLoadoutDataForClass(targetClass);
        if (loadoutData == null)
        {
            loadoutData = new ClassLoadoutData { className = targetClass };
            classLoadouts.Add(loadoutData);
        }

        loadoutData.primaryWeaponName = GetWeaponName(infantryLoadoutCustomization.selected_primary);
        loadoutData.secondaryWeaponName = GetWeaponName(infantryLoadoutCustomization.selected_secondary);
        loadoutData.gadget1Name = infantryLoadoutCustomization.selected_gadget1 != null ?
                                  infantryLoadoutCustomization.selected_gadget1.name : "";
        loadoutData.skinName = PlayerPrefs.GetString($"Skin_Selected_{targetClass}", ""); // NOVO

        SaveToPlayerPrefs();
    }

    private string GetWeaponName(GameObject weapon)
    {
        if (weapon == null) return "";

        WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
        return wp != null ? wp.weaponName : weapon.name;
    }

    public void LoadLoadoutForClass(ClassManager.Class targetClass)
    {
        ClassLoadoutData loadoutData = GetLoadoutDataForClass(targetClass);
        if (loadoutData == null) return;

        // VERIFICAÇÕES DE SEGURANÇA
        if (infantryLoadoutCustomization == null)
        {
            Debug.LogWarning("[Loadout] InfantryLoadoutCustomization is null!");
            return;
        }

        if (!string.IsNullOrEmpty(loadoutData.primaryWeaponName))
        {
            GameObject weapon = FindWeaponByName(loadoutData.primaryWeaponName, true);
            if (weapon != null)
                infantryLoadoutCustomization.selected_primary = weapon;
            else
                Debug.LogWarning($"[Loadout] Primary weapon '{loadoutData.primaryWeaponName}' was not found!");
        }

        if (!string.IsNullOrEmpty(loadoutData.secondaryWeaponName))
        {
            GameObject weapon = FindWeaponByName(loadoutData.secondaryWeaponName, false);
            if (weapon != null)
                infantryLoadoutCustomization.selected_secondary = weapon;
            else
                Debug.LogWarning($"[Loadout] Secondary weapon '{loadoutData.secondaryWeaponName}' was not found!");
        }

        if (!string.IsNullOrEmpty(loadoutData.gadget1Name))
        {
            infantryLoadoutCustomization.selected_gadget1 = FindGadgetByName(loadoutData.gadget1Name);
        }

        // Carrega a skin
        if (!string.IsNullOrEmpty(loadoutData.skinName))
        {
            PlayerPrefs.SetString($"Skin_Selected_{targetClass}", loadoutData.skinName);
            PlayerPrefs.Save();
        }
    }

    private GameObject FindWeaponByName(string weaponName, bool primary)
    {
        if (infantryLoadoutCustomization == null) return null;

        GameObject[] weaponArray = primary ? infantryLoadoutCustomization.primaryWeapons : infantryLoadoutCustomization.secondaryWeapons;

        // VERIFICAÇÃO: Se o array for nulo ou vazio, retorna null
        if (weaponArray == null || weaponArray.Length == 0)
        {
            Debug.LogWarning($"[Loadout] {(primary ? "Primary" : "Secondary")} weapon array is empty or null!");
            return null;
        }

        foreach (GameObject weapon in weaponArray)
        {
            if (weapon == null) continue;

            WeaponProperties wp = weapon.GetComponent<WeaponProperties>();
            if (wp != null && wp.weaponName == weaponName)
                return weapon;

            if (weapon.name == weaponName)
                return weapon;
        }

        Debug.LogWarning($"[Loadout] Weapon '{weaponName}' was not found in the array!");
        return null;
    }

    private GameObject FindGadgetByName(string gadgetName)
    {
        if (infantryLoadoutCustomization == null) return null;

        var gadgets = infantryLoadoutCustomization.gadgets;
        if (gadgets == null || gadgets.Length == 0)
        {
            Debug.LogWarning("[Loadout] Gadget array is empty or null!");
            return null;
        }

        foreach (GameObject gadget in gadgets)
        {
            if (gadget == null) continue;
            if (gadget.name == gadgetName) return gadget;
        }

        Debug.LogWarning($"[Loadout] Gadget '{gadgetName}' was not found!");
        return null;
    }

    private ClassLoadoutData GetLoadoutDataForClass(ClassManager.Class targetClass) => classLoadouts.Find(data => data.className == targetClass);

    private void SaveToPlayerPrefs()
    {
        try
        {
            SerializationWrapper wrapper = new SerializationWrapper { loadouts = classLoadouts };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString("ClassLoadouts", json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving loadouts: {e.Message}");
        }
    }

    public void LoadFromPlayerPrefs()
    {
        try
        {
            if (PlayerPrefs.HasKey("ClassLoadouts"))
            {
                string json = PlayerPrefs.GetString("ClassLoadouts");
                SerializationWrapper wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
                if (wrapper != null && wrapper.loadouts != null)
                    classLoadouts = wrapper.loadouts;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading loadouts: {e.Message}");
        }
    }

    public void ResetAllLoadouts()
    {
        classLoadouts.Clear();
        InitializeLoadouts();
        SaveToPlayerPrefs();
    }


}
