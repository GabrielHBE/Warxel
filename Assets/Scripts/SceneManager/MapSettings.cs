using UnityEngine;

public class MapSettings : MonoBehaviour
{
    public static MapSettings Instante {get; private set;}
    public float max_altitude;
    public int max_jets;
    public int max_tanks;
    public int max_helis;
    
    void Awake()
    {
        Instante = this;
    }

}
