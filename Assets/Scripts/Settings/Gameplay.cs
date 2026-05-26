using UnityEngine;

public class Gameplay : MonoBehaviour
{
    [Header("HitMarkers")]
    public bool show_hit_marker = true;
    public Color body_shot_marker_colour = Color.white;
    public Color head_shot_marker_colour = Color.red;
    public Color vehicle_marker_colour = Color.gray;
    [Range(0f, 1f)]
    public float hit_marker_opacity = 1;
    [Range(0.1f, 1f)]
    public float hit_marker_size = 1;

    [Header("User Interface")]
    public bool show_fps = true;
    public bool show_network_status = true;
    public bool show_level_progression = true;
    public bool show_kill_feed = true;
    [Range(0.1f, 2f)]
    public float sight_reticle_size;
    public Color sight_reticle_collor = Color.red;

    //Indicators
    public Color neutral_color = Color.gray;
    public Color enemy_color = Color.red;
    public Color ally_color = Color.softBlue;
    public Color squad_color = Color.lightGreen;
    [Range(0f, 1f)]
    public float enemy_indicator_opacity = 1;
    [Range(0f, 1f)]
    public float ally_indicator_opacity = 1;
    [Range(0f, 1f)]
    public float squad_indicator_opacity = 1;
    [Range(0f, 1f)]
    public float neutral_indicator_opacity = 1;
    [Range(0f, 1f)]
    public float enemy_indicator_aim_opacity = 1;
    [Range(0f, 1f)]
    public float ally_indicator_aim_opacity = 1;
    [Range(0f, 1f)]
    public float squad_indicator_aim_opacity = 1;
    [Range(0f, 1f)]
    public float neutral_indicator_aim_opacity = 1;
    

    [Header("Chat")]
    public bool show_chat = true;
    [Range(0f, 1f)]
    public float chat_opacity = 1;
    [Range(0.1f, 2f)]
    public float chat_size = 1;

}
