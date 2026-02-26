using UnityEngine;

public class HeliProperties : MonoBehaviour
{
    [Header("State Management")]
    public bool is_aiming;
    
    [Header("Hp")]
    public float hp;
    public float resistance;

    [Header("Movement")]
    public float maneuverability;
    public float stabilization_force;
    public float mass;
    public float lift_force;
    public float max_lift_force;
    public float rotation_value;
    public float max_rotation_value;
    public float pitch_value;
    public float max_pitch_value;
    public float lean_value;
    public float max_lean_value;

    [Header("Bullet")]
    public float muzzle_velocity;
    public float bullet_drop;
    public float minimum_damage;
    public AudioSource shoot_sound;
    public GameObject bullet_hit_effect;

    [Header("Cannon")]
    public Transform bullefPref;
    public GameObject shootPos;
    public float spread;
    public float max_spread;
    public float fire_rate;
    public float interval;
    public float zoom;
    public float overheat_time;
    public float damage;
    public float damage_dropoff;
    public float damage_dropoff_timer;
    public float destruction_force;

    void Start()
    {
        interval = 60f / fire_rate;
    }
    
}
