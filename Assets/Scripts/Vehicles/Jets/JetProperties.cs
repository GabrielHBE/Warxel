using UnityEngine;

public class JetProperties : MonoBehaviour
{

    [Header("Hp")]
    public float hp;
    public float resistance;

    [Header("Movement")]
    public bool can_afterburner;
    public float aceleration;
    public float max_throttle;
    public float rotation_value;
    public float max_rotation_value;
    public float pitch_value;
    public float max_pitch_value;
    public float lean_value;
    public float max_lean_speed;
    public bool invertY;
    public float dive_speed_boost = 50f;
    [Header("Audio")]
    public AudioSource interior_turbine;
}
