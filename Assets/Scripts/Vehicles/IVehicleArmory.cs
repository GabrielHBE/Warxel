using UnityEngine;

public interface IVehicleArmory
{
    public void Shoot();
    public Sprite GetArmoryIcon();
    public void ActivateArmory();
    public void DeactivateArmory();
    public string GetCurrentAmmo();
    public float GetHeatingLevel();
    public float GetMaxOverheat();
}
