using FishNet.Object;
using UnityEngine;

public class ScoutHelicopterMainMinigunProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    public Sprite hudIcon;

    [Header("Sounds")]
    public AudioClip shoot_sound;
    public SoundManager.SoundProperties shootSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Bullet")]
    public DummyBullet dummyBullet;
    public NetworkObject bulletPref;
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;
    public GameObject bullet_hit_effect;

    [Header("Cannon")]
    public float infantary_damage;
    public float vehicle_damage;
    [Range(Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE)] public float spread;
    [Range(Spread.MIN_SPREAD_VALUE, Spread.MAX_SPREAD_VALUE)] public float max_spread;
    public float fire_rate;
    public float interval => 60f / fire_rate;
    public float overheat_time;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;
    
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
