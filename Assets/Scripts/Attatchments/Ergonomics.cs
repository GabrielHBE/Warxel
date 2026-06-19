using System.Collections.Generic;
using UnityEngine;

public class Ergonomics : Attatchment
{
    [Header("Changes")]
    [Space(5)]

    public Vector3 visualRecoilChange;
    public float reloadSpeedChange;
    public float adsSpeedChange;
    public float pickupWeaponSpeedChange;
    public float storeWeaponSpeedChange;
    public List<WeaponProperties.FireMode> fireModesChange = new List<WeaponProperties.FireMode>();
    public float rafeOfFireChange;
    public int burstBulletsPerTapChange;
    public float burstTimeBetweenBurstsChange;

}
