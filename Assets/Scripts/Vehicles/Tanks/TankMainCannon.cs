using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class TankMainCannon : VehicleArmory
{
    protected override void Awake()
    {
        if (vehicle == null) vehicle = GetComponentInParent<Vehicle>();
        base.Awake();
    }

    protected override void OnRecoilApplied(float verticalRecoil)
    {
        if (vehicle == null || verticalRecoil == 0f) return;

        // Negative local X pitches the vehicle upward in the cannon's direction.
        Vector3 torque = -transform.right * Mathf.Abs(verticalRecoil);
        if (vehicle.IsController)
            ApplyRecoilTorque(torque);
        else
            CmdApplyRecoilTorque(torque);
    }

    [ServerRpc]
    private void CmdApplyRecoilTorque(Vector3 torque)
    {
        if (vehicle == null) return;

        // The gunner and the driver can own different network objects.
        if (vehicle.IsController)
            ApplyRecoilTorque(torque);
        else if (vehicle.Owner.IsValid)
            TargetApplyRecoilTorque(vehicle.Owner, torque);
    }

    [TargetRpc]
    private void TargetApplyRecoilTorque(NetworkConnection connection, Vector3 torque)
    {
        if (vehicle != null && vehicle.IsController)
            ApplyRecoilTorque(torque);
    }

    private void ApplyRecoilTorque(Vector3 torque)
    {
        Rigidbody body = vehicle.rb != null ? vehicle.rb : vehicle.GetComponent<Rigidbody>();
        if (body == null || body.isKinematic) return;

        body.AddTorque(torque, ForceMode.VelocityChange);
    }
}
