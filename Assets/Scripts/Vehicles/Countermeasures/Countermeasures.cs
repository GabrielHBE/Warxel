using UnityEngine;

public abstract class Countermeasures : MonoBehaviour
{
    [SerializeField] protected float countermeasures_duration = 10;
    [SerializeField] protected float reload_countermeasures_duration = 10;
    protected Vehicle vehicle;
    protected float countermeasures_original_duration;
    protected float reload_countermeasures_original_duration;

    void Start()
    {
        reload_countermeasures_original_duration = reload_countermeasures_duration;
        countermeasures_original_duration = countermeasures_duration;
    }
    
    public abstract void SetVehicle(Vehicle vehicle);
    public abstract void UseCountermeasure();
    protected abstract void StopCountermeasure();
}
