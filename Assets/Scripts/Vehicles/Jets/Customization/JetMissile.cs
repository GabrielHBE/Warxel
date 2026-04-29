using UnityEngine;

public class JetMissile : MissileController, IsVehicleCustomizationPart
{
    public void Activate()
    {
        
        GetComponentInParent<JetBombsAndMissiles>().missile = this;
    }

    public void Deactivate()
    {
        Destroy(gameObject);
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        return VehicleCustomizableParts.JetMissile;
    }

    public string GetCustomizationPartName()
    {
        return gameObject.name;
    }
}
