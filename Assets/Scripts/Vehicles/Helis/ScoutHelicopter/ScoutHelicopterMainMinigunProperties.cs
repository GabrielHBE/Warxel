using UnityEngine;

public class ScoutHelicopterMainMinigunProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    public Sprite hudIcon;
    
    [Header("Bullet")]
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;
    public AudioSource shoot_sound;
    public GameObject bullet_hit_effect;

    [Header("Cannon")]
    public float infantary_damage;
    public float vehicle_damage;
    public Transform bulletPref;
    public float spread;
    public float max_spread;
    public float fire_rate;
    public float interval => 60f / fire_rate;
    public float overheat_time;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;

    /*
    void Start()
    {
        interval = 60f / fire_rate;
    }
    */
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
