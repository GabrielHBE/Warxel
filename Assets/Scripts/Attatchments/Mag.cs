using UnityEngine;

public class Mag : Attatchment
{
    public Transform magHandPosition;

    [Header("Changes")]
    public int bullet_counter_change;
    public float ads_speed_change;
    public float reload_speed_changer;

    public override void Initialize()
    {
        GetWeaponHolder();
    }

    private void GetWeaponHolder()
    {
        WeaponHolder wh = GetComponentInParent<WeaponHolder>();
        if (wh != null) wh.SetWeaponMag(magHandPosition);
    }

}
