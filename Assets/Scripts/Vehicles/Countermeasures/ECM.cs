using UnityEngine;

public class ECM : Countermeasures
{
    void Update()
    {
        if (reload_countermeasures_duration < 0) return;

        if (vehicle.used_locking_countermeasure == false)
        {
            reload_countermeasures_duration -= Time.deltaTime;
        }
        else
        {
            countermeasures_duration -= Time.deltaTime;
            if (countermeasures_duration <= 0)
            {
                StopCountermeasure();
            }
        }
    }
    protected override void StopCountermeasure()
    {
        vehicle.used_locking_countermeasure = false;
        countermeasures_duration = countermeasures_original_duration;
    }

    public override void UseCountermeasure()
    {
        if (reload_countermeasures_duration > 0) return;

        vehicle.used_locking_countermeasure = true;
        reload_countermeasures_duration = reload_countermeasures_original_duration;
    }

    public override void SetVehicle(Vehicle vehicle)
    {
        this.vehicle = vehicle;
    }
}
