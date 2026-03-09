using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponAttachmentSaveData
{
    public string weaponName;
    public string activeSight;
    public string activeBarrel;
    public string activeMag;
    public string activeGrip;
    public string activeSideGrip;
    
    public WeaponAttachmentSaveData(string weaponName)
    {
        this.weaponName = weaponName;
        activeSight = "";
        activeBarrel = "";
        activeMag = "";
        activeGrip = "";
        activeSideGrip = "";
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