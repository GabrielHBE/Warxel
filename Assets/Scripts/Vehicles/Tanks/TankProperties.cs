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
    public float zoom;

}
