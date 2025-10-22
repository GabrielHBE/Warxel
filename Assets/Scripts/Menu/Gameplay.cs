using UnityEngine;

public class Gameplay : MonoBehaviour
{
    [Header("HitMarkers")]
    public bool show_hit_marker;
    public Color body_shot_marker_colour;
    public Color head_shot_marker_colour;
    [Range(0f, 1f)]
    public float hit_marker_opacity;
    [Range(0.1f, 2f)]
    public float hit_marker_size;

    [Header("User Interface")]
    public bool show_fps;
    public bool show_network_status;
    public bool show_level_progression;
    public bool show_kill_feed;
    [Range(0.1f, 2f)]
    public float sight_reticle_size;
    public Color sight_reticle_collor;

    //Indicators
    public Color enemy_color;
    public Color ally_color;
    public Color squad_color;
    [Range(0f, 1f)]
    public float enemy_indicator_opacity;
    [Range(0f, 1f)]
    public float ally_indicator_opacity;
    [Range(0f, 1f)]
    public float squad_indicator_opacity;
    [Range(0f, 1f)]
    public float enemy_indicator_aim_opacity;
    [Range(0f, 1f)]
    public float ally_indicator_aim_opacity;
    [Range(0f, 1f)]
    public float squad_indicator_aim_opacity;

    //Flags
    public Color enemy_color_flag;
    public Color ally_color_flag;
    public Color squad_color_flag;
    [Range(0f, 1f)]
    public float enemy_flag_opacity;
    [Range(0f, 1f)]
    public float ally_flag_opacity;
    [Range(0f, 1f)]
    public float enemy_flag_aim_opacity;
    [Range(0f, 1f)]
    public float ally_flag_aim_opacity;

    [Header("Chat")]
    public bool show_chat;
    [Range(0f, 1f)]
    public float chat_opacity;
    [Range(0.1f, 2f)]
    public float chat_size;

}
