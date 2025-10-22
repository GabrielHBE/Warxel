using System.Collections.Generic;
using UnityEngine;

public class Video : MonoBehaviour
{
    [Header("Graphics")]
    public List<string> graphic_presets = new List<string> { "Low", "Medium", "Hight" };
    [Range(100f, 4000f)]
    public float render_distance;
    public bool enable_shadows;
    public List<string> shadows = new List<string> { "Low", "Medium", "Hight" };
    public List<string> meshes = new List<string> { "Low", "Medium", "Hight" };
    public List<string> rain_quality = new List<string> { "Low", "Medium", "Hight" };

    [Header("Screen")]
    public bool limit_fps;
    public float max_fps;
    public float Vsync;
    public float brightness;
    public float render_scale;
    public bool custom_resolution;
    public float[] resolution = new float[2];
    public List<string> screen_mode = new List<string> { "Full Screen Window", "Maximized Window", "Windowed" };

    [Header("Camera")]
    [Range(50f, 120f)]
    public float infantary_fov;
    [Range(50f, 120f)]
    public float jet_fov;
    [Range(50f, 120f)]
    public float tank_fov;
    [Range(50f, 120f)]
    public float helicopter_fov;
    [Range(0.5f, 2f)]
    public float camera_shake_intensity;
    public bool vignette;
    [Range(0.5f, 2f)]
    public float motion_blur;

}
