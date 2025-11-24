using UnityEngine;

public class JetProperties : MonoBehaviour
{

    [Header("Hp")]
    public float hp;
    public float resistance;

    [Header("Movement")]
    public float aceleration;
    public float max_throttle;
    public float rotation_value;
    public float max_rotation_value;
    public float pitch_value;
    public float max_pitch_value;
    public float lean_value;
    public float max_lean_value;
    public bool invertY;
    public float dive_speed_boost = 50f;
    
    [Header("Bullet")]
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;
    public GameObject bullet_hit_effect;

    [Header("MainCannon")]
    public Transform bullefPref;
    public GameObject barrel;
    public float fire_rate;
    public float interval;
    public float zoom;
    public float overheat_time;
    public float damage;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;

    [Header("Audio")]
    public AudioSource interior_turbine;
    public AudioSource shoot_sound;


    void Start()
    {
        interval = 60f / fire_rate;
    }


}
