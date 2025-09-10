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
    public float weight;
    
    [Header("Bullet")]
    public float muzzle_velocity;
    public float bullet_drop;

    [Header("Shooting")]
    public Transform bullefPref;
    public GameObject barrel;
    public float fire_rate;
    public float interval;
    public float zoom;
    public float overheat_time;

    [Header("Audio")]
    public AudioSource interior_turbine;
    public AudioSource shoot_sound;


    void Start()
    {
        interval = 60f / fire_rate;
    }


}
