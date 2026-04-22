using UnityEngine;

public class MapSettings : MonoBehaviour
{
    public static MapSettings Instance {get; private set;}
    public MapSize map_size;
    public string map_name;
    public float max_altitude;
    public int max_jets;
    public int max_tanks;
    public int max_helis;
    
    void Awake()
    {
        Instance = this;
    }

}

public enum MapSize
{
    Small,
    Medium,
    Large
}
