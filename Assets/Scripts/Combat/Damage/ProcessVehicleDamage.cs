using UnityEngine;

public class ProcessVehicleDamage : MonoBehaviour
{
    private Vehicle vehicle;

    void Awake() => vehicle = GetVehicleComponent();
    public void Damage(float dmg) =>  vehicle.TakeDamage(dmg);
    private Vehicle GetVehicleComponent() => GetComponent<Vehicle>() ?? GetComponentInParent<Vehicle>();
    public string[] GetOccupantNames() => vehicle.GetOccupantNames();
    public bool IsVehicleDestroyed() => vehicle.vehicle_destroyed.Value;
    public float GetResistance() => vehicle.resistance.Value;
    public float GetHP() => vehicle.hp.Value;
    
}