using UnityEngine;

public class MissileProperties : MonoBehaviour
{    
    [Header("Sounds")]
    public SoundManager.SoundComponents shootSound;

    [Header("UI")]
    public Sprite hudIcon;

    [Header("Missile prefabs")]
    public GameObject missilePrefab;
    public GameObject dummyMissilePrefab;
    
    [Header("Shooting & Reloading")]
    public ProcessReload.Reload.ReloadValues reloadValues;

    [Header("Firing Settings")]
    public Firing.FiringValues firing;

    [Header("Damage & Ballistics")]
    public Projectile.ProjectileValues projectileValues;

    [Header("Spread Settings")]
    public Spread.SpreadValues spreadValues;

    [Header("Recoil Settings")]
    public Recoil.RecoilValues recoilValues;

}