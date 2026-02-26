using UnityEngine;

public class TankProperties : MonoBehaviour
{
    [Header("Hp")]
    public float hp;
    public float resistance;
    
    [Header("Movement")]
    public float acceleration;
    public float max_speed;
    public float max_throttle;
    public float rotation_value;
    public float max_rotation_speed;
    public bool can_boost;
    public float boost_force;

    [Header("Turret")]
    public float turret_rotation_value;
    public float turret_max_rotation_value;

    [Header("Bullet")]
    public float muzzle_velocity;
    public float minimum_damage;
    public float bullet_dropoff;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;
    public GameObject bullet_hit_effect;
    public Sprite pilot_gun_hud_image;
    public Transform bullefPref;
    public float fire_rate;
    public float interval;
    public float zoom;
    public float overheat_time;
    public float damage;
    public AudioSource shoot_shound;

    void Start()
    {
        interval = 60f / fire_rate;
    }
    
}
