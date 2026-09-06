using UnityEngine;

public class Mag : Attatchment
{
    public Transform magHandPosition;

    [Header("Changes")]
    public ProcessReload.Reload.ReloadValues reloadValues;
    public int bulletsPerShotChange;
    public float adsSpeedChange;

    public override void Initialize()
    {
        base.Initialize();
        GetWeaponHolder();
    }

    private void GetWeaponHolder()
    {
        EquippableItemHandTargets wh = GetComponentInParent<EquippableItemHandTargets>();
        if (wh != null) wh.SetWeaponMag(magHandPosition);
    }
}
