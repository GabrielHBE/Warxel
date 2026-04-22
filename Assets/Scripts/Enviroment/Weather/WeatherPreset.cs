using UnityEngine;

[CreateAssetMenu(fileName = "WeatherPreset", menuName = "Weather/Weather Preset")]
public class WeatherPreset : ScriptableObject
{
    [Header("Weather Preset Name")]
    public string presetName;

    [Header("Weather Modifiers")]
    public WeatherModifiers Modifiers;

    [Header("Cloud Preset")]
    public CloudPreset CloudPreset;

    [Header("Weather Particles")]
    public float ParticleRate;
    [Range(0, 1)] public float AudioVolume = 0.3f;
}

[System.Serializable]
public class WeatherModifiers
{
    public float LightModifier = 0f;
    public float SkyboxModifier = 0f;
    public float TemperatureModifier = 0f;
    [Range(0, 1)]
    public float FogIntensity = 0f;
    public Color FogColor = Color.gray;
}