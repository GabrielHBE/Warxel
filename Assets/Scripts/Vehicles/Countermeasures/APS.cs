using UnityEngine;

public class APS : Countermeasures
{
    void Update()
    {
        if (reload_countermeasures_duration < 0) return;

        if (vehicle.ignore_damage == false)
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
        vehicle.ignore_damage = false;
        countermeasures_duration = countermeasures_original_duration;
    }

    public override void UseCountermeasure()
    {
        if (reload_countermeasures_duration > 0) return;

        vehicle.ignore_damage = true;
        reload_countermeasures_duration = reload_countermeasures_original_duration;
    }

    public override void SetVehicle(Vehicle vehicle)
    {
        this.vehicle = vehicle;
    }
}
