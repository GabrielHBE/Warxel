using FishNet.Object;
using UnityEngine;

public class AttackHelicopterGunnerProperties : MonoBehaviour
{
    [Header("UI")]
    public Sprite hudIcon;

    [Header("Sounds")]
    public SoundManager.SoundComponents shootSound;

    [Header("Bullet Prefabs")]
    public DummyProjectile dummyBullet;
    public GameObject bulletPref;

    [Header("Heat Settings")]
    public Heating.HeatValues heatValues;

    [Header("Fring Settings")]
    public Firing.FiringValues firing;

    [Header("Damage & Ballistics")]
    public Projectile.ProjectileValues projectileValues;

    [Header("Spread Settings")]
    public Spread.SpreadValues spreadValues;

    [Header("Recoil Settings")]
    public Recoil.RecoilValues recoilValues;

    public void Awake()
    {
        recoilValues.CalculateRecoilSpeed(firing.interval);

    }
}
