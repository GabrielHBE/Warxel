using UnityEngine;

public class ProcessVehicleDamage : MonoBehaviour
{
    private Vehicle vehicle;
    [SerializeField] private float localResistance;

    void Awake() => vehicle = GetVehicleComponent();

    public void Damage(float dmg)
    {
        float damageDealt = dmg * ((100f - localResistance) / 100f);
        vehicle.TakeDamage(damageDealt);
    }

    private Vehicle GetVehicleComponent() => GetComponent<Vehicle>() ?? GetComponentInParent<Vehicle>();
    
    public string[] GetOccupantNames() => vehicle.GetOccupantNames();
    public bool IsVehicleDestroyed() => vehicle.vehicle_destroyed.Value;
    public float GetResistance() => vehicle.resistance.Value + localResistance;
    
}
