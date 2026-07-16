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
    public List<Firing.FireMode> fireModesChange = new List<Firing.FireMode>();
    public int rafeOfFireChange;
    public int burstBulletsPerTapChange;
    public float burstTimeBetweenBurstsChange;

    public override string GetAttatchmentDescription()
    {
        throw new System.NotImplementedException();
    }
}   
