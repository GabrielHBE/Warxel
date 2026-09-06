using UnityEngine;

public class Grip : Attatchment
{
    [Header("Settings")]
    private EquippableItemHandTargets weaponHolder;
    public Transform gripHolder;

    [Header("Changes")]
    [Range(Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE)] public float verticalRecoilChange;
    [Range(Recoil.MIN_RECOIL_VALUE, Recoil.MAX_RECOIL_VALUE)] public float horizontalRecoilChange;
    [Range(Recoil.MIN_FIRTSHOTINCREASER_VALUE, Recoil.MAX_FIRTSHOTINCREASER_VALUE)] public float firstShootChange;
    public float reloadSpeedChange;
    public float adsSpeedChange;
    public float drawWeaponSpeedChange;
    public float storeWeaponSpeedChange;

    public override void Initialize()
    {
        base.Initialize();
        weaponHolder = GetComponentInParent<EquippableItemHandTargets>();
        if(weaponHolder==null || gripHolder==null) return;

        Transform leftHandPos = weaponHolder.GetLeftHandPos();
        leftHandPos.SetParent(gripHolder);
        leftHandPos.localPosition = Vector3.zero;
    }

}
