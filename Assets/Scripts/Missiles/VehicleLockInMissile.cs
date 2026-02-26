using UnityEngine;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxelObjects;

public class VehicleLockInMissile : Missiles
{
    [SerializeField] private float maneuverability;
    private Vehicle target_vehicle;
    [HideInInspector] public Vehicle parent_vehicle;
    private bool can_follow_target = false;

    private Transform target_transform;

    private bool do_once = true;

    protected override void Update()
    {

        if (!can_follow_target) return;

        if (target_vehicle.used_locking_countermeasure == false)
        {

            target_transform = target_vehicle.GetGameObject().transform;

        }
        else
        {

            if (do_once)
            {
                target_transform.position = new Vector3(target_transform.position.x + Random.Range(0, 100), target_transform.position.y + Random.Range(0, 100), target_transform.position.z + Random.Range(0, 100));
                do_once = false;
            }


        }

        transform.LookAt(target_transform);

        transform.position = Vector3.Lerp(transform.position, target_transform.position, maneuverability);

    }
    public override void Shoot()
    {
        base.Shoot();
        can_follow_target = true;
    }
    public void SetVehicle(Vehicle vehicle)
    {
        target_vehicle = vehicle;
    }
}
