using FishNet.Object;
using UnityEngine;

public class StateManager : NetworkBehaviour
{
    public static StateManager Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] private WeatherStateManager weatherState;
    [SerializeField] private WeatherTransitionManager weatherTransition;
    [SerializeField] private WeatherVisualManager weatherVisuals;
    [SerializeField] private LightingModifier lightingModifier;
    [SerializeField] private CloudManager cloudManager;
    [SerializeField] private SunRotationController sunRotation;

    [Header("Weather Data")]
    public WeatherData weatherData;

    private DayNightCycleManager dayNightCycle;

    private void Awake()
    {
        Instance = this;
        InitializeSystems();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        weatherVisuals.InitializeComponents();
    }

    private void InitializeSystems()
    {
        dayNightCycle = new DayNightCycleManager();
        dayNightCycle.Initialize(RenderSettings.skybox);

        lightingModifier.Initialize(dayNightCycle);
        cloudManager.Initialize();
        weatherTransition.Initialize(lightingModifier, cloudManager, weatherVisuals, weatherState);
        sunRotation.Initialize(dayNightCycle);
    }

    private void OnNetworkWeatherChanged(string newValue)
    {

        // A transição ocorre localmente em cada cliente
        var weatherType = ParseWeatherType(newValue);
        weatherState.SetWeather(newValue);
        weatherTransition.TransitionToWeather(weatherType);

    }

    private WeatherStateManager.WeatherType ParseWeatherType(string weather)
    {
        switch (weather)
        {
            case "rain": return WeatherStateManager.WeatherType.Rain;
            case "snow": return WeatherStateManager.WeatherType.Snow;
            case "overcast": return WeatherStateManager.WeatherType.Overcast;
            case "storm": return WeatherStateManager.WeatherType.Storm;
            case "windy": return WeatherStateManager.WeatherType.Windy;
            case "hurricane": return WeatherStateManager.WeatherType.Hurricane;
            default: return WeatherStateManager.WeatherType.Clear;
        }
    }

    private void Update()
    {
        if (IsServerInitialized)
        {
            UpdateNetworkTime();
            UpdateWeatherFromAPI();
            HandleAdminInput();
        }

        UpdateLocalSystems();
    }

    private void UpdateNetworkTime()
    {
        weatherState.NetworkTime.Value += (Time.deltaTime * 3600f) / 1;
    }

    private void UpdateWeatherFromAPI()
    {
        if (weatherData?.Info?.currently != null)
        {
            string fetchedWeather = weatherData.Info.currently.icon;
            if (weatherState.NetworkWeather.Value != fetchedWeather)
                weatherState.NetworkWeather.Value = fetchedWeather;
        }
    }

    private void HandleAdminInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            weatherState.NetworkWeather.Value = "rain";
            OnNetworkWeatherChanged("rain");
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            weatherState.NetworkWeather.Value = "snow";
            OnNetworkWeatherChanged("snow");
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            weatherState.NetworkWeather.Value = "overcast";
            OnNetworkWeatherChanged("overcast");
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            weatherState.NetworkWeather.Value = "clear-day";
            OnNetworkWeatherChanged("clear-day");
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            weatherState.NetworkWeather.Value = "storm";
            OnNetworkWeatherChanged("storm");
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            weatherState.NetworkWeather.Value = "windy";
            OnNetworkWeatherChanged("windy");
        }
        else if (Input.GetKeyDown(KeyCode.F7))
        {
            weatherState.NetworkWeather.Value = "hurricane";
            OnNetworkWeatherChanged("hurricane");
        }

    }

    private void UpdateLocalSystems()
    {
        weatherState.CurrentTime = weatherState.NetworkTime.Value;
        dayNightCycle.UpdateTime(weatherState.CurrentTime);
        sunRotation.UpdateRotation();

        // Aplica valores base se não houver clima ativo
        if (weatherState.ActiveWeatherType.Value == WeatherStateManager.WeatherType.Clear)
            lightingModifier.ApplyBaseValues();
    }
}