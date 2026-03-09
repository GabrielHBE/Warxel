using UnityEngine;

public class RegionManager : MonoBehaviour
{
    public Region region;
    public FactionManager.Faction faction_in_control;


    public enum Region
    {
        Brazil,
        EUA,
        France,
        Russia,
        Gibraltar,
        Egipt
    }
}
