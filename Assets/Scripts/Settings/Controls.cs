using UnityEngine;

public class Controls : MonoBehaviour
{

    [Header("Gameplay")]
    public bool is_aim_on_hold = false;
    public bool is_sprint_on_hold = false;
    public bool is_crouch_on_hold = false;
    public bool is_prone_on_hold = false;
    public bool is_vehicle_boost_on_hold = false;

    [Header("Mouse")]

    [Header("Infantaty")]
    public bool invert_vertical_infantary_mouse;
    [Range(0.01f, 10f)]
    public float infantary_sensibility;
    [Range(0.01f, 10f)]
    public float infantary_aim_sensibility;

    [Header("Tank")]
    public bool invert_vertical_tank_mouse;
    [Range(0.01f, 10f)]
    public float tank_sensibility;
    [Range(0.01f, 10f)]
    public float tank_aim_sensibility;

    [Header("Jet")]
    public bool invert_vertical_jet_mouse;
    [Range(0.01f, 10f)]
    public float jet_sensibility;
    [Range(0.01f, 10f)]
    public float jet_aim_sensibility;

    [Header("Helicopter")]
    public bool invert_vertical_heli_mouse;
    [Range(0.01f, 10f)]
    public float helicopter_sensibility;
    [Range(0.01f, 10f)]
    public float helicopter_aim_sensibility;
    
}
