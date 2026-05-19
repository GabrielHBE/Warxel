using UnityEngine;

public class AttackHelicopterGunnerProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    [Header("UI")]
    public Sprite image_hud;

    [Header("Bullet")]
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;
    public AudioSource shoot_sound;
    public GameObject bullet_hit_effect;

    [Header("Cannon")]
    public float infantary_damage;
    public float vehicle_damage;
    public Transform bullefPref;
    public float spread;
    public float max_spread;
    public float fire_rate;
    public float overheat_time;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;
    public float interval => 60f / fire_rate;

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
