using UnityEngine;

public interface ISspottable
{
    // Retorna a facção do objeto
    FactionManager.Faction GetFaction();
    
    // Retorna o transform "spot_position" específico para a UI ancorar
    Transform GetSpotPosition();
}