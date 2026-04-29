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

    [Header("Aiming")]
    public float zoom;

}
