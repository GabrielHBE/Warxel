using UnityEngine;

public class Controls : MonoBehaviour
{

    [Header("Gameplay")]
    public bool tap_to_aim;
    public bool tap_to_sprint;
    public bool tap_to_crouch;
    public bool tap_to_prone;
    public bool tap_to_vehicle_speed_boost;

    [Header("Mouse")]
    //Soldier
    [Range(0.01f, 100f)]
    public bool invert_horizontal_infantary_sensibility;
    [Range(0.01f, 100f)]
    public bool invert_vertical_infantary_sensibility;
    [Range(0.01f, 100f)]
    public float infantary_sensibility;
    [Range(0.01f, 100f)]
    public float infantary_aim_sensibility;
    //Tank
    [Range(0.01f, 100f)]
    public bool invert_horizontal_tank_sensibility;
    [Range(0.01f, 100f)]
    public bool invert_vertical_tank_sensibility;
    [Range(0.01f, 100f)]
    public float tank_sensibility;
    [Range(0.01f, 100f)]
    public float tank_aim_sensibility;
    //Jet
    [Range(0.01f, 100f)]
    public bool invert_horizontal_jet_sensibility;
    [Range(0.01f, 100f)]
    public bool invert_vertical_jet_sensibility;
    [Range(0.01f, 100f)]
    public float jet_sensibility;
    [Range(0.01f, 100f)]
    public float jet_aim_sensibility;
    //Heli
    [Range(0.01f, 100f)]
    public bool invert_horizontal_heli_sensibility;
    [Range(0.01f, 100f)]
    public bool invert_vertical_heli_sensibility;
    [Range(0.01f, 100f)]
    public float helicopter_sensibility;
    [Range(0.01f, 100f)]
    public float helicopter_aim_sensibility;
    
}
