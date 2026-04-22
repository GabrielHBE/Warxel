using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class WeatherStateManager : NetworkBehaviour
{
    public static WeatherStateManager Instance { get; private set; }

    public readonly SyncVar<float> NetworkTime = new SyncVar<float>(43200f);
    public readonly SyncVar<string> NetworkWeather = new SyncVar<string>("clear-day");

    [Header("Current Time")]
    public float CurrentTime;

    [Header("Current Weather")]
    public readonly SyncVar<WeatherType> ActiveWeatherType = new SyncVar<WeatherType>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

    }

    [ObserversRpc]
    public void SetWeather(string weather)
    {

        NetworkWeather.Value = weather;
        ActiveWeatherType.Value = ParseWeatherType(weather);
 
    }

    private WeatherType ParseWeatherType(string weather)
    {
        if (weather == "rain") return WeatherType.Rain;
        if (weather == "snow" || weather == "sleet") return WeatherType.Snow;
        if (weather == "cloudy" || weather == "partly-cloudy-day" || weather == "partly-cloudy-night" || weather == "fog") return WeatherType.Overcast;
        if (weather == "storm") return WeatherType.Storm;
        if (weather == "windy") return WeatherType.Windy;
        if (weather == "hurricane") return WeatherType.Hurricane;

        return WeatherType.Clear;
    }

    public enum WeatherType {Clear, Rain, Snow, Overcast, Storm, Windy, Hurricane}

}