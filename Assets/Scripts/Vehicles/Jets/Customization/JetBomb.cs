using UnityEngine;

public class JetBomb : BombsController, IsVehicleCustomizationPart
{
    public void Activate()
    {
        GetComponentInParent<JetBombsAndMissiles>().bombs = this;
    }

    public void Deactivate()
    {
        Destroy(gameObject);
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        return VehicleCustomizableParts.JetBomb;
    }

    public string GetCustomizationPartName()
    {
        return gameObject.name;
    }
}
