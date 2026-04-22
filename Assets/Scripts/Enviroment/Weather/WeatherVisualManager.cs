using System;
using Unity.VisualScripting;
using UnityEngine;

public class WeatherVisualManager : MonoBehaviour
{
    [SerializeField] private WeatherTransitionManager transitionManager;

    [Header("Weather Objects")]
    [SerializeField] private GameObject rainObjectPrefab;
    [SerializeField] private GameObject snowObjectPrefab;
    [SerializeField] private GameObject windyObjectPrefab;
    [SerializeField] private GameObject hurricaneObjectPrefab;
    [SerializeField] private GameObject stormObjectPrefab;

    private AudioSource rain_sound;
    private AudioSource snow_sound;
    private AudioSource wind_sound;
    private AudioSource storm_sound;
    private AudioSource hurricane_sound;

    private ParticleSystem rainParticleSystem;
    private ParticleSystem snowParticleSystem;
    private ParticleSystem windyParticleSystem;
    private ParticleSystem hurricaneParticleSystem;
    private ParticleSystem stormParticleSystem;

    private WeatherStateManager.WeatherType currentActiveWeather;

    private GameObject rainObject;
    private GameObject snowObject;
    private GameObject windyObject;
    private GameObject hurricaneObject;
    private GameObject stormObject;

    private Coroutine currentParticleDeactivationCoroutine;
    private Coroutine currentSoundDeactivationCoroutine;

    public void InitializeComponents()
    {
        rainObject = Instantiate(rainObjectPrefab, transform.position, Quaternion.identity);
        snowObject = Instantiate(snowObjectPrefab, transform.position, Quaternion.identity);
        if (windyObject == null) windyObject = Instantiate(windyObjectPrefab, transform.position, Quaternion.identity);
        hurricaneObject = Instantiate(hurricaneObjectPrefab, transform.position, Quaternion.identity);
        stormObject = Instantiate(stormObjectPrefab, transform.position, Quaternion.identity);

        // Inicializa chuva
        if (rainObject != null)
        {
            rainParticleSystem = rainObject.GetComponent<ParticleSystem>();
            rain_sound = rainObject.GetOrAddComponent<AudioSource>();
        }


        // Inicializa neve
        if (snowObject != null)
        {
            snowParticleSystem = snowObject.GetComponent<ParticleSystem>();
            snow_sound = snowObject.GetOrAddComponent<AudioSource>();
        }


        // Inicializa vento (cloudy)
        if (windyObject != null)
        {
            windyParticleSystem = windyObject.GetComponent<ParticleSystem>();
            wind_sound = windyObject.GetOrAddComponent<AudioSource>();
        }


        if (hurricaneObject != null)
        {
            hurricaneParticleSystem = hurricaneObject.GetComponent<ParticleSystem>();
            hurricane_sound = hurricaneObject.GetOrAddComponent<AudioSource>();
        }


        if (stormObject != null)
        {
            stormParticleSystem = stormObject.GetComponent<ParticleSystem>();
            storm_sound = stormObject.GetOrAddComponent<AudioSource>();
        }

    }

    void Update()
    {
        try
        {
            if (PlayerController.Instance == null)
            {
                if (PlayerSpawnController.Instance == null)
                {
                    return;
                }
                snowObject.transform.position = PlayerSpawnController.Instance.spawn_camera.transform.position;
                rainObject.transform.position = PlayerSpawnController.Instance.spawn_camera.transform.position;
                windyObject.transform.position = PlayerSpawnController.Instance.spawn_camera.transform.position;
                hurricaneObject.transform.position = PlayerSpawnController.Instance.spawn_camera.transform.position;
                stormObject.transform.position = PlayerSpawnController.Instance.spawn_camera.transform.position;
            }
            else
            {
                snowObject.transform.position = PlayerController.Instance.transform.position;
                rainObject.transform.position = PlayerController.Instance.transform.position;
                windyObject.transform.position = PlayerController.Instance.transform.position;
                hurricaneObject.transform.position = PlayerController.Instance.transform.position;
                stormObject.transform.position = PlayerController.Instance.transform.position;
            }
        }catch(Exception)
        {
            
        }


    }

    public void ActivateWeather(WeatherStateManager.WeatherType weatherType)
    {
        currentActiveWeather = weatherType;

        switch (weatherType)
        {
            case WeatherStateManager.WeatherType.Rain:
                ActivateRain();
                break;
            case WeatherStateManager.WeatherType.Snow:
                ActivateSnow();
                break;
            case WeatherStateManager.WeatherType.Overcast:
                ActivateClear();
                break;
            case WeatherStateManager.WeatherType.Clear:
                ActivateClear();
                break;
            case WeatherStateManager.WeatherType.Storm:
                ActivateStorm();
                break;
            case WeatherStateManager.WeatherType.Windy:
                ActivateWindy();
                break;
            case WeatherStateManager.WeatherType.Hurricane:
                ActivateHurricane();
                break;
        }
    }

    private void ActivateRain()
    {

        if (currentParticleDeactivationCoroutine == null)
        {
            if (snowParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(snowParticleSystem, transitionManager.transitionSpeed));
            if (windyParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(windyParticleSystem, transitionManager.transitionSpeed));
            if (hurricaneParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(hurricaneParticleSystem, transitionManager.transitionSpeed));
            if (stormParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(stormParticleSystem, transitionManager.transitionSpeed));
        }

        if (currentSoundDeactivationCoroutine == null)
        {
            if (snow_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(snow_sound, transitionManager.transitionSpeed));

            if (wind_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(wind_sound, transitionManager.transitionSpeed));

            if (hurricane_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(hurricane_sound, transitionManager.transitionSpeed));

            if (storm_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(storm_sound, transitionManager.transitionSpeed));
        }



    }

    private void ActivateSnow()
    {

        if (currentParticleDeactivationCoroutine == null)
        {
            // Desativa outros climas
            if (rainParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(rainParticleSystem, transitionManager.transitionSpeed));
            if (windyParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(windyParticleSystem, transitionManager.transitionSpeed));
            if (hurricaneParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(hurricaneParticleSystem, transitionManager.transitionSpeed));

            if (stormParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(stormParticleSystem, transitionManager.transitionSpeed));
        }

        if (currentSoundDeactivationCoroutine == null)
        {

            if (rain_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(rain_sound, transitionManager.transitionSpeed));

            if (wind_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(wind_sound, transitionManager.transitionSpeed));

            if (hurricane_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(hurricane_sound, transitionManager.transitionSpeed));

            if (storm_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(storm_sound, transitionManager.transitionSpeed));
        }

    }

    private void ActivateStorm()
    {

        if (currentParticleDeactivationCoroutine == null)
        {
            // Desativa outros climas
            if (rainParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(rainParticleSystem, transitionManager.transitionSpeed));
            if (snowParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(snowParticleSystem, transitionManager.transitionSpeed));
            if (windyParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(windyParticleSystem, transitionManager.transitionSpeed));
            if (hurricaneParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(hurricaneParticleSystem, transitionManager.transitionSpeed));
        }


        if (currentSoundDeactivationCoroutine == null)
        {

            if (rain_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(rain_sound, transitionManager.transitionSpeed));

            if (snow_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(snow_sound, transitionManager.transitionSpeed));

            if (wind_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(wind_sound, transitionManager.transitionSpeed));

            if (hurricane_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(hurricane_sound, transitionManager.transitionSpeed));
        }

    }

    private void ActivateWindy()
    {
        if (currentParticleDeactivationCoroutine == null)
        {
            // Desativa outros climas
            if (rainParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(rainParticleSystem, transitionManager.transitionSpeed));
            if (snowParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(snowParticleSystem, transitionManager.transitionSpeed));
            if (hurricaneParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(hurricaneParticleSystem, transitionManager.transitionSpeed));
            if (stormParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(stormParticleSystem, transitionManager.transitionSpeed));
        }


        if (currentSoundDeactivationCoroutine == null)
        {

            if (rain_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(rain_sound, transitionManager.transitionSpeed));

            if (snow_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(snow_sound, transitionManager.transitionSpeed));

            if (hurricane_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(hurricane_sound, transitionManager.transitionSpeed));

            if (storm_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(storm_sound, transitionManager.transitionSpeed));
        }


    }

    private void ActivateHurricane()
    {
        if (currentParticleDeactivationCoroutine == null)
        {
            // Desativa outros climas
            if (rainParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(rainParticleSystem, transitionManager.transitionSpeed));
            if (snowParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(snowParticleSystem, transitionManager.transitionSpeed));
            if (windyParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(windyParticleSystem, transitionManager.transitionSpeed));
            if (stormParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(stormParticleSystem, transitionManager.transitionSpeed));
        }

        if (currentSoundDeactivationCoroutine == null)
        {

            if (rain_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(rain_sound, transitionManager.transitionSpeed));

            if (snow_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(snow_sound, transitionManager.transitionSpeed));

            if (wind_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(wind_sound, transitionManager.transitionSpeed));

            if (storm_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(storm_sound, transitionManager.transitionSpeed));
        }

    }

    private void ActivateClear()
    {

        if (currentParticleDeactivationCoroutine == null)
        {

            if (rainParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(rainParticleSystem, transitionManager.transitionSpeed));

            if (snowParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(snowParticleSystem, transitionManager.transitionSpeed));

            if (windyParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(windyParticleSystem, transitionManager.transitionSpeed));

            if (hurricaneParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(hurricaneParticleSystem, transitionManager.transitionSpeed));

            if (stormParticleSystem.emission.rateOverTimeMultiplier > 0.01f)
                currentParticleDeactivationCoroutine = StartCoroutine(DeactivateParticlesAfterDelay(stormParticleSystem, transitionManager.transitionSpeed));
        }

        if (currentSoundDeactivationCoroutine == null)
        {

            if (rain_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(rain_sound, transitionManager.transitionSpeed));

            if (snow_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(snow_sound, transitionManager.transitionSpeed));

            if (wind_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(wind_sound, transitionManager.transitionSpeed));

            if (hurricane_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(hurricane_sound, transitionManager.transitionSpeed));

            if (storm_sound.volume > 0)
                currentSoundDeactivationCoroutine = StartCoroutine(DeactivateSoundsAfterDelay(storm_sound, transitionManager.transitionSpeed));
        }


    }


    public void UpdateIntensity(float t, WeatherPreset target)
    {
        switch (currentActiveWeather)
        {
            case WeatherStateManager.WeatherType.Rain:
                UpdateRainIntensity(t, target);
                UpdateAudioVolume(t, target, rain_sound);
                break;
            case WeatherStateManager.WeatherType.Snow:
                UpdateSnowIntensity(t, target);
                UpdateAudioVolume(t, target, snow_sound);
                break;
            case WeatherStateManager.WeatherType.Overcast:
                UpdateCloudyIntensity(t, target);
                break;
            case WeatherStateManager.WeatherType.Windy:
                UpdateWindyIntensity(t, target);
                UpdateAudioVolume(t, target, wind_sound);
                break;
            case WeatherStateManager.WeatherType.Storm:
                UpdateStormIntensity(t, target);
                UpdateAudioVolume(t, target, storm_sound);
                break;
            case WeatherStateManager.WeatherType.Hurricane:
                UpdateHurricaneIntensity(t, target);
                UpdateAudioVolume(t, target, hurricane_sound);
                break;
        }

    }

    private void UpdateRainIntensity(float t, WeatherPreset target)
    {
        if (rainParticleSystem != null)
        {
            var emission = rainParticleSystem.emission;
            float currentRate = Mathf.Lerp(0, target.ParticleRate, t);
            emission.rateOverTimeMultiplier = currentRate;
        }
    }

    private void UpdateSnowIntensity(float t, WeatherPreset target)
    {
        if (snowParticleSystem != null)
        {
            var emission = snowParticleSystem.emission;
            float currentRate = Mathf.Lerp(0, target.ParticleRate, t);
            emission.rateOverTimeMultiplier = currentRate;
        }
    }

    private void UpdateCloudyIntensity(float t, WeatherPreset target)
    {
        if (windyParticleSystem != null)
        {
            var emission = windyParticleSystem.emission;
            float currentRate = Mathf.Lerp(0, target.ParticleRate, t);
            emission.rateOverTimeMultiplier = currentRate;
        }
    }

    private void UpdateWindyIntensity(float t, WeatherPreset target)
    {
        if (windyParticleSystem != null)
        {
            var emission = windyParticleSystem.emission;
            float currentRate = Mathf.Lerp(0, target.ParticleRate, t);
            emission.rateOverTimeMultiplier = currentRate;
        }
    }

    private void UpdateStormIntensity(float t, WeatherPreset target)
    {
        if (stormParticleSystem != null)
        {
            var emission = stormParticleSystem.emission;
            float currentRate = Mathf.Lerp(0, target.ParticleRate, t);
            emission.rateOverTimeMultiplier = currentRate;
        }
    }

    private void UpdateHurricaneIntensity(float t, WeatherPreset target)
    {
        if (hurricaneParticleSystem != null)
        {
            var emission = hurricaneParticleSystem.emission;
            float currentRate = Mathf.Lerp(0, target.ParticleRate, t);
            emission.rateOverTimeMultiplier = currentRate;
        }
    }

    private void UpdateAudioVolume(float t, WeatherPreset target, AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Lerp(0, target.AudioVolume, t);
        }
    }

    public void SetFinalIntensity(WeatherPreset target)
    {
        switch (currentActiveWeather)
        {
            case WeatherStateManager.WeatherType.Rain:
                if (rainParticleSystem != null)
                {
                    var emission = rainParticleSystem.emission;
                    emission.rateOverTimeMultiplier = target.ParticleRate;
                }
                break;
            case WeatherStateManager.WeatherType.Snow:
                if (snowParticleSystem != null)
                {
                    var emission = snowParticleSystem.emission;
                    emission.rateOverTimeMultiplier = target.ParticleRate;
                }
                break;
            case WeatherStateManager.WeatherType.Overcast:
                if (windyParticleSystem != null)
                {
                    var emission = windyParticleSystem.emission;
                    emission.rateOverTimeMultiplier = target.ParticleRate;
                }
                break;
            case WeatherStateManager.WeatherType.Windy:
                if (windyParticleSystem != null)
                {
                    var emission = windyParticleSystem.emission;
                    emission.rateOverTimeMultiplier = target.ParticleRate;
                }
                break;
            case WeatherStateManager.WeatherType.Storm:
                if (stormParticleSystem != null)
                {
                    var emission = stormParticleSystem.emission;
                    emission.rateOverTimeMultiplier = target.ParticleRate;
                }
                break;
            case WeatherStateManager.WeatherType.Hurricane:
                if (hurricaneParticleSystem != null)
                {
                    var emission = hurricaneParticleSystem.emission;
                    emission.rateOverTimeMultiplier = target.ParticleRate;
                }
                break;
        }

    }

    private System.Collections.IEnumerator DeactivateParticlesAfterDelay(ParticleSystem particleSystem, float time)
    {

        var emission = particleSystem.emission;
        float startRate = emission.rateOverTimeMultiplier;
        float elapsed = 0;

        // Se já está desativado, não faz nada
        if (startRate <= 0.01f) yield break;

        while (elapsed < time)
        {
            float t = elapsed / time;
            float currentRate = Mathf.Lerp(startRate, 0, t);
            emission.rateOverTimeMultiplier = currentRate;
            elapsed += Time.deltaTime;
            yield return null;
        }

        emission.rateOverTimeMultiplier = 0;
        currentParticleDeactivationCoroutine = null;
    }

    private System.Collections.IEnumerator DeactivateSoundsAfterDelay(AudioSource sound, float time)
    {

        print(sound.gameObject);

        float startRate = sound.volume;
        float elapsed = 0;

        while (elapsed < time)
        {
            float t = elapsed / time;
            sound.volume = Mathf.Lerp(startRate, 0, t);
            print(sound.volume);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sound.volume = 0;
        currentSoundDeactivationCoroutine = null;

    }

    public void StopAllWeatherEffects()
    {
        if (rainParticleSystem != null) rainParticleSystem.Stop();
        if (snowParticleSystem != null) snowParticleSystem.Stop();
        if (windyParticleSystem != null) windyParticleSystem.Stop();
        if (hurricaneParticleSystem != null) hurricaneParticleSystem.Stop();
        if (stormParticleSystem != null) stormParticleSystem.Stop();
    }
}