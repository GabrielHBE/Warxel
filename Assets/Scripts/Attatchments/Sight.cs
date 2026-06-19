using System;
using UnityEngine;

public class Sight : Attatchment
{
    [Header("Settings")]
    public Transform adsPosition;
    public SightType sightType;

    [Header("Changes")]
    public float zoom_change;
    public float ads_speed_change;
    public float sway_change;
    public string reticle;

    public override void Initialize()
    {
        return;
    }

    public enum SightType
    {
        CloseRange,
        MediumRange,
        LongRange,
        ExtremeLongRange
    }

}
