using UnityEngine;

public interface ISspottable
{
    FactionManager.Faction GetFaction();
    Transform GetSpotPosition();
}