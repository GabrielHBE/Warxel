using FishNet.Object;
using UnityEngine;

public class AttackHelicopterGunnerProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    [Header("UI")]
    public Sprite image_hud;

    [Header("Sounds")]
    public AudioClip shoot_sound;
    public SoundManager.SoundProperties shootSoundProperties = SoundManager.SoundProperties.Default;

    [Header("Bullet")]
    public DummyBullet dummyBullet;
    public NetworkObject networkBullet;
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;

    [Header("Recoil Settings")]
    public bool manual_calculate_recoil;
    public float weapon_reset_recoil_speed;
    public float weapon_apply_recoil_speed;
    public Vector3 visual_recoil;
    [Range(Recoil.MIN_FIRTSHOTINCREASER_VALUE, Recoil.MAX_FIRTSHOTINCREASER_VALUE)]
    public float first_shoot_increaser;
    public Recoil.RecoilPattern[] recoilPattern = new Recoil.RecoilPattern[0];
    [HideInInspector] public float horizontal_recoil_media;
    [HideInInspector] public float vertical_recoil_media;
    [Header("Cannon")]
    public float infantary_damage;
    public float vehicle_damage;
    public float spread;
    public float max_spread;
    public float fire_rate;
    public float overheat_time;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;
    public float interval => 60f / fire_rate;

    public void Awake()
    {

        if (!manual_calculate_recoil)
        {
            weapon_reset_recoil_speed = interval / 2;
            weapon_apply_recoil_speed = interval / 2;
        }
    }

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
