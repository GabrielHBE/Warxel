using UnityEngine;

public class Mag : Attatchment
{
    public Transform magHandPosition;

    [Header("Changes")]
    public ProcessReload.Reload.ReloadValues reloadValues;
    public int bulletsPerSHotChange;
    public float adsSpeedChange;

    public override void Initialize()
    {
        base.Initialize();
        GetWeaponHolder();
    }

    private void GetWeaponHolder()
    {
        WeaponHolder wh = GetComponentInParent<WeaponHolder>();
        if (wh != null) wh.SetWeaponMag(magHandPosition);
    }

    public override string GetAttatchmentDescription()
    {
        throw new System.NotImplementedException();
    }

}
