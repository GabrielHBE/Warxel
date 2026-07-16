using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour
{
    [Header("Volume")]
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float general_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float in_world_voip_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float voip_radio_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float music_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float world_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float enviroment_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float hit_volume = 50;
    [Range(0f, AudioMixerManager.VOLUME_COMPENSATOR)]
    public float kill_volume = 50;

    [Header("Voip")]
    public bool enable_deth_voip;
    public VoipModes selected_in_world_voip_mode;
    public VoipModes selected_radio_world_voip_mode;
    public KeyCode in_world_voip_key = KeyCode.V;
    public KeyCode radio_voip_key = KeyCode.B;

    public enum VoipModes
    {
        Off,
        Push,
        Enabled
    }

}
