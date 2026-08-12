
public class MapSettings : InMatchClientSingleton<MapSettings>
{
    public MapSize map_size;
    public string map_name;
    public float max_altitude;
    public int max_jets;
    public int max_tanks;
    public int max_helis;

    public enum MapSize
    {
        Small,
        Medium,
        Large
    }

}


