using UnityEngine;

public class TankPilotGunProperties : MonoBehaviour, IsVehicleCustomizationPart
{
    [Header("Damage model")]
    public float infantary_damage;
    public float vehicle_damage;
    public float muzzle_velocity;
    public float minimum_damage;
    public float bullet_drop;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;
    public GameObject bullet_hit_effect;
    public Sprite hud_image;
    public Transform bullefPref;
    public float fire_rate;
    public float interval;
    public float overheat_time;
    public AudioSource shoot_shound;

    void Start()
    {
        interval = 60f / fire_rate;
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
