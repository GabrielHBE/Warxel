using UnityEngine;

public abstract class VehicleAim : MonoBehaviour, IsVehicleCustomizationPart
{
    [SerializeField] private AimType aimType;
    private Vehicle vehicle;

    public abstract void EnableAim();
    public abstract void DisableAim();

    #region Interface Implementations
    public void Activate()
    {
        vehicle = GetComponentInParent<Vehicle>();
    }

    public void Deactivate()
    {
        throw new System.NotImplementedException();
    }

    public VehicleCustomizableParts GetCustomizationPart()
    {
        return VehicleCustomizableParts.VehicleAim;
    }

    public string GetCustomizationPartName()
    {
        throw new System.NotImplementedException();
    }
    #endregion

    #region Enums
    public enum AimType
    {
        None,
        Zoom,
        NightVision,
        InfraRed
        
    }
    #endregion
}
