using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour
{
    [Header("Volume")]
    [Range(0f, 100f)]
    public float general_volume;
    [Range(0f, 100f)]
    public float in_world_voip_volume;
    [Range(0f, 100f)]
    public float voip_radio_volume;
    [Range(0f, 100f)]
    public float music_volume;
    [Range(0f, 100f)]
    public float world_volume;
    [Range(0f, 100f)]
    public float hit_volume;
    [Range(0f, 100f)]
    public float kill_volume;
    [Range(0f, 100f)]
    public float vehicle_volume;
    [Range(0f, 100f)]
    public float infantary_volume;
    [Range(0f, 100f)]
    public float microphone_volume;

    [Header("Voip")]
    public bool enable_deth_voip;
    public List<string> in_world_voip_modes = new List<string> { "Off", "Push", "Enabled" };
    public List<string> radio_world_voip_modes = new List<string> { "Off", "Push", "Enabled" };
    public KeyCode in_world_voip_key;
    public KeyCode radio_voip_key;


}
