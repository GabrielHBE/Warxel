using UnityEngine;

public class JetProperties : MonoBehaviour
{

    [Header("Movement")]
    public float aceleration;
    public float max_speed;
    public float rotation_value;
    public float pitch_value;
    public float lean_value;
    public float maneuverability;
    public bool invertY;
    
    [Header("Bullet")]
    public float muzzle_velocity;
    public float bullet_drop;
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
