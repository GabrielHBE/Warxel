using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class WeatherTransitionManager : NetworkBehaviour
{
    [Header("Transition Settings")]
    public float transitionSpeed = 1f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("System References")]
    [SerializeField] private LightingModifier lightingModifier;
    [SerializeField] private CloudManager cloudManager;
    [SerializeField] private WeatherVisualManager visualManager;
    [SerializeField] private WeatherStateManager stateManager;

    [Header("Weather Presets")]
    [SerializeField] private WeatherPreset clearPreset;
    [SerializeField] private WeatherPreset rainPreset;
    [SerializeField] private WeatherPreset stormPreset;
    [SerializeField] private WeatherPreset snowPreset;
    [SerializeField] private WeatherPreset overcastPreset;
    [SerializeField] private WeatherPreset windyPreset;
    [SerializeField] private WeatherPreset hurricanePreset;


    private Dictionary<WeatherStateManager.WeatherType, WeatherPreset> presetMap;

    private WeatherModifiers currentModifiers;
    private Coroutine currentTransition;

    private void Awake()
    {
        InitializeModifiers();
        BuildPresetMap();
    }

    public void TransitionToWeather(WeatherStateManager.WeatherType weatherType)
    {
        if (presetMap.TryGetValue(weatherType, out WeatherPreset preset) && preset != null)
        {
            ActivateWeather(weatherType);
            StartWeatherTransition(preset);
        }
        else
        {
            Debug.LogWarning($"No preset found for weather type: {weatherType}");
        }
    }

    private void BuildPresetMap()
    {
        presetMap = new Dictionary<WeatherStateManager.WeatherType, WeatherPreset>
        {
            { WeatherStateManager.WeatherType.Clear, clearPreset },
            { WeatherStateManager.WeatherType.Rain, rainPreset },
            { WeatherStateManager.WeatherType.Snow, snowPreset },
            { WeatherStateManager.WeatherType.Overcast, overcastPreset},
            { WeatherStateManager.WeatherType.Storm, stormPreset },
            { WeatherStateManager.WeatherType.Windy, windyPreset },
            { WeatherStateManager.WeatherType.Hurricane, hurricanePreset }
        };
    }

    private void InitializeModifiers()
    {
        currentModifiers = new WeatherModifiers
        {
            LightModifier = 0f,
            SkyboxModifier = 0f,
            TemperatureModifier = 0f,
            FogIntensity = 0f,
            FogColor = Color.gray
        };
    }

    public void Initialize(LightingModifier lighting, CloudManager clouds,
                          WeatherVisualManager visuals, WeatherStateManager state)
    {
        lightingModifier = lighting;
        cloudManager = clouds;
        visualManager = visuals;
        stateManager = state;
    }

    [ObserversRpc]
    public void StartWeatherTransition(WeatherPreset targetPreset)
    {

        if (targetPreset == null)
        {
            Debug.LogWarning("Target preset is null!");
            return;
        }

        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(TransitionRoutine(targetPreset));
    }


    private IEnumerator TransitionRoutine(WeatherPreset target)
    {
        // Salva estado inicial
        WeatherModifiers startModifiers = currentModifiers;
        CloudPreset startCloud = cloudManager != null ? cloudManager.GetCurrentPreset() : new CloudPreset();
        float elapsed = 0;

        // Interpola nuvens
        if (cloudManager != null)
            cloudManager.TransitionToPreset(target.CloudPreset, transitionSpeed);

        while (elapsed < transitionSpeed)
        {
            float t = elapsed / transitionSpeed;
            float easedT = transitionCurve.Evaluate(t);

            // Interpola modificadores de iluminação
            currentModifiers = LerpModifiers(startModifiers, target.Modifiers, easedT);
            if (lightingModifier != null)
                lightingModifier.ApplyModifiers(currentModifiers);

            // Interpola partículas e áudio
            if (visualManager != null)
                visualManager.UpdateIntensity(easedT, target);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Aplica valores finais
        currentModifiers = target.Modifiers;
        if (lightingModifier != null)
            lightingModifier.ApplyModifiers(currentModifiers);

        if (visualManager != null)
            visualManager.SetFinalIntensity(target);

        currentTransition = null;
    }

    [ObserversRpc]
    private void ActivateWeather(WeatherStateManager.WeatherType weatherType)
    {
        // Ativa os objetos visuais
        if (visualManager != null && stateManager != null)
        {
            visualManager.ActivateWeather(weatherType);
        }
    }

    private WeatherModifiers LerpModifiers(WeatherModifiers a, WeatherModifiers b, float t)
    {
        return new WeatherModifiers
        {
            LightModifier = Mathf.Lerp(a.LightModifier, b.LightModifier, t),
            SkyboxModifier = Mathf.Lerp(a.SkyboxModifier, b.SkyboxModifier, t),
            TemperatureModifier = Mathf.Lerp(a.TemperatureModifier, b.TemperatureModifier, t),
            FogIntensity = Mathf.Lerp(a.FogIntensity, b.FogIntensity, t),
            FogColor = Color.Lerp(a.FogColor, b.FogColor, t)
        };
    }

    public void StopCurrentTransition()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
    }
}