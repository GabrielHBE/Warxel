using FishNet.Object;
using UnityEngine;

public class TankPilotGunProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    [Header("UI")]
    public Sprite hudIcon;

    [Header("Bullet Prefabs")]
    public GameObject bulletPref;
    public DummyProjectile dummyBullet;

    [Header("Sounds")]
    public SoundManager.SoundComponents shootSound;

    [Header("Shooting & Reloading")]
    public float delay_to_shoot_animation;
    public ProcessReload.Reload.ReloadValues reloadValues;

    [Header("Fring Settings")]
    public Firing.FiringValues firing;

    [Header("Damage & Ballistics")]
    public Projectile.ProjectileValues projectileValues;

    [Header("Spread Settings")]
    public Spread.SpreadValues spreadValues;

    [Header("Recoil Settings")]
    public Recoil.RecoilValues recoilValues;

    [Header("Heat Settings")]
    public Heating.HeatValues heatValues;

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

}
