using FishNet.Object;
using UnityEngine;

public class ScoutHelicopterMainMinigunProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    public Sprite hudIcon;

    [Header("Bullet Prefabs ")]
    public DummyProjectile dummyBullet;
    public GameObject bulletPref;

    [Header("Sounds")]
    public SoundManager.SoundComponents shootSound;

    [Header("Heat Settings")]
    public Heating.HeatValues heatValues;

    [Header("Fring Settings")]
    public Firing.FiringValues firing;

    [Header("Damage & Ballistics")]
    public Projectile.ProjectileValues projectileValues;

    [Header("Spread Settings")]
    public Spread.SpreadValues spreadValues;

    #region Interface Methods
    public void Activate()
    {
        throw new System.NotImplementedException();
    }

    public void Deactivate()
    {
        throw new System.NotImplementedException();
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        throw new System.NotImplementedException();
    }

    public string GetCustomizationPartName()
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
