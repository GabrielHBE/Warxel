using UnityEngine;

public class AttackHelicopterGunnerProperties : MonoBehaviour, IsVehicleCustomizationPart
{
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
    public float interval;
    public float overheat_time;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;

    public void Activate()
    {
        GetComponentInParent<AttackHelicopter>().gunner_gun_properties = this;
    }

    public void Deactivate()
    {
        Destroy(gameObject);
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        return VehicleCustomizableParts.AttackHeliGunnerGun;
    }

    public string GetCustomizationPartName()
    {
        return gameObject.name;
    }

    void Start()
    {
        interval = 60f / fire_rate;
    }
}
