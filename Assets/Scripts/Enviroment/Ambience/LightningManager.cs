
using UnityEngine;

[ExecuteAlways]
public class LightningManager : MonoBehaviour
{
    [SerializeField] private Light directional_light;
    [SerializeField] private LightningPreset preset;
    [SerializeField(), Range(0, 24)] private float day_time;

    void Update()
    {
        if (preset == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            day_time += Time.deltaTime/600;
            day_time %= 24;
            UpdateLightning(day_time / 24f);
        }
        else
        {
            UpdateLightning(day_time / 24f);
        }
    }

    private void OnValidate()
    {
        if (directional_light != null)
        {
            return;
        }

        if (RenderSettings.sun != null)
        {
            directional_light = RenderSettings.sun;
        }
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directional_light = light;
                }
            }

        }
    }

    private void UpdateLightning(float timePercent)
    {
        RenderSettings.ambientLight = preset.ambient_color.Evaluate(timePercent);
        RenderSettings.fogColor = preset.fog_color.Evaluate(timePercent);

        if (directional_light != null)
        {
            directional_light.color = preset.directional_color.Evaluate(timePercent);
            directional_light.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90, -170, 0));
        }
    }

}
