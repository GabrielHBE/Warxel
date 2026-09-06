using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponAttachmentSaveData
{
    public const int CurrentVersion = 2;

    public int version;
    public string weaponName;
    public string activeSight;
    public string activeCantedSight;
    public string activeNozzle;
    public string activeBarrel;
    public string activeMag;
    public string activeGrip;
    public string activeSideGrip;
    public string activeErgonomics;
    
    public WeaponAttachmentSaveData(string weaponName)
    {
        version = CurrentVersion;
        this.weaponName = weaponName;
        activeSight = "";
        activeCantedSight = "";
        activeNozzle = "";
        activeBarrel = "";
        activeMag = "";
        activeGrip = "";
        activeSideGrip = "";
        activeErgonomics = "";
    }
}

[Serializable]
public class PlayerAttachmentsSaveData
{
    public List<WeaponAttachmentSaveData> weaponAttachments = new List<WeaponAttachmentSaveData>();
    
    public WeaponAttachmentSaveData GetWeaponData(string weaponName)
    {
        return weaponAttachments.Find(w => w.weaponName == weaponName);
    }
    
    public void SetWeaponData(WeaponAttachmentSaveData data)
    {
        var existing = GetWeaponData(data.weaponName);
        if (existing != null)
            weaponAttachments.Remove(existing);
        
        weaponAttachments.Add(data);
    }
}
