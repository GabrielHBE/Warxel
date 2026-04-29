

public interface IsVehicleCustomizationPart
{
    public void Activate();

    public void Deactivate();

    public VehicleCustomizableParts GetCustomizationPart();

    public string GetCustomizationPartName();

}

public enum VehicleCustomizableParts
{
    //Tank
    TankShell,
    TankPilotGun,
    TankGunnerGun,

    //AttackJet
    JetBomb,
    JetMissile,

    //AttackHeli
    AttackHeliGunnerGun,
    AttackHeliPilotPrimaryMissile,
    AttackHeliPilotSecondaryMissile,

    //General
    Countermeasure,
    
}
