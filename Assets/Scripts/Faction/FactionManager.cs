using UnityEngine;

public class FactionManager : MonoBehaviour
{
    public int current_members;
    public RegionManager.Region[] regions_in_control;

    public void EnterFaction()
    {
        current_members += 1;
    }

    public void ExitFaction()
    {
        current_members -= 1;
    }


    public enum Faction
    {
        factionA,
        factionB,
    }

}
