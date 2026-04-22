using UnityEngine;

public class LightingModifier : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private float maxIntensity = 1f;
    [SerializeField] private Material skyboxMaterial;
    
    [SerializeField] private DayNightCycleManager dayNightCycle;
    
    public void Initialize(DayNightCycleManager cycleManager)
    {
        dayNightCycle = cycleManager;
    }
    
    public void ApplyModifiers(WeatherModifiers modifiers)
    {
        var baseValues = dayNightCycle.BaseValues;
        
        float finalLight = Mathf.Clamp01(baseValues.LightIntensity + modifiers.LightModifier) * maxIntensity;
        float finalTemp = Mathf.Clamp(baseValues.Temperature + modifiers.TemperatureModifier, 1000f, 20000f);
        
        ApplyLighting(finalLight, finalTemp);
        ApplyFog(modifiers.FogIntensity, modifiers.FogColor);
    }
    
    public void ApplyBaseValues()
    {
        var baseValues = dayNightCycle.BaseValues;
        ApplyLighting(baseValues.LightIntensity * maxIntensity, baseValues.Temperature);
        ApplySkybox(baseValues.SkyboxIntensity, 1f);
    }
    
    private void ApplyLighting(float intensity, float temperature)
    {
        if (sunLight != null)
        {
            sunLight.intensity = intensity;
            if (sunLight.useColorTemperature)
                sunLight.colorTemperature = temperature;
        }
    }
    
    private void ApplySkybox(float intensity, float exposure)
    {
        RenderSettings.ambientIntensity = intensity;
        if (skyboxMaterial != null && skyboxMaterial.HasProperty("_Exposure"))
            skyboxMaterial.SetFloat("_Exposure", exposure);
    }
    
    private void ApplyFog(float density, Color color)
    {
        if (RenderSettings.fog)
        {
            RenderSettings.fogDensity = density;
            RenderSettings.fogColor = color;
        }
    }
}