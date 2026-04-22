using UnityEngine;

[System.Serializable]
public class DayNightCycleManager
{
    [Header("Time Configuration")]
    [SerializeField, Range(0, 23)] private int sunriseHour = 6;
    [SerializeField, Range(0, 23)] private int sunsetHour = 18;
    [SerializeField, Range(0, 23)] private int middayHour = 12;
    
    [Header("Light Intensity")]
    [SerializeField, Range(0, 2)] private float sunriseLight = 0.3f;
    [SerializeField, Range(0, 2)] private float middayLight = 1.0f;
    [SerializeField, Range(0, 2)] private float sunsetLight = 0.4f;
    [SerializeField, Range(0, 2)] private float nightLight = 0.1f;
    
    [Header("Skybox Intensity")]
    [SerializeField, Range(0, 2)] private float sunriseSkybox = 0.4f;
    [SerializeField, Range(0, 2)] private float middaySkybox = 1.0f;
    [SerializeField, Range(0, 2)] private float sunsetSkybox = 0.5f;
    [SerializeField, Range(0, 2)] private float nightSkybox = 0.2f;
    
    [Header("Temperature (Kelvin)")]
    [SerializeField, Range(1000, 20000)] private float sunriseTemp = 3000f;
    [SerializeField, Range(1000, 20000)] private float middayTemp = 6500f;
    [SerializeField, Range(1000, 20000)] private float sunsetTemp = 2500f;
    [SerializeField, Range(1000, 20000)] private float nightTemp = 8000f;
    
    private Material skyboxMaterial;
    private float currentHour;
    
    public DayNightValues BaseValues { get; private set; }
    
    public void Initialize(Material skyboxMaterial)
    {
        this.skyboxMaterial = skyboxMaterial;
        currentHour = middayHour;
        UpdateBaseValues();
    }
    
    public void UpdateTime(float normalizedTime)
    {
        currentHour = (normalizedTime % 86400f) / 3600f;
        UpdateSkyboxDayTime();
        UpdateBaseValues();
    }
    
    private void UpdateSkyboxDayTime()
    {
        if (skyboxMaterial == null) return;
        float skyboxValue = Mathf.PingPong(currentHour / 12f + 1, 1f);
        skyboxMaterial.SetFloat("_DayTime", skyboxValue);
    }
    
    private void UpdateBaseValues()
    {
        BaseValues = CalculateValuesAtHour(currentHour);
    }
    
    private DayNightValues CalculateValuesAtHour(float hour)
    {
        if (hour >= sunriseHour && hour < middayHour)
        {
            float t = (hour - sunriseHour) / (middayHour - sunriseHour);
            return new DayNightValues
            {
                LightIntensity = Mathf.Lerp(sunriseLight, middayLight, t),
                SkyboxIntensity = Mathf.Lerp(sunriseSkybox, middaySkybox, t),
                Temperature = Mathf.Lerp(sunriseTemp, middayTemp, t)
            };
        }
        else if (hour >= middayHour && hour < sunsetHour)
        {
            float t = (hour - middayHour) / (sunsetHour - middayHour);
            return new DayNightValues
            {
                LightIntensity = Mathf.Lerp(middayLight, sunsetLight, t),
                SkyboxIntensity = Mathf.Lerp(middaySkybox, sunsetSkybox, t),
                Temperature = Mathf.Lerp(middayTemp, sunsetTemp, t)
            };
        }
        else
        {
            float nightHour = hour < sunriseHour ? hour + 24 : hour;
            float t = (nightHour - sunsetHour) / (24 - sunsetHour + sunriseHour);
            return new DayNightValues
            {
                LightIntensity = Mathf.Lerp(sunsetLight, nightLight, t),
                SkyboxIntensity = Mathf.Lerp(sunsetSkybox, nightSkybox, t),
                Temperature = Mathf.Lerp(sunsetTemp, nightTemp, t)
            };
        }
    }
    
    public float GetCurrentHour() => currentHour;
    public bool IsNightTime() => currentHour >= sunsetHour || currentHour < sunriseHour;
}

public struct DayNightValues
{
    public float LightIntensity;
    public float SkyboxIntensity;
    public float Temperature;
}